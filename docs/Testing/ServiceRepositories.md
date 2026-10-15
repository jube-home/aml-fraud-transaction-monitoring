---
layout: default
title: Service Repositories
nav_order: 13
parent: Testing
---

# Service Repositories

Tests of the repository-backed services, one per database table, covering create, read, update, delete, tenancy
isolation and validation for each entity.

## Activation watcher

### ActivationWatcherServiceTests

`Jube.Test.Service.Repository.ActivationWatcher.ActivationWatcherServiceTests` — 8 tests. Category: Service. Collection:
Database.

| Test                                                 | Kind | Verifies                                             |
|------------------------------------------------------|------|------------------------------------------------------|
| `ReplayReturnsRowsInTenantAndProjectsFieldsAsync`    | Fact | Replay returns rows in tenant and projects fields.   |
| `ReplayIsScopedToCallersTenantAsync`                 | Fact | Replay is scoped to callers tenant.                  |
| `ReplayFiltersByDateRangeAsync`                      | Fact | Replay filters by date range.                        |
| `ReplayWithoutPermissionThrowsForbiddenAsync`        | Fact | Replay without permission throws forbidden.          |
| `CreateWithNullUserNameThrowsNotAuthenticatedAsync`  | Fact | Create with null user name throws not authenticated. |
| `CreateWithUnknownTenantThrowsNotAuthenticatedAsync` | Fact | Create with unknown tenant throws not authenticated. |
| `ReplayDoesNotPublishAChangeEventAsync`              | Fact | Replay does not publish a change event.              |
| `ReplayWithNoMatchingRowsReturnsEmptyAsync`          | Fact | Replay with no matching rows returns empty.          |

## Archive

### ArchiveServiceTests

`Jube.Test.Service.Repository.Archive.ArchiveServiceTests` — 13 tests. Category: Service. Collection: Database.

| Test                                                 | Kind | Verifies                                             |
|------------------------------------------------------|------|------------------------------------------------------|
| `TagReplacesTagSetOnArchiveJsonAsync`                | Fact | Tag replaces tag set on archive json.                |
| `TagMergesArchiveTagRowsAsync`                       | Fact | Tag merges archive tag rows.                         |
| `TagWithEmptyArrayClearsAllTagsAsync`                | Fact | Tag with empty array clears all tags.                |
| `TagIsNotVisibleAcrossTenantsThrowsNotFoundAsync`    | Fact | Tag is not visible across tenants throws not found.  |
| `TagWithUnknownGuidThrowsNotFoundAsync`              | Fact | Tag with unknown guid throws not found.              |
| `TagWithoutPermissionThrowsForbiddenAsync`           | Fact | Tag without permission throws forbidden.             |
| `TagWithEmptyGuidThrowsValidationFailedAsync`        | Fact | Tag with empty guid throws validation failed.        |
| `TagWithNullTagArrayThrowsValidationFailedAsync`     | Fact | Tag with null tag array throws validation failed.    |
| `TagWithBlankTagNameThrowsValidationFailedAsync`     | Fact | Tag with blank tag name throws validation failed.    |
| `CreateWithNullUserNameThrowsNotAuthenticatedAsync`  | Fact | Create with null user name throws not authenticated. |
| `CreateWithUnknownTenantThrowsNotAuthenticatedAsync` | Fact | Create with unknown tenant throws not authenticated. |
| `TagPublishesUpdatedChangeEventAsync`                | Fact | Tag publishes updated change event.                  |
| `TagDoesNotPublishAChangeEventOnFailureAsync`        | Fact | Tag does not publish a change event on failure.      |

## Case

### CaseServiceTests

`Jube.Test.Service.Repository.Case.CaseServiceTests` — 16 tests. Category: Service. Collection: Database.

| Test                                                                  | Kind | Verifies                                                                 |
|-----------------------------------------------------------------------|------|--------------------------------------------------------------------------|
| `CreateWithNullUserNameThrowsNotAuthenticatedAsync`                   | Fact | Create with null user name throws not authenticated.                     |
| `CreateWithUnknownTenantThrowsNotAuthenticatedAsync`                  | Fact | Create with unknown tenant throws not authenticated.                     |
| `GetWithoutPermissionThrowsForbiddenAsync`                            | Fact | Get without permission throws forbidden.                                 |
| `ListIsScopedToCallersTenantAsync`                                    | Fact | List is scoped to callers tenant.                                        |
| `InsertPersistsAndDropsIdentityFieldsAsync`                           | Fact | Insert persists and drops identity fields.                               |
| `InsertWithInvalidRatingThrowsValidationFailedAsync`                  | Fact | Insert with invalid rating throws validation failed.                     |
| `CreateFromCaseKeyValueRaisesCaseFromArchiveAsync`                    | Fact | Create from case key value raises case from archive.                     |
| `CreateFromCaseKeyValueWithExistingOpenCaseThrowsConflictAsync`       | Fact | Create from case key value with existing open case throws conflict.      |
| `CreateFromCaseKeyValueWithUnknownWorkflowThrowsNotFoundAsync`        | Fact | Create from case key value with unknown workflow throws not found.       |
| `CreateFromCaseKeyValueWithNoMatchingArchiveThrowsNotFoundAsync`      | Fact | Create from case key value with no matching archive throws not found.    |
| `UpdateWithUnknownIdThrowsNotFoundAsync`                              | Fact | Update with unknown id throws not found.                                 |
| `UpdateLockingAnUnlockedCaseSetsLockedDateAndUserAsync`               | Fact | Update locking an unlocked case sets locked date and user.               |
| `UpdateUnlockingALockedCaseClearsLockedDateAsync`                     | Fact | Update unlocking a locked case clears locked date.                       |
| `UpdateWithUnknownWorkflowStatusThrowsInvalidCaseWorkflowStatusAsync` | Fact | Update with unknown workflow status throws invalid case workflow status. |
| `UpdateChangingRatingWritesCaseEventAndPublishesChangeAsync`          | Fact | Update changing rating writes case event and publishes change.           |
| `UpdateDoesNotPublishChangeOnValidationFailureAsync`                  | Fact | Update does not publish change on validation failure.                    |

## Case file

### CaseFileServiceTests

`Jube.Test.Service.Repository.CaseFile.CaseFileServiceTests` — 33 tests. Category: Service. Collection: Database.

| Test                                                                     | Kind   | Verifies                                                                          |
|--------------------------------------------------------------------------|--------|-----------------------------------------------------------------------------------|
| `CreateWithNullUserNameThrowsNotAuthenticatedAsync`                      | Fact   | Create with null user name throws not authenticated.                              |
| `CreateWithUnknownTenantThrowsNotAuthenticatedAsync`                     | Fact   | Create with unknown tenant throws not authenticated.                              |
| `UploadWithoutPermissionThrowsForbiddenAsync`                            | Fact   | Upload without permission throws forbidden.                                       |
| `RemoveWithoutPermissionThrowsForbiddenAsync`                            | Fact   | Remove without permission throws forbidden.                                       |
| `GetByCaseKeyValueWithoutPermissionThrowsForbiddenAsync`                 | Fact   | Get by case key value without permission throws forbidden.                        |
| `UploadPersistsFileAndReturnsDtoAsync`                                   | Fact   | Upload persists file and returns dto.                                             |
| `UploadSkipsEmptyFilesAndStoresFirstNonEmptyOnlyAsync`                   | Fact   | Upload skips empty files and stores first non empty only.                         |
| `UploadWithNoNonEmptyFileReturnsNullAndStoresNothingAsync`               | Fact   | Upload with no non empty file returns null and stores nothing.                    |
| `UploadWithBlankCaseKeyThrowsValidationFailedAsync`                      | Theory | Upload with blank case key throws validation failed.                              |
| `UploadWithBlankCaseKeyValueThrowsValidationFailedAsync`                 | Fact   | Upload with blank case key value throws validation failed.                        |
| `UploadWithInvalidCaseIdThrowsValidationFailedAsync`                     | Theory | Upload with invalid case id throws validation failed.                             |
| `UploadWithUnknownCaseThrowsNotVisibleAsync`                             | Fact   | Upload with unknown case throws not visible.                                      |
| `UploadOfCaseInAnotherTenantThrowsNotVisibleAsync`                       | Fact   | Upload of case in another tenant throws not visible.                              |
| `UploadDoesNotPublishChangeEventOnValidationFailureAsync`                | Fact   | Upload does not publish change event on validation failure.                       |
| `RemoveSoftDeletesFileAndPublishesDeletedChangeEventAsync`               | Fact   | Remove soft deletes file and publishes deleted change event.                      |
| `RemoveWithUnknownIdThrowsNotVisibleAsync`                               | Fact   | Remove with unknown id throws not visible.                                        |
| `RemoveOfFileInAnotherTenantThrowsNotVisibleAndLeavesRowUntouchedAsync`  | Fact   | Remove of file in another tenant throws not visible and leaves row untouched.     |
| `GenerateReturnsFileContentAsync`                                        | Fact   | Generate returns file content.                                                    |
| `GenerateWithUnknownIdThrowsNotVisibleAsync`                             | Fact   | Generate with unknown id throws not visible.                                      |
| `GenerateOfFileInAnotherTenantThrowsNotVisibleAsync`                     | Fact   | Generate of file in another tenant throws not visible.                            |
| `GetByCaseKeyValueReturnsMatchingFilesAsync`                             | Fact   | Get by case key value returns matching files.                                     |
| `GetByCaseKeyValueIsScopedToCallersTenantAsync`                          | Fact   | Get by case key value is scoped to callers tenant.                                |
| `GetByCaseKeyValueRequiresCaseWorkflowRoleNotJustTenantPermissionAsync`  | Fact   | Get by case key value requires case workflow role not just tenant permission.     |
| `GetByCaseKeyValueExcludesRemovedFilesAsync`                             | Fact   | Get by case key value excludes removed files.                                     |
| `GetByCaseKeyValueDoesNotPublishAChangeEventAsync`                       | Fact   | Get by case key value does not publish a change event.                            |
| `GetByCaseKeyValueWithCancelledTokenThrowsAsync`                         | Fact   | Get by case key value with cancelled token throws.                                |
| `UploadWithoutPermissionLogsWarnNotErrorAsync`                           | Fact   | Upload without permission logs warn not error.                                    |
| `UploadUnexpectedFailureLogsErrorWithExceptionAttachedAsync`             | Fact   | Upload unexpected failure logs error with exception attached.                     |
| `UploadSuccessLogsExactlyOneInfoEntryAsync`                              | Fact   | Upload success logs exactly one info entry.                                       |
| `GetByCaseKeyValueWithGatesDisabledRecordsNoEntriesAsync`                | Fact   | Get by case key value with gates disabled records no entries.                     |
| `GetByCaseKeyValueEmitsExactlyOneAuditRecordAsync`                       | Fact   | Get by case key value emits exactly one audit record.                             |
| `UploadWithBlankCaseKeyUsesFrenchMessageUnderFrenchCultureAsync`         | Fact   | Upload with blank case key uses french message under french culture.              |
| `ServiceToolCatalogueRegistersCaseFileOperationsWithGloballyUniqueNames` | Fact   | Service tool catalogue registers case file operations with globally unique names. |

## Case note

### CaseNoteServiceTests

`Jube.Test.Service.Repository.CaseNote.CaseNoteServiceTests` — 19 tests. Category: Service. Collection: Database.

| Test                                                        | Kind | Verifies                                                      |
|-------------------------------------------------------------|------|---------------------------------------------------------------|
| `CreateWithNullUserNameThrowsNotAuthenticatedAsync`         | Fact | Create with null user name throws not authenticated.          |
| `CreateWithUnknownTenantThrowsNotAuthenticatedAsync`        | Fact | Create with unknown tenant throws not authenticated.          |
| `InsertWithoutPermissionThrowsForbiddenAsync`               | Fact | Insert without permission throws forbidden.                   |
| `GetByCaseKeyValueWithoutPermissionThrowsForbiddenAsync`    | Fact | Get by case key value without permission throws forbidden.    |
| `InsertWithBlankNoteThrowsValidationFailedAsync`            | Fact | Insert with blank note throws validation failed.              |
| `InsertWithZeroActionIdThrowsValidationFailedAsync`         | Fact | Insert with zero action id throws validation failed.          |
| `InsertWithBlankPayloadThrowsValidationFailedAsync`         | Fact | Insert with blank payload throws validation failed.           |
| `InsertWithUnknownCaseIdThrowsNotFoundAsync`                | Fact | Insert with unknown case id throws not found.                 |
| `InsertWithUnknownActionIdThrowsNotFoundAsync`              | Fact | Insert with unknown action id throws not found.               |
| `InsertForCaseInAnotherTenantThrowsNotFoundAsync`           | Fact | Insert for case in another tenant throws not found.           |
| `InsertPersistsAndReturnsNoteWithServerAssignedFieldsAsync` | Fact | Insert persists and returns note with server assigned fields. |
| `InsertPublishesCreatedChangeEventAsync`                    | Fact | Insert publishes created change event.                        |
| `InsertDoesNotPublishChangeEventOnValidationFailureAsync`   | Fact | Insert does not publish change event on validation failure.   |
| `GetByCaseKeyValueReturnsNewestFirstAsync`                  | Fact | Get by case key value returns newest first.                   |
| `GetByCaseKeyValueIsScopedToCallersTenantAsync`             | Fact | Get by case key value is scoped to callers tenant.            |
| `GetByCaseKeyValueWithNoMatchesReturnsEmptyAsync`           | Fact | Get by case key value with no matches returns empty.          |
| `GetByCaseKeyValueHonoursCancellationAsync`                 | Fact | Get by case key value honours cancellation.                   |
| `InsertWarnsNotErrorsOnForbiddenAsync`                      | Fact | Insert warns not errors on forbidden.                         |
| `InsertLogsOneAuditRecordOnSuccessAsync`                    | Fact | Insert logs one audit record on success.                      |

## Case workflow

### CaseWorkflowServiceTests

`Jube.Test.Service.Repository.CaseWorkflow.CaseWorkflowServiceTests` — 56 tests. Category: Service. Collection:
Database.

| Test                                                                         | Kind   | Verifies                                                                              |
|------------------------------------------------------------------------------|--------|---------------------------------------------------------------------------------------|
| `CreateWithNullOrBlankUserNameThrowsNotAuthenticatedAsync`                   | Theory | Create with null or blank user name throws not authenticated.                         |
| `CreateWithUserHavingNoTenantThrowsNotAuthenticatedAsync`                    | Fact   | Create with user having no tenant throws not authenticated.                           |
| `CreateWithUnknownUserThrowsNotAuthenticatedAsync`                           | Fact   | Create with unknown user throws not authenticated.                                    |
| `ListWithoutPermissionThrowsForbiddenAsync`                                  | Fact   | List without permission throws forbidden.                                             |
| `GetByIdWithoutPermissionThrowsForbiddenAsync`                               | Fact   | Get by id without permission throws forbidden.                                        |
| `InsertWithoutPermissionThrowsForbiddenAndWritesNoRowAsync`                  | Fact   | Insert without permission throws forbidden and writes no row.                         |
| `UpdateWithoutPermissionThrowsForbiddenAsync`                                | Fact   | Update without permission throws forbidden.                                           |
| `DeleteWithoutPermissionThrowsForbiddenAsync`                                | Fact   | Delete without permission throws forbidden.                                           |
| `ListByModelActiveOnlyRequiresCaseWorkflowRoleNotJustTenantPermissionAsync`  | Fact   | List by model active only requires case workflow role not just tenant permission.     |
| `ListByModelActiveOnlyReturnsRowsOnceRoleIsGrantedAsync`                     | Fact   | List by model active only returns rows once role is granted.                          |
| `LandlordUserDoesNotAutomaticallySeeOtherTenantsRowsAsync`                   | Fact   | Landlord user does not automatically see other tenants rows.                          |
| `InsertPersistsAndReturnsVersionOneAsync`                                    | Fact   | Insert persists and returns version one.                                              |
| `GetByIdReturnsInsertedRowAsync`                                             | Fact   | Get by id returns inserted row.                                                       |
| `GetByEntityAnalysisModelIdReturnsRowsForThatModelOnlyAsync`                 | Fact   | Get by entity analysis model id returns rows for that model only.                     |
| `UpdateIncrementsVersionPreservesCreatedFieldsAndSetsUpdatedFieldsAsync`     | Fact   | Update increments version preserves created fields and sets updated fields.           |
| `UpdateWritesAuditRowAsync`                                                  | Fact   | Update writes audit row.                                                              |
| `DeleteSoftDeletesRowAsync`                                                  | Fact   | Delete soft deletes row.                                                              |
| `TenantBCannotGetByIdTenantARowAsync`                                        | Fact   | Tenant b cannot get by id tenant a row.                                               |
| `TenantBUpdateOfTenantARowThrowsNotFoundAndLeavesRowUntouchedAsync`          | Fact   | Tenant b update of tenant a row throws not found and leaves row untouched.            |
| `TenantBDeleteOfTenantARowThrowsNotFoundAndRowRemainsAsync`                  | Fact   | Tenant b delete of tenant a row throws not found and row remains.                     |
| `GetAllAsTenantBNeverContainsTenantARowsAsync`                               | Fact   | Get all as tenant b never contains tenant a rows.                                     |
| `InsertWithBlankNameThrowsValidationFailedAsync`                             | Theory | Insert with blank name throws validation failed.                                      |
| `InsertWithNameOverMaxLengthThrowsValidationFailedAsync`                     | Fact   | Insert with name over max length throws validation failed.                            |
| `InsertWithNameAtExactlyMaxLengthSucceedsAsync`                              | Fact   | Insert with name at exactly max length succeeds.                                      |
| `InsertWithUnicodeAndSqlLikeNamePersistsSafelyAsync`                         | Fact   | Insert with unicode and sql like name persists safely.                                |
| `InsertWithDuplicateNameInSameTenantAndModelThrowsValidationFailedAsync`     | Fact   | Insert with duplicate name in same tenant and model throws validation failed.         |
| `InsertWithSameNameDifferentCasingInSameTenantThrowsValidationFailedAsync`   | Fact   | Insert with same name different casing in same tenant throws validation failed.       |
| `InsertWithSameNameInDifferentTenantSucceedsAsync`                           | Fact   | Insert with same name in different tenant succeeds.                                   |
| `InsertWithNonPositiveEntityAnalysisModelIdThrowsValidationFailedAsync`      | Theory | Insert with non positive entity analysis model id throws validation failed.           |
| `InsertWithNonExistentEntityAnalysisModelIdThrowsValidationFailedAsync`      | Fact   | Insert with non existent entity analysis model id throws validation failed.           |
| `InsertWithEntityAnalysisModelIdInAnotherTenantThrowsValidationFailedAsync`  | Fact   | Insert with entity analysis model id in another tenant throws validation failed.      |
| `InsertWithVisualisationEnabledAndEmptyGuidThrowsValidationFailedAsync`      | Fact   | Insert with visualisation enabled and empty guid throws validation failed.            |
| `InsertTamperingIdentityAndAuditFieldsHasNoEffectAsync`                      | Fact   | Insert tampering identity and audit fields has no effect.                             |
| `UpdateTamperingIdentityAndAuditFieldsHasNoEffectAsync`                      | Fact   | Update tampering identity and audit fields has no effect.                             |
| `UpdateOfLockedRowThrowsNotFoundAsync`                                       | Fact   | Update of locked row throws not found.                                                |
| `UpdateOfSoftDeletedRowThrowsNotFoundAsync`                                  | Fact   | Update of soft deleted row throws not found.                                          |
| `UpdateWithUnknownIdThrowsNotFoundAsync`                                     | Fact   | Update with unknown id throws not found.                                              |
| `DeleteOfMissingRowThrowsNotFoundAsync`                                      | Fact   | Delete of missing row throws not found.                                               |
| `DeleteOfLockedRowThrowsNotFoundAsync`                                       | Fact   | Delete of locked row throws not found.                                                |
| `GetByIdForMissingRowReturnsNullAsync`                                       | Fact   | Get by id for missing row returns null.                                               |
| `ActiveAndLockedByteRoundTripAsync`                                          | Fact   | Active and locked byte round trip.                                                    |
| `GetWithCancelledTokenThrowsAsync`                                           | Fact   | Get with cancelled token throws.                                                      |
| `InsertPublishesCreatedChangeEventAsync`                                     | Fact   | Insert publishes created change event.                                                |
| `UpdatePublishesUpdatedChangeEventAsync`                                     | Fact   | Update publishes updated change event.                                                |
| `DeletePublishesDeletedChangeEventAsync`                                     | Fact   | Delete publishes deleted change event.                                                |
| `InsertDoesNotPublishChangeEventOnValidationFailureAsync`                    | Fact   | Insert does not publish change event on validation failure.                           |
| `ListDoesNotPublishAChangeEventAsync`                                        | Fact   | List does not publish a change event.                                                 |
| `InsertWithoutPermissionLogsWarnNotErrorAsync`                               | Fact   | Insert without permission logs warn not error.                                        |
| `InsertUnexpectedFailureLogsErrorWithExceptionAttachedAsync`                 | Fact   | Insert unexpected failure logs error with exception attached.                         |
| `InsertSuccessLogsExactlyOneInfoEntryAsync`                                  | Fact   | Insert success logs exactly one info entry.                                           |
| `ListWithGatesDisabledRecordsNoEntriesAsync`                                 | Fact   | List with gates disabled records no entries.                                          |
| `ListEmitsExactlyOneAuditRecordAsync`                                        | Fact   | List emits exactly one audit record.                                                  |
| `InsertEmitsExactlyOneAuditRecordWithEntityIdAsync`                          | Fact   | Insert emits exactly one audit record with entity id.                                 |
| `InsertWithBlankNameUsesFrenchMessageUnderFrenchCultureAsync`                | Fact   | Insert with blank name uses french message under french culture.                      |
| `ServiceToolCatalogueRegistersCaseWorkflowOperationsWithGloballyUniqueNames` | Fact   | Service tool catalogue registers case workflow operations with globally unique names. |
| `ListAsyncClampsTakeAndPaginatesAsync`                                       | Fact   | List async clamps take and paginates.                                                 |

## Case workflow action

### CaseWorkflowActionServiceTests

`Jube.Test.Service.Repository.CaseWorkflowAction.CaseWorkflowActionServiceTests` — 38 tests. Category: Service.
Collection: Database.

| Test                                                                          | Kind   | Verifies                                                                            |
|-------------------------------------------------------------------------------|--------|-------------------------------------------------------------------------------------|
| `CreateWithNullOrBlankUserNameThrowsNotAuthenticatedAsync`                    | Theory | Create with null or blank user name throws not authenticated.                       |
| `CreateWithUserHavingNoTenantThrowsNotAuthenticatedAsync`                     | Fact   | Create with user having no tenant throws not authenticated.                         |
| `ListWithoutPermissionThrowsForbiddenAsync`                                   | Fact   | List without permission throws forbidden.                                           |
| `GetByIdWithoutPermissionThrowsForbiddenAsync`                                | Fact   | Get by id without permission throws forbidden.                                      |
| `InsertWithoutPermissionThrowsForbiddenAndWritesNoRowAsync`                   | Fact   | Insert without permission throws forbidden and writes no row.                       |
| `UpdateWithoutPermissionThrowsForbiddenAsync`                                 | Fact   | Update without permission throws forbidden.                                         |
| `DeleteWithoutPermissionThrowsForbiddenAsync`                                 | Fact   | Delete without permission throws forbidden.                                         |
| `InsertPersistsAndDropsIdentityFieldsAsync`                                   | Fact   | Insert persists and drops identity fields.                                          |
| `UpdateIncrementsVersionPreservesCreatedFieldsAndSetsUpdatedFieldsAsync`      | Fact   | Update increments version preserves created fields and sets updated fields.         |
| `UpdateTamperingIdentityAndAuditFieldsHasNoEffectAsync`                       | Fact   | Update tampering identity and audit fields has no effect.                           |
| `UpdateWithUnknownIdThrowsNotFoundAsync`                                      | Fact   | Update with unknown id throws not found.                                            |
| `UpdateOfLockedRowThrowsNotFoundAsync`                                        | Fact   | Update of locked row throws not found.                                              |
| `DeleteRemovesRowAndSubsequentGetReturnsNullAsync`                            | Fact   | Delete removes row and subsequent get returns null.                                 |
| `DeleteOfMissingRowThrowsNotFoundAsync`                                       | Fact   | Delete of missing row throws not found.                                             |
| `ListIsScopedToCallersTenantAsync`                                            | Fact   | List is scoped to callers tenant.                                                   |
| `UpdateForRowInAnotherTenantThrowsValidationFailedOnTheReferentialCheckAsync` | Fact   | Update for row in another tenant throws validation failed on the referential check. |
| `InsertWithBlankNameThrowsValidationFailedAsync`                              | Fact   | Insert with blank name throws validation failed.                                    |
| `InsertWithOverLongNameThrowsValidationFailedAsync`                           | Fact   | Insert with over long name throws validation failed.                                |
| `InsertWithDuplicateNameInSameWorkflowThrowsValidationFailedAsync`            | Fact   | Insert with duplicate name in same workflow throws validation failed.               |
| `InsertWithNonExistentCaseWorkflowIdThrowsValidationFailedAsync`              | Fact   | Insert with non existent case workflow id throws validation failed.                 |
| `InsertWithEnableHttpEndpointAndNoUrlThrowsValidationFailedAsync`             | Fact   | Insert with enable http endpoint and no url throws validation failed.               |
| `InsertWithEnableHttpEndpointAndInvalidTypeThrowsValidationFailedAsync`       | Fact   | Insert with enable http endpoint and invalid type throws validation failed.         |
| `InsertWithEnableHttpEndpointAndValidDataSucceedsAsync`                       | Fact   | Insert with enable http endpoint and valid data succeeds.                           |
| `InsertWithEnableNotificationAndNoDestinationThrowsValidationFailedAsync`     | Fact   | Insert with enable notification and no destination throws validation failed.        |
| `InsertWithEnableNotificationAndInvalidTypeThrowsValidationFailedAsync`       | Fact   | Insert with enable notification and invalid type throws validation failed.          |
| `InsertWithEnableNotificationAndValidDataSucceedsAsync`                       | Fact   | Insert with enable notification and valid data succeeds.                            |
| `InsertIgnoresDescriptionFieldAsync`                                          | Fact   | Insert ignores description field.                                                   |
| `ListByWorkflowIncludesInactiveRowsAsync`                                     | Fact   | List by workflow includes inactive rows.                                            |
| `ListByWorkflowIdActiveOnlyRequiresBothWorkflowAndActionRoleGrantsAsync`      | Fact   | List by workflow id active only requires both workflow and action role grants.      |
| `ListByWorkflowGuidActiveOnlyReturnsRowsOnceBothRolesGrantedAsync`            | Fact   | List by workflow guid active only returns rows once both roles granted.             |
| `ListHonoursCancellationAsync`                                                | Fact   | List honours cancellation.                                                          |
| `InsertPublishesCreatedChangeEventAsync`                                      | Fact   | Insert publishes created change event.                                              |
| `InsertDoesNotPublishChangeEventOnValidationFailureAsync`                     | Fact   | Insert does not publish change event on validation failure.                         |
| `DeletePublishesDeletedChangeEventAsync`                                      | Fact   | Delete publishes deleted change event.                                              |
| `ListDoesNotPublishAChangeEventAsync`                                         | Fact   | List does not publish a change event.                                               |
| `InsertWarnsNotErrorsOnForbiddenAsync`                                        | Fact   | Insert warns not errors on forbidden.                                               |
| `InsertWritesOneAuditRecordOnSuccessAsync`                                    | Fact   | Insert writes one audit record on success.                                          |
| `CatalogueRegistersUniqueCaseWorkflowActionToolNames`                         | Fact   | Catalogue registers unique case workflow action tool names.                         |

## Case workflow action role

### CaseWorkflowActionRoleServiceTests

`Jube.Test.Service.Repository.CaseWorkflowActionRole.CaseWorkflowActionRoleServiceTests` — 31 tests. Category: Service.
Collection: Database.

| Test                                                                     | Kind   | Verifies                                                                     |
|--------------------------------------------------------------------------|--------|------------------------------------------------------------------------------|
| `CreateWithNullOrBlankUserNameThrowsNotAuthenticatedAsync`               | Theory | Create with null or blank user name throws not authenticated.                |
| `CreateWithUserHavingNoTenantThrowsNotAuthenticatedAsync`                | Fact   | Create with user having no tenant throws not authenticated.                  |
| `ListWithoutPermissionThrowsForbiddenAsync`                              | Fact   | List without permission throws forbidden.                                    |
| `InsertWithoutPermissionThrowsForbiddenAndWritesNoRowAsync`              | Fact   | Insert without permission throws forbidden and writes no row.                |
| `DeleteWithoutPermissionThrowsForbiddenAsync`                            | Fact   | Delete without permission throws forbidden.                                  |
| `InsertPersistsAndReturnsGrantAsync`                                     | Fact   | Insert persists and returns grant.                                           |
| `InsertWithEmptyCaseWorkflowActionGuidThrowsValidationFailedAsync`       | Fact   | Insert with empty case workflow action guid throws validation failed.        |
| `InsertWithUnknownCaseWorkflowActionGuidThrowsValidationFailedAsync`     | Fact   | Insert with unknown case workflow action guid throws validation failed.      |
| `InsertWithCrossTenantCaseWorkflowActionGuidThrowsValidationFailedAsync` | Fact   | Insert with cross tenant case workflow action guid throws validation failed. |
| `InsertWithEmptyRoleRegistryGuidThrowsValidationFailedAsync`             | Fact   | Insert with empty role registry guid throws validation failed.               |
| `InsertWithCrossTenantRoleRegistryGuidThrowsValidationFailedAsync`       | Fact   | Insert with cross tenant role registry guid throws validation failed.        |
| `InsertPublishesCreatedChangeEventAsync`                                 | Fact   | Insert publishes created change event.                                       |
| `InsertDoesNotPublishChangeEventOnValidationFailureAsync`                | Fact   | Insert does not publish change event on validation failure.                  |
| `ListByCaseWorkflowActionGuidReturnsGrantsAsync`                         | Fact   | List by case workflow action guid returns grants.                            |
| `ListForUnknownActionGuidReturnsEmptyAsync`                              | Fact   | List for unknown action guid returns empty.                                  |
| `ListIsScopedToCallersTenantAsync`                                       | Fact   | List is scoped to callers tenant.                                            |
| `ListExcludesDeletedGrantsAsync`                                         | Fact   | List excludes deleted grants.                                                |
| `DeleteSoftDeletesAndPublishesDeletedChangeEventAsync`                   | Fact   | Delete soft deletes and publishes deleted change event.                      |
| `DeleteOfMissingRowThrowsKeyNotFoundExceptionNotA404OrA204Async`         | Fact   | Delete of missing row throws key not found exception not a 404 or a 204.     |
| `DeleteOfCrossTenantRowThrowsKeyNotFoundExceptionAsync`                  | Fact   | Delete of cross tenant row throws key not found exception.                   |
| `DeleteDoesNotPublishChangeEventOnFailureAsync`                          | Fact   | Delete does not publish change event on failure.                             |
| `ListHonoursCancellationAsync`                                           | Fact   | List honours cancellation.                                                   |
| `InsertWarnsNotErrorsOnForbiddenAsync`                                   | Fact   | Insert warns not errors on forbidden.                                        |
| `InsertLogsOneAuditRecordOnSuccessAsync`                                 | Fact   | Insert logs one audit record on success.                                     |
| `ListLogsOneAuditRecordAsync`                                            | Fact   | List logs one audit record.                                                  |
| `CatalogueListsUniqueCaseWorkflowActionRoleToolNames`                    | Fact   | Catalogue lists unique case workflow action role tool names.                 |
| `RepeatedGrantReturnsTheExistingGrantAndWritesNoSecondRowAsync`          | Fact   | Repeated grant returns the existing grant and writes no second row.          |
| `RepeatedGrantAfterRevokeCreatesANewLiveGrantAsync`                      | Fact   | Repeated grant after revoke creates a new live grant.                        |
| `RepeatedGrantAndRevokeLeavesNoAccessBehindAsync`                        | Fact   | Repeated grant and revoke leaves no access behind.                           |
| `TheSameRoleOnTwoParentsIsTwoIndependentGrantsAsync`                     | Fact   | The same role on two parents is two independent grants.                      |
| `ConcurrentIdenticalGrantsProduceExactlyOneLiveRowAsync`                 | Fact   | Concurrent identical grants produce exactly one live row.                    |

## Case workflow display

### CaseWorkflowDisplayServiceTests

`Jube.Test.Service.Repository.CaseWorkflowDisplay.CaseWorkflowDisplayServiceTests` — 33 tests. Category: Service.
Collection: Database.

| Test                                                                          | Kind   | Verifies                                                                            |
|-------------------------------------------------------------------------------|--------|-------------------------------------------------------------------------------------|
| `CreateWithNullOrBlankUserNameThrowsNotAuthenticatedAsync`                    | Theory | Create with null or blank user name throws not authenticated.                       |
| `CreateWithUserHavingNoTenantThrowsNotAuthenticatedAsync`                     | Fact   | Create with user having no tenant throws not authenticated.                         |
| `ListWithoutPermissionThrowsForbiddenAsync`                                   | Fact   | List without permission throws forbidden.                                           |
| `GetByIdWithoutPermissionThrowsForbiddenAsync`                                | Fact   | Get by id without permission throws forbidden.                                      |
| `InsertWithoutPermissionThrowsForbiddenAndWritesNoRowAsync`                   | Fact   | Insert without permission throws forbidden and writes no row.                       |
| `UpdateWithoutPermissionThrowsForbiddenAsync`                                 | Fact   | Update without permission throws forbidden.                                         |
| `DeleteWithoutPermissionThrowsForbiddenAsync`                                 | Fact   | Delete without permission throws forbidden.                                         |
| `InsertPersistsAndDropsIdentityFieldsAsync`                                   | Fact   | Insert persists and drops identity fields.                                          |
| `UpdateIncrementsVersionPreservesCreatedFieldsAndSetsUpdatedFieldsAsync`      | Fact   | Update increments version preserves created fields and sets updated fields.         |
| `UpdateTamperingIdentityAndAuditFieldsHasNoEffectAsync`                       | Fact   | Update tampering identity and audit fields has no effect.                           |
| `UpdateWithUnknownIdThrowsNotFoundAsync`                                      | Fact   | Update with unknown id throws not found.                                            |
| `UpdateOfLockedRowThrowsNotFoundAsync`                                        | Fact   | Update of locked row throws not found.                                              |
| `DeleteRemovesRowAndSubsequentGetReturnsNullAsync`                            | Fact   | Delete removes row and subsequent get returns null.                                 |
| `DeleteOfMissingRowThrowsNotFoundAsync`                                       | Fact   | Delete of missing row throws not found.                                             |
| `ListIsScopedToCallersTenantAsync`                                            | Fact   | List is scoped to callers tenant.                                                   |
| `UpdateForRowInAnotherTenantThrowsValidationFailedOnTheReferentialCheckAsync` | Fact   | Update for row in another tenant throws validation failed on the referential check. |
| `InsertWithBlankNameThrowsValidationFailedAsync`                              | Fact   | Insert with blank name throws validation failed.                                    |
| `InsertWithOverLongNameThrowsValidationFailedAsync`                           | Fact   | Insert with over long name throws validation failed.                                |
| `InsertWithDuplicateNameInSameWorkflowThrowsValidationFailedAsync`            | Fact   | Insert with duplicate name in same workflow throws validation failed.               |
| `InsertWithNonExistentCaseWorkflowIdThrowsValidationFailedAsync`              | Fact   | Insert with non existent case workflow id throws validation failed.                 |
| `InsertWithBlankHtmlThrowsValidationFailedAsync`                              | Fact   | Insert with blank html throws validation failed.                                    |
| `InsertIgnoresDescriptionAndHttpNotificationFieldsAsync`                      | Fact   | Insert ignores description and http notification fields.                            |
| `ListByWorkflowIncludesInactiveRowsAsync`                                     | Fact   | List by workflow includes inactive rows.                                            |
| `ListByWorkflowIdActiveOnlyRequiresBothWorkflowAndDisplayRoleGrantsAsync`     | Fact   | List by workflow id active only requires both workflow and display role grants.     |
| `ListByWorkflowGuidActiveOnlyReturnsRowsOnceBothRolesGrantedAsync`            | Fact   | List by workflow guid active only returns rows once both roles granted.             |
| `ListHonoursCancellationAsync`                                                | Fact   | List honours cancellation.                                                          |
| `InsertPublishesCreatedChangeEventAsync`                                      | Fact   | Insert publishes created change event.                                              |
| `InsertDoesNotPublishChangeEventOnValidationFailureAsync`                     | Fact   | Insert does not publish change event on validation failure.                         |
| `DeletePublishesDeletedChangeEventAsync`                                      | Fact   | Delete publishes deleted change event.                                              |
| `ListDoesNotPublishAChangeEventAsync`                                         | Fact   | List does not publish a change event.                                               |
| `InsertWarnsNotErrorsOnForbiddenAsync`                                        | Fact   | Insert warns not errors on forbidden.                                               |
| `InsertWritesOneAuditRecordOnSuccessAsync`                                    | Fact   | Insert writes one audit record on success.                                          |
| `CatalogueRegistersUniqueCaseWorkflowDisplayToolNames`                        | Fact   | Catalogue registers unique case workflow display tool names.                        |

## Case workflow display role

### CaseWorkflowDisplayRoleServiceTests

`Jube.Test.Service.Repository.CaseWorkflowDisplayRole.CaseWorkflowDisplayRoleServiceTests` — 31 tests. Category:
Service. Collection: Database.

| Test                                                                      | Kind   | Verifies                                                                      |
|---------------------------------------------------------------------------|--------|-------------------------------------------------------------------------------|
| `CreateWithNullOrBlankUserNameThrowsNotAuthenticatedAsync`                | Theory | Create with null or blank user name throws not authenticated.                 |
| `CreateWithUserHavingNoTenantThrowsNotAuthenticatedAsync`                 | Fact   | Create with user having no tenant throws not authenticated.                   |
| `ListWithoutPermissionThrowsForbiddenAsync`                               | Fact   | List without permission throws forbidden.                                     |
| `InsertWithoutPermissionThrowsForbiddenAndWritesNoRowAsync`               | Fact   | Insert without permission throws forbidden and writes no row.                 |
| `DeleteWithoutPermissionThrowsForbiddenAsync`                             | Fact   | Delete without permission throws forbidden.                                   |
| `InsertPersistsAndReturnsGrantAsync`                                      | Fact   | Insert persists and returns grant.                                            |
| `InsertWithEmptyCaseWorkflowDisplayGuidThrowsValidationFailedAsync`       | Fact   | Insert with empty case workflow display guid throws validation failed.        |
| `InsertWithUnknownCaseWorkflowDisplayGuidThrowsValidationFailedAsync`     | Fact   | Insert with unknown case workflow display guid throws validation failed.      |
| `InsertWithCrossTenantCaseWorkflowDisplayGuidThrowsValidationFailedAsync` | Fact   | Insert with cross tenant case workflow display guid throws validation failed. |
| `InsertWithEmptyRoleRegistryGuidThrowsValidationFailedAsync`              | Fact   | Insert with empty role registry guid throws validation failed.                |
| `InsertWithCrossTenantRoleRegistryGuidThrowsValidationFailedAsync`        | Fact   | Insert with cross tenant role registry guid throws validation failed.         |
| `InsertPublishesCreatedChangeEventAsync`                                  | Fact   | Insert publishes created change event.                                        |
| `InsertDoesNotPublishChangeEventOnValidationFailureAsync`                 | Fact   | Insert does not publish change event on validation failure.                   |
| `ListByCaseWorkflowDisplayGuidReturnsGrantsAsync`                         | Fact   | List by case workflow display guid returns grants.                            |
| `ListForUnknownDisplayGuidReturnsEmptyAsync`                              | Fact   | List for unknown display guid returns empty.                                  |
| `ListIsScopedToCallersTenantAsync`                                        | Fact   | List is scoped to callers tenant.                                             |
| `ListExcludesDeletedGrantsAsync`                                          | Fact   | List excludes deleted grants.                                                 |
| `DeleteSoftDeletesAndPublishesDeletedChangeEventAsync`                    | Fact   | Delete soft deletes and publishes deleted change event.                       |
| `DeleteOfMissingRowThrowsKeyNotFoundExceptionNotA404OrA204Async`          | Fact   | Delete of missing row throws key not found exception not a 404 or a 204.      |
| `DeleteOfCrossTenantRowThrowsKeyNotFoundExceptionAsync`                   | Fact   | Delete of cross tenant row throws key not found exception.                    |
| `DeleteDoesNotPublishChangeEventOnFailureAsync`                           | Fact   | Delete does not publish change event on failure.                              |
| `ListHonoursCancellationAsync`                                            | Fact   | List honours cancellation.                                                    |
| `InsertWarnsNotErrorsOnForbiddenAsync`                                    | Fact   | Insert warns not errors on forbidden.                                         |
| `InsertLogsOneAuditRecordOnSuccessAsync`                                  | Fact   | Insert logs one audit record on success.                                      |
| `ListLogsOneAuditRecordAsync`                                             | Fact   | List logs one audit record.                                                   |
| `CatalogueListsUniqueCaseWorkflowDisplayRoleToolNames`                    | Fact   | Catalogue lists unique case workflow display role tool names.                 |
| `RepeatedGrantReturnsTheExistingGrantAndWritesNoSecondRowAsync`           | Fact   | Repeated grant returns the existing grant and writes no second row.           |
| `RepeatedGrantAfterRevokeCreatesANewLiveGrantAsync`                       | Fact   | Repeated grant after revoke creates a new live grant.                         |
| `RepeatedGrantAndRevokeLeavesNoAccessBehindAsync`                         | Fact   | Repeated grant and revoke leaves no access behind.                            |
| `TheSameRoleOnTwoParentsIsTwoIndependentGrantsAsync`                      | Fact   | The same role on two parents is two independent grants.                       |
| `ConcurrentIdenticalGrantsProduceExactlyOneLiveRowAsync`                  | Fact   | Concurrent identical grants produce exactly one live row.                     |

## Case workflow filter

### CaseWorkflowFilterServiceTests

`Jube.Test.Service.Repository.CaseWorkflowFilter.CaseWorkflowFilterServiceTests` — 23 tests. Category: Service.
Collection: Database.

| Test                                                                     | Kind   | Verifies                                                                       |
|--------------------------------------------------------------------------|--------|--------------------------------------------------------------------------------|
| `CreateWithNullOrBlankUserNameThrowsNotAuthenticatedAsync`               | Theory | Create with null or blank user name throws not authenticated.                  |
| `CreateWithUserHavingNoTenantThrowsNotAuthenticatedAsync`                | Fact   | Create with user having no tenant throws not authenticated.                    |
| `ListWithoutPermissionThrowsForbiddenAsync`                              | Fact   | List without permission throws forbidden.                                      |
| `InsertWithoutPermissionThrowsForbiddenAndWritesNoRowAsync`              | Fact   | Insert without permission throws forbidden and writes no row.                  |
| `InsertPersistsAndReturnsVersionOneWithAuditFieldsAsync`                 | Fact   | Insert persists and returns version one with audit fields.                     |
| `InsertWithBlankNameThrowsValidationFailedAsync`                         | Fact   | Insert with blank name throws validation failed.                               |
| `InsertWithUnknownCaseWorkflowIdThrowsValidationFailedAsync`             | Fact   | Insert with unknown case workflow id throws validation failed.                 |
| `InsertWithBlankSelectJsonThrowsValidationFailedAsync`                   | Fact   | Insert with blank select json throws validation failed.                        |
| `InsertWithDuplicateNameInSameWorkflowThrowsValidationFailedAsync`       | Fact   | Insert with duplicate name in same workflow throws validation failed.          |
| `InsertIgnoresDescriptionAndHttpNotificationFieldsAsync`                 | Fact   | Insert ignores description and http notification fields.                       |
| `UpdatePreservesCreatedFieldsAndSetsUpdatedFieldsAsync`                  | Fact   | Update preserves created fields and sets updated fields.                       |
| `UpdatePreservesImportIdAsync`                                           | Fact   | Update preserves import id.                                                    |
| `UpdateOfUnknownIdThrowsNotFoundAsync`                                   | Fact   | Update of unknown id throws not found.                                         |
| `UpdateOfCrossTenantRowThrowsNotFoundAsync`                              | Fact   | Update of cross tenant row throws not found.                                   |
| `UpdateWithCaseWorkflowIdFromAnotherTenantThrowsValidationFailedAsync`   | Fact   | Update with case workflow id from another tenant throws validation failed.     |
| `DeleteSoftDeletesAndPublishesDeletedChangeEventAsync`                   | Fact   | Delete soft deletes and publishes deleted change event.                        |
| `DeleteOfUnknownIdThrowsNotFoundAsync`                                   | Fact   | Delete of unknown id throws not found.                                         |
| `ListByWorkflowIdIsScopedToCallersTenantAsync`                           | Fact   | List by workflow id is scoped to callers tenant.                               |
| `ListByWorkflowIdActiveOnlyRequiresBothWorkflowAndFilterRoleGrantsAsync` | Fact   | List by workflow id active only requires both workflow and filter role grants. |
| `GetByGuidReturnsMatchingFilterAsync`                                    | Fact   | Get by guid returns matching filter.                                           |
| `ListHonoursCancellationAsync`                                           | Fact   | List honours cancellation.                                                     |
| `ListDoesNotPublishAChangeEventAsync`                                    | Fact   | List does not publish a change event.                                          |
| `CatalogueRegistersExpectedToolNamesForThisArea`                         | Fact   | Catalogue registers expected tool names for this area.                         |

## Case workflow filter role

### CaseWorkflowFilterRoleServiceTests

`Jube.Test.Service.Repository.CaseWorkflowFilterRole.CaseWorkflowFilterRoleServiceTests` — 31 tests. Category: Service.
Collection: Database.

| Test                                                                     | Kind   | Verifies                                                                     |
|--------------------------------------------------------------------------|--------|------------------------------------------------------------------------------|
| `CreateWithNullOrBlankUserNameThrowsNotAuthenticatedAsync`               | Theory | Create with null or blank user name throws not authenticated.                |
| `CreateWithUserHavingNoTenantThrowsNotAuthenticatedAsync`                | Fact   | Create with user having no tenant throws not authenticated.                  |
| `ListWithoutPermissionThrowsForbiddenAsync`                              | Fact   | List without permission throws forbidden.                                    |
| `InsertWithoutPermissionThrowsForbiddenAndWritesNoRowAsync`              | Fact   | Insert without permission throws forbidden and writes no row.                |
| `DeleteWithoutPermissionThrowsForbiddenAsync`                            | Fact   | Delete without permission throws forbidden.                                  |
| `InsertPersistsAndReturnsGrantAsync`                                     | Fact   | Insert persists and returns grant.                                           |
| `InsertWithEmptyCaseWorkflowFilterGuidThrowsValidationFailedAsync`       | Fact   | Insert with empty case workflow filter guid throws validation failed.        |
| `InsertWithUnknownCaseWorkflowFilterGuidThrowsValidationFailedAsync`     | Fact   | Insert with unknown case workflow filter guid throws validation failed.      |
| `InsertWithCrossTenantCaseWorkflowFilterGuidThrowsValidationFailedAsync` | Fact   | Insert with cross tenant case workflow filter guid throws validation failed. |
| `InsertWithEmptyRoleRegistryGuidThrowsValidationFailedAsync`             | Fact   | Insert with empty role registry guid throws validation failed.               |
| `InsertWithCrossTenantRoleRegistryGuidThrowsValidationFailedAsync`       | Fact   | Insert with cross tenant role registry guid throws validation failed.        |
| `InsertPublishesCreatedChangeEventAsync`                                 | Fact   | Insert publishes created change event.                                       |
| `InsertDoesNotPublishChangeEventOnValidationFailureAsync`                | Fact   | Insert does not publish change event on validation failure.                  |
| `ListByCaseWorkflowFilterGuidReturnsGrantsAsync`                         | Fact   | List by case workflow filter guid returns grants.                            |
| `ListForUnknownFilterGuidReturnsEmptyAsync`                              | Fact   | List for unknown filter guid returns empty.                                  |
| `ListIsScopedToCallersTenantAsync`                                       | Fact   | List is scoped to callers tenant.                                            |
| `ListExcludesDeletedGrantsAsync`                                         | Fact   | List excludes deleted grants.                                                |
| `DeleteSoftDeletesAndPublishesDeletedChangeEventAsync`                   | Fact   | Delete soft deletes and publishes deleted change event.                      |
| `DeleteOfMissingRowThrowsKeyNotFoundExceptionNotA404OrA204Async`         | Fact   | Delete of missing row throws key not found exception not a 404 or a 204.     |
| `DeleteOfCrossTenantRowThrowsKeyNotFoundExceptionAsync`                  | Fact   | Delete of cross tenant row throws key not found exception.                   |
| `DeleteDoesNotPublishChangeEventOnFailureAsync`                          | Fact   | Delete does not publish change event on failure.                             |
| `ListHonoursCancellationAsync`                                           | Fact   | List honours cancellation.                                                   |
| `InsertWarnsNotErrorsOnForbiddenAsync`                                   | Fact   | Insert warns not errors on forbidden.                                        |
| `InsertLogsOneAuditRecordOnSuccessAsync`                                 | Fact   | Insert logs one audit record on success.                                     |
| `ListLogsOneAuditRecordAsync`                                            | Fact   | List logs one audit record.                                                  |
| `CatalogueListsUniqueCaseWorkflowFilterRoleToolNames`                    | Fact   | Catalogue lists unique case workflow filter role tool names.                 |
| `RepeatedGrantReturnsTheExistingGrantAndWritesNoSecondRowAsync`          | Fact   | Repeated grant returns the existing grant and writes no second row.          |
| `RepeatedGrantAfterRevokeCreatesANewLiveGrantAsync`                      | Fact   | Repeated grant after revoke creates a new live grant.                        |
| `RepeatedGrantAndRevokeLeavesNoAccessBehindAsync`                        | Fact   | Repeated grant and revoke leaves no access behind.                           |
| `TheSameRoleOnTwoParentsIsTwoIndependentGrantsAsync`                     | Fact   | The same role on two parents is two independent grants.                      |
| `ConcurrentIdenticalGrantsProduceExactlyOneLiveRowAsync`                 | Fact   | Concurrent identical grants produce exactly one live row.                    |

## Case workflow form

### CaseWorkflowFormServiceTests

`Jube.Test.Service.Repository.CaseWorkflowForm.CaseWorkflowFormServiceTests` — 28 tests. Category: Service. Collection:
Database.

| Test                                                                      | Kind   | Verifies                                                                     |
|---------------------------------------------------------------------------|--------|------------------------------------------------------------------------------|
| `CreateWithNullOrBlankUserNameThrowsNotAuthenticatedAsync`                | Theory | Create with null or blank user name throws not authenticated.                |
| `CreateWithUserHavingNoTenantThrowsNotAuthenticatedAsync`                 | Fact   | Create with user having no tenant throws not authenticated.                  |
| `ListWithoutPermissionThrowsForbiddenAsync`                               | Fact   | List without permission throws forbidden.                                    |
| `InsertWithoutPermissionThrowsForbiddenAndWritesNoRowAsync`               | Fact   | Insert without permission throws forbidden and writes no row.                |
| `InsertPersistsAndReturnsVersionOneWithAuditFieldsAsync`                  | Fact   | Insert persists and returns version one with audit fields.                   |
| `InsertWithBlankNameThrowsValidationFailedAsync`                          | Fact   | Insert with blank name throws validation failed.                             |
| `InsertWithUnknownCaseWorkflowIdThrowsValidationFailedAsync`              | Fact   | Insert with unknown case workflow id throws validation failed.               |
| `InsertWithBlankHtmlThrowsValidationFailedAsync`                          | Fact   | Insert with blank html throws validation failed.                             |
| `InsertWithDuplicateNameInSameWorkflowThrowsValidationFailedAsync`        | Fact   | Insert with duplicate name in same workflow throws validation failed.        |
| `InsertIgnoresDescriptionFieldAsync`                                      | Fact   | Insert ignores description field.                                            |
| `InsertWithEnableHttpEndpointAndNoUrlThrowsValidationFailedAsync`         | Fact   | Insert with enable http endpoint and no url throws validation failed.        |
| `InsertWithEnableHttpEndpointAndInvalidTypeThrowsValidationFailedAsync`   | Fact   | Insert with enable http endpoint and invalid type throws validation failed.  |
| `InsertWithEnableHttpEndpointAndValidDataSucceedsAsync`                   | Fact   | Insert with enable http endpoint and valid data succeeds.                    |
| `InsertWithEnableNotificationAndNoDestinationThrowsValidationFailedAsync` | Fact   | Insert with enable notification and no destination throws validation failed. |
| `InsertWithEnableNotificationAndInvalidTypeThrowsValidationFailedAsync`   | Fact   | Insert with enable notification and invalid type throws validation failed.   |
| `InsertWithEnableNotificationAndValidDataSucceedsAsync`                   | Fact   | Insert with enable notification and valid data succeeds.                     |
| `UpdatePreservesCreatedFieldsAndSetsUpdatedFieldsAsync`                   | Fact   | Update preserves created fields and sets updated fields.                     |
| `UpdatePreservesImportIdAsync`                                            | Fact   | Update preserves import id.                                                  |
| `UpdateOfUnknownIdThrowsNotFoundAsync`                                    | Fact   | Update of unknown id throws not found.                                       |
| `UpdateOfCrossTenantRowThrowsNotFoundAsync`                               | Fact   | Update of cross tenant row throws not found.                                 |
| `UpdateWithCaseWorkflowIdFromAnotherTenantThrowsValidationFailedAsync`    | Fact   | Update with case workflow id from another tenant throws validation failed.   |
| `DeleteSoftDeletesAndPublishesDeletedChangeEventAsync`                    | Fact   | Delete soft deletes and publishes deleted change event.                      |
| `DeleteOfUnknownIdThrowsNotFoundAsync`                                    | Fact   | Delete of unknown id throws not found.                                       |
| `ListByWorkflowIdIsScopedToCallersTenantAsync`                            | Fact   | List by workflow id is scoped to callers tenant.                             |
| `ListByWorkflowIdActiveOnlyRequiresBothWorkflowAndFormRoleGrantsAsync`    | Fact   | List by workflow id active only requires both workflow and form role grants. |
| `ListHonoursCancellationAsync`                                            | Fact   | List honours cancellation.                                                   |
| `ListDoesNotPublishAChangeEventAsync`                                     | Fact   | List does not publish a change event.                                        |
| `CatalogueRegistersExpectedToolNamesForThisArea`                          | Fact   | Catalogue registers expected tool names for this area.                       |

## Case workflow form entry

### CaseWorkflowFormEntryServiceTests

`Jube.Test.Service.Repository.CaseWorkflowFormEntry.CaseWorkflowFormEntryServiceTests` — 21 tests. Category: Service.
Collection: Database.

| Test                                                          | Kind | Verifies                                                         |
|---------------------------------------------------------------|------|------------------------------------------------------------------|
| `CreateWithNullUserNameThrowsNotAuthenticatedAsync`           | Fact | Create with null user name throws not authenticated.             |
| `CreateWithUnknownTenantThrowsNotAuthenticatedAsync`          | Fact | Create with unknown tenant throws not authenticated.             |
| `InsertWithoutPermissionThrowsForbiddenAsync`                 | Fact | Insert without permission throws forbidden.                      |
| `GetByCaseKeyValueWithoutPermissionThrowsForbiddenAsync`      | Fact | Get by case key value without permission throws forbidden.       |
| `InsertWithZeroCaseWorkflowFormIdThrowsValidationFailedAsync` | Fact | Insert with zero case workflow form id throws validation failed. |
| `InsertWithEmptyPayloadThrowsValidationFailedAsync`           | Fact | Insert with empty payload throws validation failed.              |
| `InsertWithZeroCaseIdThrowsValidationFailedAsync`             | Fact | Insert with zero case id throws validation failed.               |
| `InsertWithUnknownCaseIdThrowsNotFoundAsync`                  | Fact | Insert with unknown case id throws not found.                    |
| `InsertWithUnknownCaseWorkflowFormIdThrowsNotFoundAsync`      | Fact | Insert with unknown case workflow form id throws not found.      |
| `InsertForCaseInAnotherTenantThrowsNotFoundAsync`             | Fact | Insert for case in another tenant throws not found.              |
| `InsertPersistsEntryAndValueRowsAsync`                        | Fact | Insert persists entry and value rows.                            |
| `InsertSkipsNullPayloadValuesAsync`                           | Fact | Insert skips null payload values.                                |
| `InsertPublishesCreatedChangeEventAsync`                      | Fact | Insert publishes created change event.                           |
| `InsertDoesNotPublishChangeEventOnValidationFailureAsync`     | Fact | Insert does not publish change event on validation failure.      |
| `GetByCaseKeyValueReturnsNewestFirstAsync`                    | Fact | Get by case key value returns newest first.                      |
| `GetByCaseKeyValueIsScopedToCallersTenantAsync`               | Fact | Get by case key value is scoped to callers tenant.               |
| `GetByCaseKeyValueWithNoMatchesReturnsEmptyAsync`             | Fact | Get by case key value with no matches returns empty.             |
| `GetByCaseKeyValueHonoursCancellationAsync`                   | Fact | Get by case key value honours cancellation.                      |
| `InsertWarnsNotErrorsOnForbiddenAsync`                        | Fact | Insert warns not errors on forbidden.                            |
| `InsertLogsOneAuditRecordOnSuccessAsync`                      | Fact | Insert logs one audit record on success.                         |
| `CatalogueRegistersExpectedToolNames`                         | Fact | Catalogue registers expected tool names.                         |

## Case workflow form entry value

### CaseWorkflowFormEntryValueServiceTests

`Jube.Test.Service.Repository.CaseWorkflowFormEntryValue.CaseWorkflowFormEntryValueServiceTests` — 12 tests. Category:
Service. Collection: Database.

| Test                                                                | Kind | Verifies                                                                |
|---------------------------------------------------------------------|------|-------------------------------------------------------------------------|
| `CreateWithNullUserNameThrowsNotAuthenticatedAsync`                 | Fact | Create with null user name throws not authenticated.                    |
| `CreateWithUnknownTenantThrowsNotAuthenticatedAsync`                | Fact | Create with unknown tenant throws not authenticated.                    |
| `GetByCaseWorkflowFormEntryIdWithoutPermissionThrowsForbiddenAsync` | Fact | Get by case workflow form entry id without permission throws forbidden. |
| `ListReturnsValuesNewestFirstAsync`                                 | Fact | List returns values newest first.                                       |
| `ListRequiresBothCaseWorkflowAndCaseWorkflowStatusRoleGrantsAsync`  | Fact | List requires both case workflow and case workflow status role grants.  |
| `ListIsScopedToCallersTenantAsync`                                  | Fact | List is scoped to callers tenant.                                       |
| `ListForUnknownEntryIdReturnsEmptyAsync`                            | Fact | List for unknown entry id returns empty.                                |
| `ListHonoursCancellationAsync`                                      | Fact | List honours cancellation.                                              |
| `ListDoesNotPublishAChangeEventAsync`                               | Fact | List does not publish a change event.                                   |
| `ListWarnsNotErrorsOnForbiddenAsync`                                | Fact | List warns not errors on forbidden.                                     |
| `ListLogsOneAuditRecordOnSuccessAsync`                              | Fact | List logs one audit record on success.                                  |
| `CatalogueRegistersExpectedToolNames`                               | Fact | Catalogue registers expected tool names.                                |

## Case workflow form role

### CaseWorkflowFormRoleServiceTests

`Jube.Test.Service.Repository.CaseWorkflowFormRole.CaseWorkflowFormRoleServiceTests` — 31 tests. Category: Service.
Collection: Database.

| Test                                                                   | Kind   | Verifies                                                                   |
|------------------------------------------------------------------------|--------|----------------------------------------------------------------------------|
| `CreateWithNullOrBlankUserNameThrowsNotAuthenticatedAsync`             | Theory | Create with null or blank user name throws not authenticated.              |
| `CreateWithUserHavingNoTenantThrowsNotAuthenticatedAsync`              | Fact   | Create with user having no tenant throws not authenticated.                |
| `ListWithoutPermissionThrowsForbiddenAsync`                            | Fact   | List without permission throws forbidden.                                  |
| `InsertWithoutPermissionThrowsForbiddenAndWritesNoRowAsync`            | Fact   | Insert without permission throws forbidden and writes no row.              |
| `DeleteWithoutPermissionThrowsForbiddenAsync`                          | Fact   | Delete without permission throws forbidden.                                |
| `InsertPersistsAndReturnsGrantAsync`                                   | Fact   | Insert persists and returns grant.                                         |
| `InsertWithEmptyCaseWorkflowFormGuidThrowsValidationFailedAsync`       | Fact   | Insert with empty case workflow form guid throws validation failed.        |
| `InsertWithUnknownCaseWorkflowFormGuidThrowsValidationFailedAsync`     | Fact   | Insert with unknown case workflow form guid throws validation failed.      |
| `InsertWithCrossTenantCaseWorkflowFormGuidThrowsValidationFailedAsync` | Fact   | Insert with cross tenant case workflow form guid throws validation failed. |
| `InsertWithEmptyRoleRegistryGuidThrowsValidationFailedAsync`           | Fact   | Insert with empty role registry guid throws validation failed.             |
| `InsertWithCrossTenantRoleRegistryGuidThrowsValidationFailedAsync`     | Fact   | Insert with cross tenant role registry guid throws validation failed.      |
| `InsertPublishesCreatedChangeEventAsync`                               | Fact   | Insert publishes created change event.                                     |
| `InsertDoesNotPublishChangeEventOnValidationFailureAsync`              | Fact   | Insert does not publish change event on validation failure.                |
| `ListByCaseWorkflowFormGuidReturnsGrantsAsync`                         | Fact   | List by case workflow form guid returns grants.                            |
| `ListForUnknownFormGuidReturnsEmptyAsync`                              | Fact   | List for unknown form guid returns empty.                                  |
| `ListIsScopedToCallersTenantAsync`                                     | Fact   | List is scoped to callers tenant.                                          |
| `ListExcludesDeletedGrantsAsync`                                       | Fact   | List excludes deleted grants.                                              |
| `DeleteSoftDeletesAndPublishesDeletedChangeEventAsync`                 | Fact   | Delete soft deletes and publishes deleted change event.                    |
| `DeleteOfMissingRowThrowsKeyNotFoundExceptionNotA404OrA204Async`       | Fact   | Delete of missing row throws key not found exception not a 404 or a 204.   |
| `DeleteOfCrossTenantRowThrowsKeyNotFoundExceptionAsync`                | Fact   | Delete of cross tenant row throws key not found exception.                 |
| `DeleteDoesNotPublishChangeEventOnFailureAsync`                        | Fact   | Delete does not publish change event on failure.                           |
| `ListHonoursCancellationAsync`                                         | Fact   | List honours cancellation.                                                 |
| `InsertWarnsNotErrorsOnForbiddenAsync`                                 | Fact   | Insert warns not errors on forbidden.                                      |
| `InsertLogsOneAuditRecordOnSuccessAsync`                               | Fact   | Insert logs one audit record on success.                                   |
| `ListLogsOneAuditRecordAsync`                                          | Fact   | List logs one audit record.                                                |
| `CatalogueListsUniqueCaseWorkflowFormRoleToolNames`                    | Fact   | Catalogue lists unique case workflow form role tool names.                 |
| `RepeatedGrantReturnsTheExistingGrantAndWritesNoSecondRowAsync`        | Fact   | Repeated grant returns the existing grant and writes no second row.        |
| `RepeatedGrantAfterRevokeCreatesANewLiveGrantAsync`                    | Fact   | Repeated grant after revoke creates a new live grant.                      |
| `RepeatedGrantAndRevokeLeavesNoAccessBehindAsync`                      | Fact   | Repeated grant and revoke leaves no access behind.                         |
| `TheSameRoleOnTwoParentsIsTwoIndependentGrantsAsync`                   | Fact   | The same role on two parents is two independent grants.                    |
| `ConcurrentIdenticalGrantsProduceExactlyOneLiveRowAsync`               | Fact   | Concurrent identical grants produce exactly one live row.                  |

## Case workflow macro

### CaseWorkflowMacroServiceTests

`Jube.Test.Service.Repository.CaseWorkflowMacro.CaseWorkflowMacroServiceTests` — 28 tests. Category: Service.
Collection: Database.

| Test                                                                      | Kind   | Verifies                                                                     |
|---------------------------------------------------------------------------|--------|------------------------------------------------------------------------------|
| `CreateWithNullOrBlankUserNameThrowsNotAuthenticatedAsync`                | Theory | Create with null or blank user name throws not authenticated.                |
| `CreateWithUserHavingNoTenantThrowsNotAuthenticatedAsync`                 | Fact   | Create with user having no tenant throws not authenticated.                  |
| `ListWithoutPermissionThrowsForbiddenAsync`                               | Fact   | List without permission throws forbidden.                                    |
| `InsertWithoutPermissionThrowsForbiddenAndWritesNoRowAsync`               | Fact   | Insert without permission throws forbidden and writes no row.                |
| `InsertPersistsAndReturnsVersionOneWithAuditFieldsAsync`                  | Fact   | Insert persists and returns version one with audit fields.                   |
| `InsertWithBlankNameThrowsValidationFailedAsync`                          | Fact   | Insert with blank name throws validation failed.                             |
| `InsertWithUnknownCaseWorkflowIdThrowsValidationFailedAsync`              | Fact   | Insert with unknown case workflow id throws validation failed.               |
| `InsertWithBlankJavascriptThrowsValidationFailedAsync`                    | Fact   | Insert with blank javascript throws validation failed.                       |
| `InsertWithDuplicateNameInSameWorkflowThrowsValidationFailedAsync`        | Fact   | Insert with duplicate name in same workflow throws validation failed.        |
| `InsertIgnoresDescriptionFieldAsync`                                      | Fact   | Insert ignores description field.                                            |
| `InsertWithEnableHttpEndpointAndNoUrlThrowsValidationFailedAsync`         | Fact   | Insert with enable http endpoint and no url throws validation failed.        |
| `InsertWithEnableHttpEndpointAndInvalidTypeThrowsValidationFailedAsync`   | Fact   | Insert with enable http endpoint and invalid type throws validation failed.  |
| `InsertWithEnableHttpEndpointAndValidDataSucceedsAsync`                   | Fact   | Insert with enable http endpoint and valid data succeeds.                    |
| `InsertWithEnableNotificationAndNoDestinationThrowsValidationFailedAsync` | Fact   | Insert with enable notification and no destination throws validation failed. |
| `InsertWithEnableNotificationAndInvalidTypeThrowsValidationFailedAsync`   | Fact   | Insert with enable notification and invalid type throws validation failed.   |
| `InsertWithEnableNotificationAndValidDataSucceedsAsync`                   | Fact   | Insert with enable notification and valid data succeeds.                     |
| `UpdatePreservesCreatedFieldsAndSetsUpdatedFieldsAsync`                   | Fact   | Update preserves created fields and sets updated fields.                     |
| `UpdatePreservesImportIdAsync`                                            | Fact   | Update preserves import id.                                                  |
| `UpdateOfUnknownIdThrowsNotFoundAsync`                                    | Fact   | Update of unknown id throws not found.                                       |
| `UpdateOfCrossTenantRowThrowsNotFoundAsync`                               | Fact   | Update of cross tenant row throws not found.                                 |
| `UpdateWithCaseWorkflowIdFromAnotherTenantThrowsValidationFailedAsync`    | Fact   | Update with case workflow id from another tenant throws validation failed.   |
| `DeleteSoftDeletesAndPublishesDeletedChangeEventAsync`                    | Fact   | Delete soft deletes and publishes deleted change event.                      |
| `DeleteOfUnknownIdThrowsNotFoundAsync`                                    | Fact   | Delete of unknown id throws not found.                                       |
| `ListByWorkflowIdIsScopedToCallersTenantAsync`                            | Fact   | List by workflow id is scoped to callers tenant.                             |
| `ListByWorkflowIdActiveOnlyRequiresBothWorkflowAndFormRoleGrantsAsync`    | Fact   | List by workflow id active only requires both workflow and form role grants. |
| `ListHonoursCancellationAsync`                                            | Fact   | List honours cancellation.                                                   |
| `ListDoesNotPublishAChangeEventAsync`                                     | Fact   | List does not publish a change event.                                        |
| `CatalogueRegistersExpectedToolNamesForThisArea`                          | Fact   | Catalogue registers expected tool names for this area.                       |

## Case workflow macro role

### CaseWorkflowMacroRoleServiceTests

`Jube.Test.Service.Repository.CaseWorkflowMacroRole.CaseWorkflowMacroRoleServiceTests` — 31 tests. Category: Service.
Collection: Database.

| Test                                                                    | Kind   | Verifies                                                                    |
|-------------------------------------------------------------------------|--------|-----------------------------------------------------------------------------|
| `CreateWithNullOrBlankUserNameThrowsNotAuthenticatedAsync`              | Theory | Create with null or blank user name throws not authenticated.               |
| `CreateWithUserHavingNoTenantThrowsNotAuthenticatedAsync`               | Fact   | Create with user having no tenant throws not authenticated.                 |
| `ListWithoutPermissionThrowsForbiddenAsync`                             | Fact   | List without permission throws forbidden.                                   |
| `InsertWithoutPermissionThrowsForbiddenAndWritesNoRowAsync`             | Fact   | Insert without permission throws forbidden and writes no row.               |
| `DeleteWithoutPermissionThrowsForbiddenAsync`                           | Fact   | Delete without permission throws forbidden.                                 |
| `InsertPersistsAndReturnsGrantAsync`                                    | Fact   | Insert persists and returns grant.                                          |
| `InsertWithEmptyCaseWorkflowMacroGuidThrowsValidationFailedAsync`       | Fact   | Insert with empty case workflow macro guid throws validation failed.        |
| `InsertWithUnknownCaseWorkflowMacroGuidThrowsValidationFailedAsync`     | Fact   | Insert with unknown case workflow macro guid throws validation failed.      |
| `InsertWithCrossTenantCaseWorkflowMacroGuidThrowsValidationFailedAsync` | Fact   | Insert with cross tenant case workflow macro guid throws validation failed. |
| `InsertWithEmptyRoleRegistryGuidThrowsValidationFailedAsync`            | Fact   | Insert with empty role registry guid throws validation failed.              |
| `InsertWithCrossTenantRoleRegistryGuidThrowsValidationFailedAsync`      | Fact   | Insert with cross tenant role registry guid throws validation failed.       |
| `InsertPublishesCreatedChangeEventAsync`                                | Fact   | Insert publishes created change event.                                      |
| `InsertDoesNotPublishChangeEventOnValidationFailureAsync`               | Fact   | Insert does not publish change event on validation failure.                 |
| `ListByCaseWorkflowMacroGuidReturnsGrantsAsync`                         | Fact   | List by case workflow macro guid returns grants.                            |
| `ListForUnknownMacroGuidReturnsEmptyAsync`                              | Fact   | List for unknown macro guid returns empty.                                  |
| `ListIsScopedToCallersTenantAsync`                                      | Fact   | List is scoped to callers tenant.                                           |
| `ListExcludesDeletedGrantsAsync`                                        | Fact   | List excludes deleted grants.                                               |
| `DeleteSoftDeletesAndPublishesDeletedChangeEventAsync`                  | Fact   | Delete soft deletes and publishes deleted change event.                     |
| `DeleteOfMissingRowThrowsKeyNotFoundExceptionNotA404OrA204Async`        | Fact   | Delete of missing row throws key not found exception not a 404 or a 204.    |
| `DeleteOfCrossTenantRowThrowsKeyNotFoundExceptionAsync`                 | Fact   | Delete of cross tenant row throws key not found exception.                  |
| `DeleteDoesNotPublishChangeEventOnFailureAsync`                         | Fact   | Delete does not publish change event on failure.                            |
| `ListHonoursCancellationAsync`                                          | Fact   | List honours cancellation.                                                  |
| `InsertWarnsNotErrorsOnForbiddenAsync`                                  | Fact   | Insert warns not errors on forbidden.                                       |
| `InsertLogsOneAuditRecordOnSuccessAsync`                                | Fact   | Insert logs one audit record on success.                                    |
| `ListLogsOneAuditRecordAsync`                                           | Fact   | List logs one audit record.                                                 |
| `CatalogueListsUniqueCaseWorkflowMacroRoleToolNames`                    | Fact   | Catalogue lists unique case workflow macro role tool names.                 |
| `RepeatedGrantReturnsTheExistingGrantAndWritesNoSecondRowAsync`         | Fact   | Repeated grant returns the existing grant and writes no second row.         |
| `RepeatedGrantAfterRevokeCreatesANewLiveGrantAsync`                     | Fact   | Repeated grant after revoke creates a new live grant.                       |
| `RepeatedGrantAndRevokeLeavesNoAccessBehindAsync`                       | Fact   | Repeated grant and revoke leaves no access behind.                          |
| `TheSameRoleOnTwoParentsIsTwoIndependentGrantsAsync`                    | Fact   | The same role on two parents is two independent grants.                     |
| `ConcurrentIdenticalGrantsProduceExactlyOneLiveRowAsync`                | Fact   | Concurrent identical grants produce exactly one live row.                   |

## Case workflow priority

### CaseWorkflowPriorityServiceTests

`Jube.Test.Service.Repository.CaseWorkflowPriority.CaseWorkflowPriorityServiceTests` — 10 tests. Category: Service.
Collection: Database.

| Test                                                       | Kind   | Verifies                                                      |
|------------------------------------------------------------|--------|---------------------------------------------------------------|
| `CreateWithNullOrBlankUserNameThrowsNotAuthenticatedAsync` | Theory | Create with null or blank user name throws not authenticated. |
| `CreateWithUserHavingNoTenantThrowsNotAuthenticatedAsync`  | Fact   | Create with user having no tenant throws not authenticated.   |
| `ListActiveOnlyWithoutPermissionThrowsForbiddenAsync`      | Fact   | List active only without permission throws forbidden.         |
| `ListActiveOnlyReturnsFixedThreePrioritiesInOrderAsync`    | Fact   | List active only returns fixed three priorities in order.     |
| `ListActiveOnlyIsIdenticalRegardlessOfCallerTenantAsync`   | Fact   | List active only is identical regardless of caller tenant.    |
| `ListActiveOnlyDoesNotPublishAChangeEventAsync`            | Fact   | List active only does not publish a change event.             |
| `ListActiveOnlyHonoursCancellationAsync`                   | Fact   | List active only honours cancellation.                        |
| `ListActiveOnlyWritesOneAuditRecordOnSuccessAsync`         | Fact   | List active only writes one audit record on success.          |
| `ListActiveOnlyWarnsNotErrorsOnForbiddenAsync`             | Fact   | List active only warns not errors on forbidden.               |
| `AgentCatalogueRegistersCaseWorkflowPriorityListTool`      | Fact   | Agent catalogue registers case workflow priority list tool.   |

## Case workflow role

### CaseWorkflowRoleServiceTests

`Jube.Test.Service.Repository.CaseWorkflowRole.CaseWorkflowRoleServiceTests` — 31 tests. Category: Service. Collection:
Database.

| Test                                                               | Kind   | Verifies                                                                 |
|--------------------------------------------------------------------|--------|--------------------------------------------------------------------------|
| `CreateWithNullOrBlankUserNameThrowsNotAuthenticatedAsync`         | Theory | Create with null or blank user name throws not authenticated.            |
| `CreateWithUserHavingNoTenantThrowsNotAuthenticatedAsync`          | Fact   | Create with user having no tenant throws not authenticated.              |
| `ListWithoutPermissionThrowsForbiddenAsync`                        | Fact   | List without permission throws forbidden.                                |
| `InsertWithoutPermissionThrowsForbiddenAndWritesNoRowAsync`        | Fact   | Insert without permission throws forbidden and writes no row.            |
| `DeleteWithoutPermissionThrowsForbiddenAsync`                      | Fact   | Delete without permission throws forbidden.                              |
| `InsertPersistsAndReturnsGrantAsync`                               | Fact   | Insert persists and returns grant.                                       |
| `InsertWithEmptyCaseWorkflowGuidThrowsValidationFailedAsync`       | Fact   | Insert with empty case workflow guid throws validation failed.           |
| `InsertWithUnknownCaseWorkflowGuidThrowsValidationFailedAsync`     | Fact   | Insert with unknown case workflow guid throws validation failed.         |
| `InsertWithCrossTenantCaseWorkflowGuidThrowsValidationFailedAsync` | Fact   | Insert with cross tenant case workflow guid throws validation failed.    |
| `InsertWithEmptyRoleRegistryGuidThrowsValidationFailedAsync`       | Fact   | Insert with empty role registry guid throws validation failed.           |
| `InsertWithCrossTenantRoleRegistryGuidThrowsValidationFailedAsync` | Fact   | Insert with cross tenant role registry guid throws validation failed.    |
| `InsertPublishesCreatedChangeEventAsync`                           | Fact   | Insert publishes created change event.                                   |
| `InsertDoesNotPublishChangeEventOnValidationFailureAsync`          | Fact   | Insert does not publish change event on validation failure.              |
| `ListByCaseWorkflowGuidReturnsGrantsAsync`                         | Fact   | List by case workflow guid returns grants.                               |
| `ListForUnknownWorkflowGuidReturnsEmptyAsync`                      | Fact   | List for unknown workflow guid returns empty.                            |
| `ListIsScopedToCallersTenantAsync`                                 | Fact   | List is scoped to callers tenant.                                        |
| `ListExcludesDeletedGrantsAsync`                                   | Fact   | List excludes deleted grants.                                            |
| `DeleteSoftDeletesAndPublishesDeletedChangeEventAsync`             | Fact   | Delete soft deletes and publishes deleted change event.                  |
| `DeleteOfMissingRowThrowsKeyNotFoundExceptionNotA404OrA204Async`   | Fact   | Delete of missing row throws key not found exception not a 404 or a 204. |
| `DeleteOfCrossTenantRowThrowsKeyNotFoundExceptionAsync`            | Fact   | Delete of cross tenant row throws key not found exception.               |
| `DeleteDoesNotPublishChangeEventOnFailureAsync`                    | Fact   | Delete does not publish change event on failure.                         |
| `ListHonoursCancellationAsync`                                     | Fact   | List honours cancellation.                                               |
| `InsertWarnsNotErrorsOnForbiddenAsync`                             | Fact   | Insert warns not errors on forbidden.                                    |
| `InsertLogsOneAuditRecordOnSuccessAsync`                           | Fact   | Insert logs one audit record on success.                                 |
| `ListLogsOneAuditRecordAsync`                                      | Fact   | List logs one audit record.                                              |
| `CatalogueListsUniqueCaseWorkflowRoleToolNames`                    | Fact   | Catalogue lists unique case workflow role tool names.                    |
| `RepeatedGrantReturnsTheExistingGrantAndWritesNoSecondRowAsync`    | Fact   | Repeated grant returns the existing grant and writes no second row.      |
| `RepeatedGrantAfterRevokeCreatesANewLiveGrantAsync`                | Fact   | Repeated grant after revoke creates a new live grant.                    |
| `RepeatedGrantAndRevokeLeavesNoAccessBehindAsync`                  | Fact   | Repeated grant and revoke leaves no access behind.                       |
| `TheSameRoleOnTwoParentsIsTwoIndependentGrantsAsync`               | Fact   | The same role on two parents is two independent grants.                  |
| `ConcurrentIdenticalGrantsProduceExactlyOneLiveRowAsync`           | Fact   | Concurrent identical grants produce exactly one live row.                |

## Case workflow status

### CaseWorkflowStatusServiceTests

`Jube.Test.Service.Repository.CaseWorkflowStatus.CaseWorkflowStatusServiceTests` — 44 tests. Category: Service.
Collection: Database.

| Test                                                                          | Kind   | Verifies                                                                            |
|-------------------------------------------------------------------------------|--------|-------------------------------------------------------------------------------------|
| `CreateWithNullOrBlankUserNameThrowsNotAuthenticatedAsync`                    | Theory | Create with null or blank user name throws not authenticated.                       |
| `CreateWithUserHavingNoTenantThrowsNotAuthenticatedAsync`                     | Fact   | Create with user having no tenant throws not authenticated.                         |
| `ListWithoutPermissionThrowsForbiddenAsync`                                   | Fact   | List without permission throws forbidden.                                           |
| `GetByIdWithoutPermissionThrowsForbiddenAsync`                                | Fact   | Get by id without permission throws forbidden.                                      |
| `InsertWithoutPermissionThrowsForbiddenAndWritesNoRowAsync`                   | Fact   | Insert without permission throws forbidden and writes no row.                       |
| `UpdateWithoutPermissionThrowsForbiddenAsync`                                 | Fact   | Update without permission throws forbidden.                                         |
| `DeleteWithoutPermissionThrowsForbiddenAsync`                                 | Fact   | Delete without permission throws forbidden.                                         |
| `InsertPersistsAndDropsIdentityFieldsAsync`                                   | Fact   | Insert persists and drops identity fields.                                          |
| `UpdateIncrementsVersionPreservesCreatedFieldsAndSetsUpdatedFieldsAsync`      | Fact   | Update increments version preserves created fields and sets updated fields.         |
| `UpdateTamperingIdentityAndAuditFieldsHasNoEffectAsync`                       | Fact   | Update tampering identity and audit fields has no effect.                           |
| `UpdateWithUnknownIdThrowsNotFoundAsync`                                      | Fact   | Update with unknown id throws not found.                                            |
| `UpdateOfLockedRowThrowsNotFoundAsync`                                        | Fact   | Update of locked row throws not found.                                              |
| `UpdatePreservesImportIdAsync`                                                | Fact   | Update preserves import id.                                                         |
| `DeleteRemovesRowAndSubsequentGetReturnsNullAsync`                            | Fact   | Delete removes row and subsequent get returns null.                                 |
| `DeleteOfMissingRowThrowsNotFoundAsync`                                       | Fact   | Delete of missing row throws not found.                                             |
| `ListIsScopedToCallersTenantAsync`                                            | Fact   | List is scoped to callers tenant.                                                   |
| `UpdateForRowInAnotherTenantThrowsValidationFailedOnTheReferentialCheckAsync` | Fact   | Update for row in another tenant throws validation failed on the referential check. |
| `InsertWithBlankNameThrowsValidationFailedAsync`                              | Fact   | Insert with blank name throws validation failed.                                    |
| `InsertWithNullNameThrowsValidationFailedAndDoesNotThrowNullReferenceAsync`   | Fact   | Insert with null name throws validation failed and does not throw null reference.   |
| `InsertWithOverLongNameThrowsValidationFailedAsync`                           | Fact   | Insert with over long name throws validation failed.                                |
| `InsertWithDuplicateNameInSameWorkflowThrowsValidationFailedAsync`            | Fact   | Insert with duplicate name in same workflow throws validation failed.               |
| `InsertWithNonExistentCaseWorkflowIdThrowsValidationFailedAsync`              | Fact   | Insert with non existent case workflow id throws validation failed.                 |
| `InsertWithInvalidPriorityThrowsValidationFailedAsync`                        | Fact   | Insert with invalid priority throws validation failed.                              |
| `InsertWithBlankForeColorThrowsValidationFailedAsync`                         | Fact   | Insert with blank fore color throws validation failed.                              |
| `InsertWithMalformedBackColorThrowsValidationFailedAsync`                     | Fact   | Insert with malformed back color throws validation failed.                          |
| `InsertWithEnableHttpEndpointAndNoUrlThrowsValidationFailedAsync`             | Fact   | Insert with enable http endpoint and no url throws validation failed.               |
| `InsertWithEnableHttpEndpointAndInvalidTypeThrowsValidationFailedAsync`       | Fact   | Insert with enable http endpoint and invalid type throws validation failed.         |
| `InsertWithEnableHttpEndpointAndValidDataSucceedsAsync`                       | Fact   | Insert with enable http endpoint and valid data succeeds.                           |
| `InsertWithEnableNotificationAndNoDestinationThrowsValidationFailedAsync`     | Fact   | Insert with enable notification and no destination throws validation failed.        |
| `InsertWithEnableNotificationAndInvalidTypeThrowsValidationFailedAsync`       | Fact   | Insert with enable notification and invalid type throws validation failed.          |
| `InsertWithEnableNotificationAndValidDataSucceedsAsync`                       | Fact   | Insert with enable notification and valid data succeeds.                            |
| `InsertRoundTripsPriorityAndColoursAsync`                                     | Fact   | Insert round trips priority and colours.                                            |
| `ListByWorkflowIncludesInactiveRowsAsync`                                     | Fact   | List by workflow includes inactive rows.                                            |
| `ListByWorkflowGuidIncludesInactiveRowsAsync`                                 | Fact   | List by workflow guid includes inactive rows.                                       |
| `ListByWorkflowIdActiveOnlyRequiresBothWorkflowAndStatusRoleGrantsAsync`      | Fact   | List by workflow id active only requires both workflow and status role grants.      |
| `ListByWorkflowGuidActiveOnlyReturnsRowsOnceBothRolesGrantedAsync`            | Fact   | List by workflow guid active only returns rows once both roles granted.             |
| `ListHonoursCancellationAsync`                                                | Fact   | List honours cancellation.                                                          |
| `InsertPublishesCreatedChangeEventAsync`                                      | Fact   | Insert publishes created change event.                                              |
| `InsertDoesNotPublishChangeEventOnValidationFailureAsync`                     | Fact   | Insert does not publish change event on validation failure.                         |
| `DeletePublishesDeletedChangeEventAsync`                                      | Fact   | Delete publishes deleted change event.                                              |
| `ListDoesNotPublishAChangeEventAsync`                                         | Fact   | List does not publish a change event.                                               |
| `InsertWarnsNotErrorsOnForbiddenAsync`                                        | Fact   | Insert warns not errors on forbidden.                                               |
| `InsertWritesOneAuditRecordOnSuccessAsync`                                    | Fact   | Insert writes one audit record on success.                                          |
| `CatalogueRegistersUniqueCaseWorkflowStatusToolNames`                         | Fact   | Catalogue registers unique case workflow status tool names.                         |

## Case workflow status role

### CaseWorkflowStatusRoleServiceTests

`Jube.Test.Service.Repository.CaseWorkflowStatusRole.CaseWorkflowStatusRoleServiceTests` — 31 tests. Category: Service.
Collection: Database.

| Test                                                                     | Kind   | Verifies                                                                     |
|--------------------------------------------------------------------------|--------|------------------------------------------------------------------------------|
| `CreateWithNullOrBlankUserNameThrowsNotAuthenticatedAsync`               | Theory | Create with null or blank user name throws not authenticated.                |
| `CreateWithUserHavingNoTenantThrowsNotAuthenticatedAsync`                | Fact   | Create with user having no tenant throws not authenticated.                  |
| `ListWithoutPermissionThrowsForbiddenAsync`                              | Fact   | List without permission throws forbidden.                                    |
| `InsertWithoutPermissionThrowsForbiddenAndWritesNoRowAsync`              | Fact   | Insert without permission throws forbidden and writes no row.                |
| `DeleteWithoutPermissionThrowsForbiddenAsync`                            | Fact   | Delete without permission throws forbidden.                                  |
| `InsertPersistsAndReturnsGrantAsync`                                     | Fact   | Insert persists and returns grant.                                           |
| `InsertWithEmptyCaseWorkflowStatusGuidThrowsValidationFailedAsync`       | Fact   | Insert with empty case workflow status guid throws validation failed.        |
| `InsertWithUnknownCaseWorkflowStatusGuidThrowsValidationFailedAsync`     | Fact   | Insert with unknown case workflow status guid throws validation failed.      |
| `InsertWithCrossTenantCaseWorkflowStatusGuidThrowsValidationFailedAsync` | Fact   | Insert with cross tenant case workflow status guid throws validation failed. |
| `InsertWithEmptyRoleRegistryGuidThrowsValidationFailedAsync`             | Fact   | Insert with empty role registry guid throws validation failed.               |
| `InsertWithCrossTenantRoleRegistryGuidThrowsValidationFailedAsync`       | Fact   | Insert with cross tenant role registry guid throws validation failed.        |
| `InsertPublishesCreatedChangeEventAsync`                                 | Fact   | Insert publishes created change event.                                       |
| `InsertDoesNotPublishChangeEventOnValidationFailureAsync`                | Fact   | Insert does not publish change event on validation failure.                  |
| `ListByCaseWorkflowStatusGuidReturnsGrantsAsync`                         | Fact   | List by case workflow status guid returns grants.                            |
| `ListForUnknownStatusGuidReturnsEmptyAsync`                              | Fact   | List for unknown status guid returns empty.                                  |
| `ListIsScopedToCallersTenantAsync`                                       | Fact   | List is scoped to callers tenant.                                            |
| `ListExcludesDeletedGrantsAsync`                                         | Fact   | List excludes deleted grants.                                                |
| `DeleteSoftDeletesAndPublishesDeletedChangeEventAsync`                   | Fact   | Delete soft deletes and publishes deleted change event.                      |
| `DeleteOfMissingRowThrowsKeyNotFoundExceptionNotA404OrA204Async`         | Fact   | Delete of missing row throws key not found exception not a 404 or a 204.     |
| `DeleteOfCrossTenantRowThrowsKeyNotFoundExceptionAsync`                  | Fact   | Delete of cross tenant row throws key not found exception.                   |
| `DeleteDoesNotPublishChangeEventOnFailureAsync`                          | Fact   | Delete does not publish change event on failure.                             |
| `ListHonoursCancellationAsync`                                           | Fact   | List honours cancellation.                                                   |
| `InsertWarnsNotErrorsOnForbiddenAsync`                                   | Fact   | Insert warns not errors on forbidden.                                        |
| `InsertLogsOneAuditRecordOnSuccessAsync`                                 | Fact   | Insert logs one audit record on success.                                     |
| `ListLogsOneAuditRecordAsync`                                            | Fact   | List logs one audit record.                                                  |
| `CatalogueListsUniqueCaseWorkflowStatusRoleToolNames`                    | Fact   | Catalogue lists unique case workflow status role tool names.                 |
| `RepeatedGrantReturnsTheExistingGrantAndWritesNoSecondRowAsync`          | Fact   | Repeated grant returns the existing grant and writes no second row.          |
| `RepeatedGrantAfterRevokeCreatesANewLiveGrantAsync`                      | Fact   | Repeated grant after revoke creates a new live grant.                        |
| `RepeatedGrantAndRevokeLeavesNoAccessBehindAsync`                        | Fact   | Repeated grant and revoke leaves no access behind.                           |
| `TheSameRoleOnTwoParentsIsTwoIndependentGrantsAsync`                     | Fact   | The same role on two parents is two independent grants.                      |
| `ConcurrentIdenticalGrantsProduceExactlyOneLiveRowAsync`                 | Fact   | Concurrent identical grants produce exactly one live row.                    |

## Case workflow x path

### CaseWorkflowXPathServiceTests

`Jube.Test.Service.Repository.CaseWorkflowXPath.CaseWorkflowXPathServiceTests` — 44 tests. Category: Service.
Collection: Database.

| Test                                                                         | Kind   | Verifies                                                                             |
|------------------------------------------------------------------------------|--------|--------------------------------------------------------------------------------------|
| `CreateWithNullOrBlankUserNameThrowsNotAuthenticatedAsync`                   | Theory | Create with null or blank user name throws not authenticated.                        |
| `CreateWithUserHavingNoTenantThrowsNotAuthenticatedAsync`                    | Fact   | Create with user having no tenant throws not authenticated.                          |
| `ListWithoutPermissionThrowsForbiddenAsync`                                  | Fact   | List without permission throws forbidden.                                            |
| `InsertWithoutPermissionThrowsForbiddenAndWritesNoRowAsync`                  | Fact   | Insert without permission throws forbidden and writes no row.                        |
| `PermissionDeniedLogsWarnNotErrorAsync`                                      | Fact   | Permission denied logs warn not error.                                               |
| `InsertPersistsAndReturnsVersionOneWithAuditFieldsAsync`                     | Fact   | Insert persists and returns version one with audit fields.                           |
| `InsertWithBlankNameThrowsValidationFailedAsync`                             | Fact   | Insert with blank name throws validation failed.                                     |
| `InsertWithUnknownCaseWorkflowIdThrowsValidationFailedAsync`                 | Fact   | Insert with unknown case workflow id throws validation failed.                       |
| `InsertWithCaseWorkflowUnderSoftDeletedModelThrowsValidationFailedAsync`     | Fact   | Insert with case workflow under soft deleted model throws validation failed.         |
| `InsertWithBlankXPathThrowsValidationFailedAsync`                            | Fact   | Insert with blank x path throws validation failed.                                   |
| `InsertWithDuplicateNameInSameWorkflowThrowsValidationFailedAsync`           | Fact   | Insert with duplicate name in same workflow throws validation failed.                |
| `InsertWithSameNameInDifferentWorkflowSucceedsAsync`                         | Fact   | Insert with same name in different workflow succeeds.                                |
| `InsertWithBoldLineMatchedRequiresColorsAsync`                               | Fact   | Insert with bold line matched requires colors.                                       |
| `InsertWithConditionalFormattingRequiresRegexAndColorsAsync`                 | Fact   | Insert with conditional formatting requires regex and colors.                        |
| `InsertIgnoresDisplayNameDeadFieldAsync`                                     | Fact   | Insert ignores display name dead field.                                              |
| `InsertIgnoresTamperedIdentityAndAuditFieldsAsync`                           | Fact   | Insert ignores tampered identity and audit fields.                                   |
| `UpdatePreservesCreatedFieldsAndSetsUpdatedFieldsAsync`                      | Fact   | Update preserves created fields and sets updated fields.                             |
| `UpdateWritesAVersionAuditRowCarryingTheDeletedFieldsAsync`                  | Fact   | Update writes a version audit row carrying the deleted fields.                       |
| `UpdatePreservesImportIdAsync`                                               | Fact   | Update preserves import id.                                                          |
| `UpdateOfUnknownIdThrowsNotFoundAsync`                                       | Fact   | Update of unknown id throws not found.                                               |
| `UpdateOfLockedRowThrowsNotFoundAsync`                                       | Fact   | Update of locked row throws not found.                                               |
| `UpdateOfCrossTenantRowThrowsNotFoundAsync`                                  | Fact   | Update of cross tenant row throws not found.                                         |
| `UpdateWithCaseWorkflowIdFromAnotherTenantThrowsValidationFailedAsync`       | Fact   | Update with case workflow id from another tenant throws validation failed.           |
| `UpdateIgnoresTamperedIdentityAndAuditFieldsAsync`                           | Fact   | Update ignores tampered identity and audit fields.                                   |
| `DeleteSoftDeletesAndPublishesDeletedChangeEventAsync`                       | Fact   | Delete soft deletes and publishes deleted change event.                              |
| `DeleteOfUnknownIdThrowsNotFoundAsync`                                       | Fact   | Delete of unknown id throws not found.                                               |
| `DeleteOfLockedRowThrowsNotFoundAsync`                                       | Fact   | Delete of locked row throws not found.                                               |
| `ListByWorkflowIdIsScopedToCallersTenantAsync`                               | Fact   | List by workflow id is scoped to callers tenant.                                     |
| `ListByWorkflowIdActiveDrillOnlyRequiresBothWorkflowAndXPathRoleGrantsAsync` | Fact   | List by workflow id active drill only requires both workflow and x path role grants. |
| `ListByWorkflowIdActiveDrillOnlyExcludesNonDrillRowsAsync`                   | Fact   | List by workflow id active drill only excludes non drill rows.                       |
| `ListByWorkflowGuidActiveDrillOnlyReturnsMatchingAsync`                      | Fact   | List by workflow guid active drill only returns matching.                            |
| `ListHonoursCancellationAsync`                                               | Fact   | List honours cancellation.                                                           |
| `RoundTripPreservesAllExtractionAndColorFieldsAsync`                         | Fact   | Round trip preserves all extraction and color fields.                                |
| `SuccessfulInsertLogsExactlyOneInfoAndReadsLogNoInfoAsync`                   | Fact   | Successful insert logs exactly one info and reads log no info.                       |
| `WithGatesDisabledHappyPathRecordsNoDebugInfoOrWarnAsync`                    | Fact   | With gates disabled happy path records no debug info or warn.                        |
| `FailureLogsErrorWithExceptionAttachedAndStillPropagatesAsync`               | Fact   | Failure logs error with exception attached and still propagates.                     |
| `EachCallEmitsOneSpanWithOutcomeTagAsync`                                    | Fact   | Each call emits one span with outcome tag.                                           |
| `EachCallRecordsOneDurationMeasurementAsync`                                 | Fact   | Each call records one duration measurement.                                          |
| `AuditLogGetsExactlyOneLineIncludingReadsAsync`                              | Fact   | Audit log gets exactly one line including reads.                                     |
| `ListDoesNotPublishAChangeEventAsync`                                        | Fact   | List does not publish a change event.                                                |
| `FailedInsertPublishesNothingAsync`                                          | Fact   | Failed insert publishes nothing.                                                     |
| `InsertWithBlankNameUsesFrenchMessageUnderFrenchCultureAsync`                | Fact   | Insert with blank name uses french message under french culture.                     |
| `CatalogueRegistersExpectedToolNamesForThisArea`                             | Fact   | Catalogue registers expected tool names for this area.                               |
| `ListAsyncClampsTakeAndPaginatesDeterministicallyAsync`                      | Fact   | List async clamps take and paginates deterministically.                              |

## Case workflow x path role

### CaseWorkflowXPathRoleServiceTests

`Jube.Test.Service.Repository.CaseWorkflowXPathRole.CaseWorkflowXPathRoleServiceTests` — 31 tests. Category: Service.
Collection: Database.

| Test                                                                    | Kind   | Verifies                                                                     |
|-------------------------------------------------------------------------|--------|------------------------------------------------------------------------------|
| `CreateWithNullOrBlankUserNameThrowsNotAuthenticatedAsync`              | Theory | Create with null or blank user name throws not authenticated.                |
| `CreateWithUserHavingNoTenantThrowsNotAuthenticatedAsync`               | Fact   | Create with user having no tenant throws not authenticated.                  |
| `ListWithoutPermissionThrowsForbiddenAsync`                             | Fact   | List without permission throws forbidden.                                    |
| `InsertWithoutPermissionThrowsForbiddenAndWritesNoRowAsync`             | Fact   | Insert without permission throws forbidden and writes no row.                |
| `DeleteWithoutPermissionThrowsForbiddenAsync`                           | Fact   | Delete without permission throws forbidden.                                  |
| `InsertPersistsAndReturnsGrantAsync`                                    | Fact   | Insert persists and returns grant.                                           |
| `InsertWithEmptyCaseWorkflowXPathGuidThrowsValidationFailedAsync`       | Fact   | Insert with empty case workflow x path guid throws validation failed.        |
| `InsertWithUnknownCaseWorkflowXPathGuidThrowsValidationFailedAsync`     | Fact   | Insert with unknown case workflow x path guid throws validation failed.      |
| `InsertWithCrossTenantCaseWorkflowXPathGuidThrowsValidationFailedAsync` | Fact   | Insert with cross tenant case workflow x path guid throws validation failed. |
| `InsertWithEmptyRoleRegistryGuidThrowsValidationFailedAsync`            | Fact   | Insert with empty role registry guid throws validation failed.               |
| `InsertWithCrossTenantRoleRegistryGuidThrowsValidationFailedAsync`      | Fact   | Insert with cross tenant role registry guid throws validation failed.        |
| `InsertPublishesCreatedChangeEventAsync`                                | Fact   | Insert publishes created change event.                                       |
| `InsertDoesNotPublishChangeEventOnValidationFailureAsync`               | Fact   | Insert does not publish change event on validation failure.                  |
| `ListByCaseWorkflowXPathGuidReturnsGrantsAsync`                         | Fact   | List by case workflow x path guid returns grants.                            |
| `ListForUnknownXPathGuidReturnsEmptyAsync`                              | Fact   | List for unknown x path guid returns empty.                                  |
| `ListIsScopedToCallersTenantAsync`                                      | Fact   | List is scoped to callers tenant.                                            |
| `ListExcludesDeletedGrantsAsync`                                        | Fact   | List excludes deleted grants.                                                |
| `DeleteSoftDeletesAndPublishesDeletedChangeEventAsync`                  | Fact   | Delete soft deletes and publishes deleted change event.                      |
| `DeleteOfMissingRowThrowsKeyNotFoundExceptionNotA404OrA204Async`        | Fact   | Delete of missing row throws key not found exception not a 404 or a 204.     |
| `DeleteOfCrossTenantRowThrowsKeyNotFoundExceptionAsync`                 | Fact   | Delete of cross tenant row throws key not found exception.                   |
| `DeleteDoesNotPublishChangeEventOnFailureAsync`                         | Fact   | Delete does not publish change event on failure.                             |
| `ListHonoursCancellationAsync`                                          | Fact   | List honours cancellation.                                                   |
| `InsertWarnsNotErrorsOnForbiddenAsync`                                  | Fact   | Insert warns not errors on forbidden.                                        |
| `InsertLogsOneAuditRecordOnSuccessAsync`                                | Fact   | Insert logs one audit record on success.                                     |
| `ListLogsOneAuditRecordAsync`                                           | Fact   | List logs one audit record.                                                  |
| `CatalogueListsUniqueCaseWorkflowXPathRoleToolNames`                    | Fact   | Catalogue lists unique case workflow x path role tool names.                 |
| `RepeatedGrantReturnsTheExistingGrantAndWritesNoSecondRowAsync`         | Fact   | Repeated grant returns the existing grant and writes no second row.          |
| `RepeatedGrantAfterRevokeCreatesANewLiveGrantAsync`                     | Fact   | Repeated grant after revoke creates a new live grant.                        |
| `RepeatedGrantAndRevokeLeavesNoAccessBehindAsync`                       | Fact   | Repeated grant and revoke leaves no access behind.                           |
| `TheSameRoleOnTwoParentsIsTwoIndependentGrantsAsync`                    | Fact   | The same role on two parents is two independent grants.                      |
| `ConcurrentIdenticalGrantsProduceExactlyOneLiveRowAsync`                | Fact   | Concurrent identical grants produce exactly one live row.                    |

## Entity analysis model role

### EntityAnalysisModelRoleServiceTests

`Jube.Test.Service.Repository.EntityAnalysisModelRole.EntityAnalysisModelRoleServiceTests` — 31 tests. Category:
Service. Collection: Database.

| Test                                                                      | Kind   | Verifies                                                                      |
|---------------------------------------------------------------------------|--------|-------------------------------------------------------------------------------|
| `CreateWithNullOrBlankUserNameThrowsNotAuthenticatedAsync`                | Theory | Create with null or blank user name throws not authenticated.                 |
| `CreateWithUserHavingNoTenantThrowsNotAuthenticatedAsync`                 | Fact   | Create with user having no tenant throws not authenticated.                   |
| `ListWithoutPermissionThrowsForbiddenAsync`                               | Fact   | List without permission throws forbidden.                                     |
| `InsertWithoutPermissionThrowsForbiddenAndWritesNoRowAsync`               | Fact   | Insert without permission throws forbidden and writes no row.                 |
| `DeleteWithoutPermissionThrowsForbiddenAsync`                             | Fact   | Delete without permission throws forbidden.                                   |
| `InsertPersistsAndReturnsGrantAsync`                                      | Fact   | Insert persists and returns grant.                                            |
| `InsertWithEmptyEntityAnalysisModelGuidThrowsValidationFailedAsync`       | Fact   | Insert with empty entity analysis model guid throws validation failed.        |
| `InsertWithUnknownEntityAnalysisModelGuidThrowsValidationFailedAsync`     | Fact   | Insert with unknown entity analysis model guid throws validation failed.      |
| `InsertWithCrossTenantEntityAnalysisModelGuidThrowsValidationFailedAsync` | Fact   | Insert with cross tenant entity analysis model guid throws validation failed. |
| `InsertWithEmptyRoleRegistryGuidThrowsValidationFailedAsync`              | Fact   | Insert with empty role registry guid throws validation failed.                |
| `InsertWithCrossTenantRoleRegistryGuidThrowsValidationFailedAsync`        | Fact   | Insert with cross tenant role registry guid throws validation failed.         |
| `InsertPublishesCreatedChangeEventAsync`                                  | Fact   | Insert publishes created change event.                                        |
| `InsertDoesNotPublishChangeEventOnValidationFailureAsync`                 | Fact   | Insert does not publish change event on validation failure.                   |
| `ListByEntityAnalysisModelGuidReturnsGrantsAsync`                         | Fact   | List by entity analysis model guid returns grants.                            |
| `ListForUnknownModelGuidReturnsEmptyAsync`                                | Fact   | List for unknown model guid returns empty.                                    |
| `ListIsScopedToCallersTenantAsync`                                        | Fact   | List is scoped to callers tenant.                                             |
| `ListExcludesDeletedGrantsAsync`                                          | Fact   | List excludes deleted grants.                                                 |
| `DeleteSoftDeletesAndPublishesDeletedChangeEventAsync`                    | Fact   | Delete soft deletes and publishes deleted change event.                       |
| `DeleteOfMissingRowThrowsKeyNotFoundExceptionNotA404OrA204Async`          | Fact   | Delete of missing row throws key not found exception not a 404 or a 204.      |
| `DeleteOfCrossTenantRowThrowsKeyNotFoundExceptionAsync`                   | Fact   | Delete of cross tenant row throws key not found exception.                    |
| `DeleteDoesNotPublishChangeEventOnFailureAsync`                           | Fact   | Delete does not publish change event on failure.                              |
| `ListHonoursCancellationAsync`                                            | Fact   | List honours cancellation.                                                    |
| `InsertWarnsNotErrorsOnForbiddenAsync`                                    | Fact   | Insert warns not errors on forbidden.                                         |
| `InsertLogsOneAuditRecordOnSuccessAsync`                                  | Fact   | Insert logs one audit record on success.                                      |
| `ListLogsOneAuditRecordAsync`                                             | Fact   | List logs one audit record.                                                   |
| `CatalogueListsUniqueEntityAnalysisModelRoleToolNames`                    | Fact   | Catalogue lists unique entity analysis model role tool names.                 |
| `RepeatedGrantReturnsTheExistingGrantAndWritesNoSecondRowAsync`           | Fact   | Repeated grant returns the existing grant and writes no second row.           |
| `RepeatedGrantAfterRevokeCreatesANewLiveGrantAsync`                       | Fact   | Repeated grant after revoke creates a new live grant.                         |
| `RepeatedGrantAndRevokeLeavesNoAccessBehindAsync`                         | Fact   | Repeated grant and revoke leaves no access behind.                            |
| `TheSameRoleOnTwoParentsIsTwoIndependentGrantsAsync`                      | Fact   | The same role on two parents is two independent grants.                       |
| `ConcurrentIdenticalGrantsProduceExactlyOneLiveRowAsync`                  | Fact   | Concurrent identical grants produce exactly one live row.                     |

## Entity analysis model synchronisation schedule

### EntityAnalysisModelSynchronisationScheduleServiceTests

`Jube.Test.Service.Repository.EntityAnalysisModelSynchronisationSchedule.EntityAnalysisModelSynchronisationScheduleServiceTests` — 18 tests. Category: Service. Collection: Database.

| Test                                                                     | Kind | Verifies                                                                     |
|--------------------------------------------------------------------------|------|------------------------------------------------------------------------------|
| `InsertPersistsWithScheduleDateAndIsReturnedByGetCurrentAsync`           | Fact | Insert persists with schedule date and is returned by get current.           |
| `InsertWithNoScheduleDateDefaultsToCreatedDateAsync`                     | Fact | Insert with no schedule date defaults to created date.                       |
| `GetCurrentReturnsTheMostRecentlyScheduledRowAsync`                      | Fact | Get current returns the most recently scheduled row.                         |
| `GetCurrentWithNoScheduleReturnsNullAsync`                               | Fact | Get current with no schedule returns null.                                   |
| `InsertByUserWithoutPermissionThrowsForbiddenAsync`                      | Fact | Insert by user without permission throws forbidden.                          |
| `GetCurrentByUserWithoutPermissionThrowsForbiddenAsync`                  | Fact | Get current by user without permission throws forbidden.                     |
| `InsertByUnauthenticatedUserThrowsNotAuthenticatedAndIssuesNoQueryAsync` | Fact | Insert by unauthenticated user throws not authenticated and issues no query. |
| `InsertByBlankUserThrowsNotAuthenticatedAsync`                           | Fact | Insert by blank user throws not authenticated.                               |
| `InsertByUserWithNoTenantThrowsNotAuthenticatedAsync`                    | Fact | Insert by user with no tenant throws not authenticated.                      |
| `InsertByUnknownUserThrowsNotAuthenticatedAsync`                         | Fact | Insert by unknown user throws not authenticated.                             |
| `ScheduleIsIsolatedPerTenantAsync`                                       | Fact | Schedule is isolated per tenant.                                             |
| `TamperedCreatedUserAndTenantRegistryIdAreIgnoredOnInsertAsync`          | Fact | Tampered created user and tenant registry id are ignored on insert.          |
| `ListAsyncClampsTakeAndPaginatesAsync`                                   | Fact | List async clamps take and paginates.                                        |
| `InsertPublishesServiceChangeEventOnlyOnMutationAsync`                   | Fact | Insert publishes service change event only on mutation.                      |
| `InsertLogsBusinessInfoAndAuditRecordAsync`                              | Fact | Insert logs business info and audit record.                                  |
| `PermissionDeniedLogsWarnNotErrorAsync`                                  | Fact | Permission denied logs warn not error.                                       |
| `InsertWithScheduleDateTooFarInFutureIsRejectedAsync`                    | Fact | Insert with schedule date too far in future is rejected.                     |
| `GetCurrentHonoursCancellationAsync`                                     | Fact | Get current honours cancellation.                                            |

### SyncScheduleButtonTests

`Jube.Test.Service.Repository.EntityAnalysisModelSynchronisationSchedule.SyncScheduleButtonTests` — 4 tests. Category:
Service. Collection: Database.

| Test                                                          | Kind | Verifies                                                           |
|---------------------------------------------------------------|------|--------------------------------------------------------------------|
| `APickedTimeIsStoredAsTheSameInstantItWasPickedForAsync`      | Fact | A picked time is stored as the same instant it was picked for.     |
| `ThePickedTimeIsSentAsUtcAndReadBackUnchangedAsync`           | Fact | The picked time is sent as utc and read back unchanged.            |
| `SyncNowWithNoPickedTimeSchedulesNowAsync`                    | Fact | Sync now with no picked time schedules now.                        |
| `AFutureTimeIsSavedAndIsTheCurrentScheduleUntilReplacedAsync` | Fact | A future time is saved and is the current schedule until replaced. |

## Exhaustive search instance promoted trial instance

### ExhaustiveSearchInstancePromotedTrialInstanceServiceTests

`Jube.Test.Service.Repository.ExhaustiveSearchInstancePromotedTrialInstance.ExhaustiveSearchInstancePromotedTrialInstanceServiceTests` — 25 tests. Category: Service. Collection: Database.

| Test                                                                               | Kind   | Verifies                                                                                  |
|------------------------------------------------------------------------------------|--------|-------------------------------------------------------------------------------------------|
| `UpdateActiveSetsActiveTrueAsync`                                                  | Fact   | Update active sets active true.                                                           |
| `UpdateActiveSetsActiveFalseAsync`                                                 | Fact   | Update active sets active false.                                                          |
| `UpdateActiveIsIdempotentAsync`                                                    | Fact   | Update active is idempotent.                                                              |
| `UpdateOfSoftDeletedRowStillSucceedsBecauseTheRepositoryDoesNotFilterDeletedAsync` | Fact   | Update of soft deleted row still succeeds because the repository does not filter deleted. |
| `UpdateOfMissingIdThrowsNotFoundAsync`                                             | Fact   | Update of missing id throws not found.                                                    |
| `UserInTenantBCannotUpdateTenantARowAsync`                                         | Fact   | User in tenant b cannot update tenant a row.                                              |
| `EveryCallThrowsForbiddenWhenPermissionMissingAndWritesNoRowAsync`                 | Fact   | Every call throws forbidden when permission missing and writes no row.                    |
| `NullOrBlankUserNameThrowsNotAuthenticatedBeforeAnyQueryAsync`                     | Theory | Null or blank user name throws not authenticated before any query.                        |
| `UserWithNoUserInTenantRowThrowsNotAuthenticatedAsync`                             | Fact   | User with no user in tenant row throws not authenticated.                                 |
| `UnknownUserThrowsNotAuthenticatedAsync`                                           | Fact   | Unknown user throws not authenticated.                                                    |
| `InvalidIdIsRejectedWithErrorCodeAsync`                                            | Theory | Invalid id is rejected with error code.                                                   |
| `NullModelThrowsArgumentNullExceptionAsync`                                        | Fact   | Null model throws argument null exception.                                                |
| `PreCancelledTokenThrowsOperationCanceledAsync`                                    | Fact   | Pre cancelled token throws operation canceled.                                            |
| `PermissionDeniedLogsWarnNotErrorAsync`                                            | Fact   | Permission denied logs warn not error.                                                    |
| `NotFoundLogsWarnNotErrorAsync`                                                    | Fact   | Not found logs warn not error.                                                            |
| `SuccessfulUpdateLogsExactlyOneInfoAsync`                                          | Fact   | Successful update logs exactly one info.                                                  |
| `WithGatesDisabledHappyPathRecordsNoDebugInfoOrWarnAsync`                          | Fact   | With gates disabled happy path records no debug info or warn.                             |
| `FailureLogsErrorWithExceptionAttachedAndStillPropagatesAsync`                     | Fact   | Failure logs error with exception attached and still propagates.                          |
| `CallEmitsOneSpanWithOutcomeTagAsync`                                              | Fact   | Call emits one span with outcome tag.                                                     |
| `CallRecordsOneDurationMeasurementAsync`                                           | Fact   | Call records one duration measurement.                                                    |
| `AuditLogGetsExactlyOneLineAsync`                                                  | Fact   | Audit log gets exactly one line.                                                          |
| `SuccessfulUpdatePublishesExactlyOneUpdatedEventAsync`                             | Fact   | Successful update publishes exactly one updated event.                                    |
| `FailedUpdatePublishesNothingAsync`                                                | Fact   | Failed update publishes nothing.                                                          |
| `CatalogueRegistersUniquePascalCaseNoUnderscoreName`                               | Fact   | Catalogue registers unique pascal case no underscore name.                                |
| `IdInvalidMessageResolvesFromFrenchResourceAsync`                                  | Fact   | Id invalid message resolves from french resource.                                         |

## Permission specification

### PermissionSpecificationServiceTests

`Jube.Test.Service.Repository.PermissionSpecification.PermissionSpecificationServiceTests` — 25 tests. Category:
Service. Collection: Database.

| Test                                                             | Kind   | Verifies                                                             |
|------------------------------------------------------------------|--------|----------------------------------------------------------------------|
| `CreateWithNullOrBlankUserNameThrowsNotAuthenticatedAsync`       | Theory | Create with null or blank user name throws not authenticated.        |
| `CreateWithUserHavingNoTenantThrowsNotAuthenticatedAsync`        | Fact   | Create with user having no tenant throws not authenticated.          |
| `CreateWithUnknownUserThrowsNotAuthenticatedAsync`               | Fact   | Create with unknown user throws not authenticated.                   |
| `GetAsyncThrowsForbiddenWhenPermissionMissingAsync`              | Fact   | Get async throws forbidden when permission missing.                  |
| `ListAsyncThrowsForbiddenWhenPermissionMissingAsync`             | Fact   | List async throws forbidden when permission missing.                 |
| `GetAsyncSucceedsForAUserGrantedExactlyPermissionThirtySixAsync` | Fact   | Get async succeeds for a user granted exactly permission thirty six. |
| `GetAsyncDoesNotListTheRetiredObservabilitySpecificationAsync`   | Fact   | Get async does not list the retired observability specification.     |
| `GetAsyncSucceedsForLandlordUserAsync`                           | Fact   | Get async succeeds for landlord user.                                |
| `GetAsyncReturnsTheKnownBaselineCatalogueRowsAsync`              | Fact   | Get async returns the known baseline catalogue rows.                 |
| `CatalogueIsNotTenantScopedAndIsIdenticalFromEveryTenantAsync`   | Fact   | Catalogue is not tenant scoped and is identical from every tenant.   |
| `ListAsyncClampsTakeAndOrdersByIdAscendingAsync`                 | Fact   | List async clamps take and orders by id ascending.                   |
| `ListAsyncClampsTakeToAtLeastOneAsync`                           | Fact   | List async clamps take to at least one.                              |
| `ListAsyncClampsTakeToAtMostTwoHundredAsync`                     | Fact   | List async clamps take to at most two hundred.                       |
| `ListAsyncPagesWithAfterIdAsync`                                 | Fact   | List async pages with after id.                                      |
| `PreCancelledTokenOnGetThrowsOperationCanceledAsync`             | Fact   | Pre cancelled token on get throws operation canceled.                |
| `PermissionDeniedLogsWarnNotErrorAsync`                          | Fact   | Permission denied logs warn not error.                               |
| `SuccessfulGetLogsNoInfoAsync`                                   | Fact   | Successful get logs no info.                                         |
| `WithGatesDisabledHappyPathRecordsNoDebugInfoOrWarnAsync`        | Fact   | With gates disabled happy path records no debug info or warn.        |
| `FailureLogsErrorWithExceptionAttachedAndStillPropagatesAsync`   | Fact   | Failure logs error with exception attached and still propagates.     |
| `EachCallEmitsOneSpanWithOutcomeTagAsync`                        | Fact   | Each call emits one span with outcome tag.                           |
| `EachCallRecordsOneDurationMeasurementAsync`                     | Fact   | Each call records one duration measurement.                          |
| `AuditLogGetsExactlyOneLineIncludingReadsAsync`                  | Fact   | Audit log gets exactly one line including reads.                     |
| `ReadsAndFailuresPublishNothingAsync`                            | Fact   | Reads and failures publish nothing.                                  |
| `CatalogueRegistersUniquePascalCaseNoUnderscoreNames`            | Fact   | Catalogue registers unique pascal case no underscore names.          |
| `PermissionDeniedMessageResolvesForFrenchCultureAsync`           | Fact   | Permission denied message resolves for french culture.               |

## Preservation

### PreservationServiceTests

`Jube.Test.Service.Repository.Preservation.PreservationServiceTests` — 39 tests. Category: Service. Collection:
Database.

| Test                                                                               | Kind   | Verifies                                                                                |
|------------------------------------------------------------------------------------|--------|-----------------------------------------------------------------------------------------|
| `CreateWithNullOrBlankUserNameThrowsNotAuthenticatedAsync`                         | Theory | Create with null or blank user name throws not authenticated.                           |
| `CreateWithUnmappedOrTenantlessUserThrowsNotAuthenticatedAsync`                    | Fact   | Create with unmapped or tenantless user throws not authenticated.                       |
| `WithoutPermissionAllOperationsForbiddenAndNothingWrittenAsync`                    | Fact   | Without permission all operations forbidden and nothing written.                        |
| `ExportContainsOnlyCallersTenantAndPeekMatchesAsync`                               | Fact   | Export contains only callers tenant and peek matches.                                   |
| `ExportWithDifferentPasswordCannotBeDecryptedWithOthersAsync`                      | Fact   | Export with different password cannot be decrypted with others.                         |
| `ExportReturnsJempFileNameFromExportGuidAndPersistsAuditRowAsync`                  | Fact   | Export returns jemp file name from export guid and persists audit row.                  |
| `RoundTripIntoSecondTenantRemapsIdsTenantAndKeepsSourceUntouchedAsync`             | Fact   | Round trip into second tenant remaps ids tenant and keeps source untouched.             |
| `ExhaustiveClassHistogramsAreExportedAndRoundTripIntoASecondTenantAsync`           | Fact   | Exhaustive class histograms are exported and round trip into a second tenant.           |
| `ExhaustiveUploadedDatasetsRoundTripWithRowsAndRemappedInstanceReferenceAsync`     | Fact   | Exhaustive uploaded datasets round trip with rows and remapped instance reference.      |
| `ExhaustiveUploadedJsonDatasetsRoundTripWithRowsAndRemappedInstanceReferenceAsync` | Fact   | Exhaustive uploaded json datasets round trip with rows and remapped instance reference. |
| `ExhaustiveInstanceWhoseDatasetIsAbsentFromTheFileFallsBackToArchiveAsync`         | Fact   | Exhaustive instance whose dataset is absent from the file falls back to archive.        |
| `ExhaustiveJsonDatasetExportKeepsOnlyTheRowsTrainingCanReadAsync`                  | Fact   | Exhaustive json dataset export keeps only the rows training can read.                   |
| `PreservationCoversEveryExhaustiveInstanceTable`                                   | Fact   | Preservation covers every exhaustive instance table.                                    |
| `ExhaustiveDatasetExportKeepsOnlyTheRowsTrainingCanReadAsync`                      | Fact   | Exhaustive dataset export keeps only the rows training can read.                        |
| `RolesImportedIntoOwnTenantKeepsRoleAndPermissionAsync`                            | Fact   | Roles imported into own tenant keeps role and permission.                               |
| `RolesImportedFromAnotherTenantIsRefusedWhileTargetHasUsersAndRollsBackAsync`      | Fact   | Roles imported from another tenant is refused while target has users and rolls back.    |
| `ImportRestoresGuidWhenSameTenantReimportsItsOwnExportAsync`                       | Fact   | Import restores guid when same tenant reimports its own export.                         |
| `WrongPasswordThrowsInvalidFileLeavesDataAndMarksImportInErrorAsync`               | Fact   | Wrong password throws invalid file leaves data and marks import in error.               |
| `CorruptOrEmptyFileFailsLeavesDataUntouchedAndPublishesNothingAsync`               | Theory | Corrupt or empty file fails leaves data untouched and publishes nothing.                |
| `TamperedFileWithLegacyFallbackDisabledIsInvalidFileAsync`                         | Fact   | Tampered file with legacy fallback disabled is invalid file.                            |
| `NoFilesThrowsNoFileExceptionAsync`                                                | Fact   | No files throws no file exception.                                                      |
| `FailedImportMidTransactionRollsBackEverythingAsync`                               | Fact   | Failed import mid transaction rolls back everything.                                    |
| `CancelledTokenIsHonouredAndLeavesDataUntouchedAsync`                              | Fact   | Cancelled token is honoured and leaves data untouched.                                  |
| `ExportAndPeekWriteExactlyOneAuditRecordEachAndPublishNothingAsync`                | Fact   | Export and peek write exactly one audit record each and publish nothing.                |
| `ForbiddenOperationAuditsForbiddenOutcomeAsync`                                    | Fact   | Forbidden operation audits forbidden outcome.                                           |
| `RoundTripKeepsTheCategoricalVocabularyAndTheRecallInputsAreIdenticalAsync`        | Fact   | Round trip keeps the categorical vocabulary and the recall inputs are identical.        |
| `ToolCatalogueDoesNotExposeWholeTenantExportOrImport`                              | Fact   | Tool catalogue does not expose whole tenant export or import.                           |
| `SnapshotStoresTheExportBodyAsQueryableJsonAndReturnsItsMetadataAsync`             | Fact   | Snapshot stores the export body as queryable json and returns its metadata.             |
| `SnapshotBodyIsQueryableInPostgresAsync`                                           | Fact   | Snapshot body is queryable in postgres.                                                 |
| `SnapshotListReturnsMetadataOnlyAndIsTenantScopedAsync`                            | Fact   | Snapshot list returns metadata only and is tenant scoped.                               |
| `SnapshotImportRestoresTheConfigurationItHoldsAsync`                               | Fact   | Snapshot import restores the configuration it holds.                                    |
| `SnapshotOperationsAreRefusedWithoutThePreservationPermissionAsync`                | Fact   | Snapshot operations are refused without the preservation permission.                    |
| `DeletedSnapshotStopsBeingListedAsync`                                             | Fact   | Deleted snapshot stops being listed.                                                    |
| `SnapshotImportIsRefusedWithoutItsOwnPermissionEvenWithPreservationAsync`          | Fact   | Snapshot import is refused without its own permission even with preservation.           |
| `ProcessSnapshotBypassesPermissionButStillRecordsTheUserAndStaysInTenantAsync`     | Fact   | Process snapshot bypasses permission but still records the user and stays in tenant.    |
| `ProcessSnapshotRefusesToActForThePreservationPageSourceAsync`                     | Fact   | Process snapshot refuses to act for the preservation page source.                       |
| `ProcessImportBypassesThePermissionAndRestoresAsync`                               | Fact   | Process import bypasses the permission and restores.                                    |
| `SnapshotBodyIsReturnedWholeAndIsTenantScopedAsync`                                | Fact   | Snapshot body is returned whole and is tenant scoped.                                   |
| `SnapshotBodyIsRefusedWithoutThePreservationPermissionAsync`                       | Fact   | Snapshot body is refused without the preservation permission.                           |

## Role registry

### RoleRegistryServiceTests

`Jube.Test.Service.Repository.RoleRegistry.RoleRegistryServiceTests` — 36 tests. Category: Service. Collection:
Database.

| Test                                                                        | Kind   | Verifies                                                                          |
|-----------------------------------------------------------------------------|--------|-----------------------------------------------------------------------------------|
| `CreateWithNullOrBlankUserNameThrowsNotAuthenticatedAsync`                  | Theory | Create with null or blank user name throws not authenticated.                     |
| `CreateWithUserHavingNoTenantThrowsNotAuthenticatedAsync`                   | Fact   | Create with user having no tenant throws not authenticated.                       |
| `ListWithoutPermissionThrowsForbiddenAsync`                                 | Fact   | List without permission throws forbidden.                                         |
| `GetByIdWithoutPermissionThrowsForbiddenAsync`                              | Fact   | Get by id without permission throws forbidden.                                    |
| `InsertWithoutPermissionThrowsForbiddenAndWritesNoRowAsync`                 | Fact   | Insert without permission throws forbidden and writes no row.                     |
| `UpdateWithoutPermissionThrowsForbiddenAsync`                               | Fact   | Update without permission throws forbidden.                                       |
| `DeleteWithoutPermissionThrowsForbiddenAsync`                               | Fact   | Delete without permission throws forbidden.                                       |
| `InsertPersistsAndDropsIdentityFieldsAsync`                                 | Fact   | Insert persists and drops identity fields.                                        |
| `UpdateIncrementsVersionPreservesCreatedFieldsAndSetsUpdatedFieldsAsync`    | Fact   | Update increments version preserves created fields and sets updated fields.       |
| `UpdateTamperingIdentityAndAuditFieldsHasNoEffectAsync`                     | Fact   | Update tampering identity and audit fields has no effect.                         |
| `UpdateWithUnknownIdThrowsNotFoundAsync`                                    | Fact   | Update with unknown id throws not found.                                          |
| `UpdateOfLockedRowThrowsNotFoundAsync`                                      | Fact   | Update of locked row throws not found.                                            |
| `UpdatePreservesImportIdAsync`                                              | Fact   | Update preserves import id.                                                       |
| `DeleteRemovesRowAndSubsequentGetReturnsNullAsync`                          | Fact   | Delete removes row and subsequent get returns null.                               |
| `DeleteSetsDeletedUserAsync`                                                | Fact   | Delete sets deleted user.                                                         |
| `DeleteOfMissingRowThrowsNotFoundAsync`                                     | Fact   | Delete of missing row throws not found.                                           |
| `ListIsScopedToCallersTenantAsync`                                          | Fact   | List is scoped to callers tenant.                                                 |
| `UpdateForRowInAnotherTenantThrowsNotFoundAsync`                            | Fact   | Update for row in another tenant throws not found.                                |
| `InsertWithBlankNameThrowsValidationFailedAsync`                            | Fact   | Insert with blank name throws validation failed.                                  |
| `InsertWithNullNameThrowsValidationFailedAndDoesNotThrowNullReferenceAsync` | Fact   | Insert with null name throws validation failed and does not throw null reference. |
| `InsertWithOverLongNameThrowsValidationFailedAsync`                         | Fact   | Insert with over long name throws validation failed.                              |
| `InsertWithDuplicateNameInSameTenantThrowsValidationFailedAsync`            | Fact   | Insert with duplicate name in same tenant throws validation failed.               |
| `InsertWithDuplicateNameAcrossTenantsSucceedsAsync`                         | Fact   | Insert with duplicate name across tenants succeeds.                               |
| `InsertRoundTripsActiveAndLockedAsync`                                      | Fact   | Insert round trips active and locked.                                             |
| `InsertDropsUnpersistedDescriptionFieldAsync`                               | Fact   | Insert drops unpersisted description field.                                       |
| `ListHonoursCancellationAsync`                                              | Fact   | List honours cancellation.                                                        |
| `ListAsyncClampsTakeAndPaginatesAsync`                                      | Fact   | List async clamps take and paginates.                                             |
| `InsertPublishesCreatedChangeEventAsync`                                    | Fact   | Insert publishes created change event.                                            |
| `InsertDoesNotPublishChangeEventOnValidationFailureAsync`                   | Fact   | Insert does not publish change event on validation failure.                       |
| `UpdatePublishesUpdatedChangeEventAsync`                                    | Fact   | Update publishes updated change event.                                            |
| `DeletePublishesDeletedChangeEventAsync`                                    | Fact   | Delete publishes deleted change event.                                            |
| `ListDoesNotPublishAChangeEventAsync`                                       | Fact   | List does not publish a change event.                                             |
| `InsertWarnsNotErrorsOnForbiddenAsync`                                      | Fact   | Insert warns not errors on forbidden.                                             |
| `InsertWritesOneAuditRecordOnSuccessAsync`                                  | Fact   | Insert writes one audit record on success.                                        |
| `ListWritesOneAuditRecordOnSuccessAsync`                                    | Fact   | List writes one audit record on success.                                          |
| `CatalogueRegistersUniqueRoleRegistryToolNames`                             | Fact   | Catalogue registers unique role registry tool names.                              |

## Role registry permission

### RoleRegistryPermissionServiceTests

`Jube.Test.Service.Repository.RoleRegistryPermission.RoleRegistryPermissionServiceTests` — 33 tests. Category: Service.
Collection: Database.

| Test                                                                          | Kind   | Verifies                                                                            |
|-------------------------------------------------------------------------------|--------|-------------------------------------------------------------------------------------|
| `CreateWithNullOrBlankUserNameThrowsNotAuthenticatedAsync`                    | Theory | Create with null or blank user name throws not authenticated.                       |
| `CreateWithUserHavingNoTenantThrowsNotAuthenticatedAsync`                     | Fact   | Create with user having no tenant throws not authenticated.                         |
| `ListWithoutPermissionThrowsForbiddenAsync`                                   | Fact   | List without permission throws forbidden.                                           |
| `GetByIdWithoutPermissionThrowsForbiddenAsync`                                | Fact   | Get by id without permission throws forbidden.                                      |
| `InsertWithoutPermissionThrowsForbiddenAndWritesNoRowAsync`                   | Fact   | Insert without permission throws forbidden and writes no row.                       |
| `UpdateWithoutPermissionThrowsForbiddenAsync`                                 | Fact   | Update without permission throws forbidden.                                         |
| `DeleteWithoutPermissionThrowsForbiddenAsync`                                 | Fact   | Delete without permission throws forbidden.                                         |
| `InsertPersistsAndDropsIdentityFieldsAsync`                                   | Fact   | Insert persists and drops identity fields.                                          |
| `UpdateIncrementsVersionPreservesCreatedFieldsAndSetsUpdatedFieldsAsync`      | Fact   | Update increments version preserves created fields and sets updated fields.         |
| `UpdateTamperingIdentityAndAuditFieldsHasNoEffectAsync`                       | Fact   | Update tampering identity and audit fields has no effect.                           |
| `UpdateWithUnknownIdThrowsNotFoundAsync`                                      | Fact   | Update with unknown id throws not found.                                            |
| `UpdateOfLockedRowThrowsNotFoundAsync`                                        | Fact   | Update of locked row throws not found.                                              |
| `UpdatePreservesImportIdAsync`                                                | Fact   | Update preserves import id.                                                         |
| `DeleteRemovesRowAndSubsequentGetReturnsNullAsync`                            | Fact   | Delete removes row and subsequent get returns null.                                 |
| `DeleteOfMissingRowThrowsNotFoundAsync`                                       | Fact   | Delete of missing row throws not found.                                             |
| `ListIsScopedToCallersTenantAsync`                                            | Fact   | List is scoped to callers tenant.                                                   |
| `InsertForRoleRegistryInAnotherTenantThrowsValidationFailedAsync`             | Fact   | Insert for role registry in another tenant throws validation failed.                |
| `UpdateForRowInAnotherTenantThrowsValidationFailedOnTheReferentialCheckAsync` | Fact   | Update for row in another tenant throws validation failed on the referential check. |
| `InsertWithZeroRoleRegistryIdThrowsValidationFailedAsync`                     | Fact   | Insert with zero role registry id throws validation failed.                         |
| `InsertWithNonExistentRoleRegistryIdThrowsValidationFailedAsync`              | Fact   | Insert with non existent role registry id throws validation failed.                 |
| `InsertWithZeroPermissionSpecificationIdThrowsValidationFailedAsync`          | Fact   | Insert with zero permission specification id throws validation failed.              |
| `InsertWithNonExistentPermissionSpecificationIdThrowsValidationFailedAsync`   | Fact   | Insert with non existent permission specification id throws validation failed.      |
| `InsertWithPermissionAlreadyGrantedToTheSameRoleThrowsValidationFailedAsync`  | Fact   | Insert with permission already granted to the same role throws validation failed.   |
| `UpdateOfSameRowWithSamePermissionDoesNotTriggerDuplicateValidationAsync`     | Fact   | Update of same row with same permission does not trigger duplicate validation.      |
| `InsertRoundTripsActiveAndLockedAsync`                                        | Fact   | Insert round trips active and locked.                                               |
| `ListHonoursCancellationAsync`                                                | Fact   | List honours cancellation.                                                          |
| `InsertPublishesCreatedChangeEventAsync`                                      | Fact   | Insert publishes created change event.                                              |
| `InsertDoesNotPublishChangeEventOnValidationFailureAsync`                     | Fact   | Insert does not publish change event on validation failure.                         |
| `DeletePublishesDeletedChangeEventAsync`                                      | Fact   | Delete publishes deleted change event.                                              |
| `ListDoesNotPublishAChangeEventAsync`                                         | Fact   | List does not publish a change event.                                               |
| `InsertWarnsNotErrorsOnForbiddenAsync`                                        | Fact   | Insert warns not errors on forbidden.                                               |
| `InsertWritesOneAuditRecordOnSuccessAsync`                                    | Fact   | Insert writes one audit record on success.                                          |
| `CatalogueRegistersUniqueRoleRegistryPermissionToolNames`                     | Fact   | Catalogue registers unique role registry permission tool names.                     |

## Sanction entry source

### SanctionEntrySourceServiceTests

`Jube.Test.Service.Repository.SanctionEntrySource.SanctionEntrySourceServiceTests` — 16 tests. Category: Service.
Collection: Database.

| Test                                                                      | Kind | Verifies                                                                      |
|---------------------------------------------------------------------------|------|-------------------------------------------------------------------------------|
| `CreateWithNullUserNameThrowsNotAuthenticatedAsync`                       | Fact | Create with null user name throws not authenticated.                          |
| `CreateWithUnknownTenantThrowsNotAuthenticatedAsync`                      | Fact | Create with unknown tenant throws not authenticated.                          |
| `GetWithoutLandlordThrowsForbiddenAsync`                                  | Fact | Get without landlord throws forbidden.                                        |
| `GetAsLandlordIncludesTheConfiguredSourceAsync`                           | Fact | Get as landlord includes the configured source.                               |
| `ListWithoutLandlordThrowsForbiddenAsync`                                 | Fact | List without landlord throws forbidden.                                       |
| `ListClampsTakeToTwoHundredAsync`                                         | Fact | List clamps take to two hundred.                                              |
| `ListWithAfterIdExcludesThatRowAndIncludesLaterOnesAsync`                 | Fact | List with after id excludes that row and includes later ones.                 |
| `ImportWithoutLandlordThrowsForbiddenAsync`                               | Fact | Import without landlord throws forbidden.                                     |
| `ImportWithNoFileThrowsDtoValidationExceptionAsync`                       | Fact | Import with no file throws dto validation exception.                          |
| `ImportWithEmptyFileThrowsDtoValidationExceptionAsync`                    | Fact | Import with empty file throws dto validation exception.                       |
| `ImportWithUnknownSanctionEntrySourceIdThrowsDtoValidationExceptionAsync` | Fact | Import with unknown sanction entry source id throws dto validation exception. |
| `ImportWithNoDelimiterConfiguredSkipsSilentlyAsync`                       | Fact | Import with no delimiter configured skips silently.                           |
| `ImportInsertsEntryAndRecordsSuccessfulImportRowAsync`                    | Fact | Import inserts entry and records successful import row.                       |
| `ImportDoesNotPublishChangeEventOnValidationFailureAsync`                 | Fact | Import does not publish change event on validation failure.                   |
| `PermissionDeniedIsLoggedAsWarnNotErrorAsync`                             | Fact | Permission denied is logged as warn not error.                                |
| `GetWritesExactlyOneAuditRecordAsync`                                     | Fact | Get writes exactly one audit record.                                          |

## Session case journal

### SessionCaseJournalServiceTests

`Jube.Test.Service.Repository.SessionCaseJournal.SessionCaseJournalServiceTests` — 21 tests. Category: Service.
Collection: Database.

| Test                                                          | Kind | Verifies                                                        |
|---------------------------------------------------------------|------|-----------------------------------------------------------------|
| `CreateServiceWithNullUserNameThrowsNotAuthenticatedAsync`    | Fact | Create service with null user name throws not authenticated.    |
| `CreateServiceWithBlankUserNameThrowsNotAuthenticatedAsync`   | Fact | Create service with blank user name throws not authenticated.   |
| `CreateServiceWithUnknownUserThrowsNotAuthenticatedAsync`     | Fact | Create service with unknown user throws not authenticated.      |
| `CreateServiceWithUserInNoTenantThrowsNotAuthenticatedAsync`  | Fact | Create service with user in no tenant throws not authenticated. |
| `GetWithoutPermissionThrowsForbiddenWithCodeAndSpecsAsync`    | Fact | Get without permission throws forbidden with code and specs.    |
| `CreateWithoutPermissionThrowsForbiddenAndWritesNothingAsync` | Fact | Create without permission throws forbidden and writes nothing.  |
| `CreateWithNullModelThrowsArgumentNullAsync`                  | Fact | Create with null model throws argument null.                    |
| `CreatePersistsAndReturnsServerAssignedFieldsAsync`           | Fact | Create persists and returns server assigned fields.             |
| `CreateIgnoresClientSuppliedOwnerAndIdOnInsertAsync`          | Fact | Create ignores client supplied owner and id on insert.          |
| `CreateTwiceReplacesRatherThanDuplicatesAsync`                | Fact | Create twice replaces rather than duplicates.                   |
| `CreateDoesNotPublishChangeEventAsync`                        | Fact | Create does not publish change event.                           |
| `GetReturnsOwnJournalWithExactFieldMappingAsync`              | Fact | Get returns own journal with exact field mapping.               |
| `GetWithNoJournalReturnsNullAsync`                            | Fact | Get with no journal returns null.                               |
| `GetDoesNotReturnAnotherUsersJournalInSameTenantAsync`        | Fact | Get does not return another users journal in same tenant.       |
| `CreateDoesNotOverwriteAnotherUsersJournalAsync`              | Fact | Create does not overwrite another users journal.                |
| `TenantBJournalIsInvisibleToTenantAAndViceVersaAsync`         | Fact | Tenant b journal is invisible to tenant a and vice versa.       |
| `GetHonoursCancellationAsync`                                 | Fact | Get honours cancellation.                                       |
| `CreateHonoursCancellationAsync`                              | Fact | Create honours cancellation.                                    |
| `ForbiddenWarnsNotErrorsAsync`                                | Fact | Forbidden warns not errors.                                     |
| `EachOperationLogsExactlyOneAuditRecordAsync`                 | Fact | Each operation logs exactly one audit record.                   |
| `ToolCatalogueListsBothUniqueToolNames`                       | Fact | Tool catalogue lists both unique tool names.                    |

## Session case search compiled sql

### SessionCaseSearchCompiledSqlServiceTests

`Jube.Test.Service.Repository.SessionCaseSearchCompiledSql.SessionCaseSearchCompiledSqlServiceTests` — 24 tests.
Category: Service. Collection: Database.

| Test                                                                                     | Kind   | Verifies                                                                                         |
|------------------------------------------------------------------------------------------|--------|--------------------------------------------------------------------------------------------------|
| `CreateWithNullOrBlankUserNameThrowsNotAuthenticatedAsync`                               | Theory | Create with null or blank user name throws not authenticated.                                    |
| `CreateWithUserHavingNoTenantOrUnknownUserThrowsNotAuthenticatedAsync`                   | Fact   | Create with user having no tenant or unknown user throws not authenticated.                      |
| `EveryOperationWithoutPermissionThrowsForbiddenAndWritesNothingAsync`                    | Fact   | Every operation without permission throws forbidden and writes nothing.                          |
| `InsertCompilesValidRulesStoresTheRowAndMapsEveryFieldAsync`                             | Fact   | Insert compiles valid rules stores the row and maps every field.                                 |
| `InsertDoesNotHonourClientSuppliedIdentityOwnerOrTokensAsync`                            | Fact   | Insert does not honour client supplied identity owner or tokens.                                 |
| `InsertWithEmptyFilterRulesStoresAnUnpreparedRowWithTheDatabaseErrorAsync`               | Fact   | Insert with empty filter rules stores an unprepared row with the database error.                 |
| `InsertWithMissingRequiredFieldsThrowsValidationAndStoresNothingAsync`                   | Fact   | Insert with missing required fields throws validation and stores nothing.                        |
| `InsertWithMalformedJsonUnknownFieldOrUnknownWorkflowFailsAsLegacyAndStoresNothingAsync` | Fact   | Insert with malformed json unknown field or unknown workflow fails as legacy and stores nothing. |
| `InsertAgainstAnotherTenantsWorkflowFailsAndStoresNothingAsync`                          | Fact   | Insert against another tenants workflow fails and stores nothing.                                |
| `GetLastWhenNoneExistsReturnsNotFoundPlaceholderAsync`                                   | Fact   | Get last when none exists returns not found placeholder.                                         |
| `GetLastReturnsTheCallersNewestRowNeverAnotherUsersAsync`                                | Fact   | Get last returns the callers newest row never another users.                                     |
| `GetLastRebuildsAFlaggedStaleRowInPlaceAsync`                                            | Fact   | Get last rebuilds a flagged stale row in place.                                                  |
| `GetLastRebuildsAFlaggedRowWithNullRebuildDateOnceOnlyAsync`                             | Fact   | Get last rebuilds a flagged row with null rebuild date once only.                                |
| `GetLastDoesNotStampAFailedRebuildAndRebuildsOnALaterReadAsync`                          | Fact   | Get last does not stamp a failed rebuild and rebuilds on a later read.                           |
| `GetLastDoesNotRebuildUnflaggedRowsAsync`                                                | Fact   | Get last does not rebuild unflagged rows.                                                        |
| `ExecuteByGuidReturnsRowsAndWritesOneExecutionLogRowAsync`                               | Fact   | Execute by guid returns rows and writes one execution log row.                                   |
| `ExecuteByGuidWithNoMatchingCaseReturnsEmptyRowsAndLogsZeroRecordsAsync`                 | Fact   | Execute by guid with no matching case returns empty rows and logs zero records.                  |
| `ExecuteByGuidOfUnknownGuidIsNotFoundAndWritesNothingAsync`                              | Fact   | Execute by guid of unknown guid is not found and writes nothing.                                 |
| `ExecuteByGuidIsOwnerScopedSameTenantOtherUserAndOtherTenantCannotExecuteAsync`          | Fact   | Execute by guid is owner scoped same tenant other user and other tenant cannot execute.          |
| `ExecuteByGuidRebuildsAFlaggedStaleRowInPlaceAndExecutesItAsync`                         | Fact   | Execute by guid rebuilds a flagged stale row in place and executes it.                           |
| `PreCancelledTokenThrowsOperationCanceledAndWritesNothingAsync`                          | Fact   | Pre cancelled token throws operation canceled and writes nothing.                                |
| `EachOperationEmitsOneAuditLineOneSpanAndNoChangeEventAsync`                             | Fact   | Each operation emits one audit line one span and no change event.                                |
| `ForbiddenAndNotFoundAuditTheirOutcomeAndPublishNothingAsync`                            | Fact   | Forbidden and not found audit their outcome and publish nothing.                                 |
| `OnlyTheSafeLastSearchReadIsPublishedAsAnAgentTool`                                      | Fact   | Only the safe last search read is published as an agent tool.                                    |

## Tenant registry

### TenantRegistryServiceTests

`Jube.Test.Service.Repository.TenantRegistry.TenantRegistryServiceTests` — 47 tests. Category: Service. Collection:
Database.

| Test                                                                        | Kind   | Verifies                                                                          |
|-----------------------------------------------------------------------------|--------|-----------------------------------------------------------------------------------|
| `CreateWithNullOrBlankUserNameThrowsNotAuthenticatedAsync`                  | Theory | Create with null or blank user name throws not authenticated.                     |
| `CreateWithUserHavingNoTenantThrowsNotAuthenticatedAsync`                   | Fact   | Create with user having no tenant throws not authenticated.                       |
| `CreateWithUnknownUserThrowsNotAuthenticatedAsync`                          | Fact   | Create with unknown user throws not authenticated.                                |
| `ListWithoutLandlordThrowsForbiddenAsync`                                   | Fact   | List without landlord throws forbidden.                                           |
| `GetByIdWithoutLandlordThrowsForbiddenAsync`                                | Fact   | Get by id without landlord throws forbidden.                                      |
| `GetByFilterWithoutLandlordThrowsForbiddenAsync`                            | Fact   | Get by filter without landlord throws forbidden.                                  |
| `ListPagedWithoutLandlordThrowsForbiddenAsync`                              | Fact   | List paged without landlord throws forbidden.                                     |
| `InsertWithoutLandlordThrowsForbiddenAndWritesNoRowAsync`                   | Fact   | Insert without landlord throws forbidden and writes no row.                       |
| `UpdateWithoutLandlordThrowsForbiddenAsync`                                 | Fact   | Update without landlord throws forbidden.                                         |
| `DeleteWithoutLandlordThrowsForbiddenAsync`                                 | Fact   | Delete without landlord throws forbidden.                                         |
| `InsertAsLandlordPersistsAndDropsIdentityFieldsAsync`                       | Fact   | Insert as landlord persists and drops identity fields.                            |
| `UpdateIncrementsVersionPreservesCreatedFieldsAndSetsUpdatedFieldsAsync`    | Fact   | Update increments version preserves created fields and sets updated fields.       |
| `UpdateTamperingIdentityAndAuditFieldsHasNoEffectAsync`                     | Fact   | Update tampering identity and audit fields has no effect.                         |
| `UpdateWithUnknownIdThrowsNotFoundAsync`                                    | Fact   | Update with unknown id throws not found.                                          |
| `UpdateOfLockedRowThrowsNotFoundAsync`                                      | Fact   | Update of locked row throws not found.                                            |
| `DeleteRemovesRowAndSubsequentGetReturnsNullAsync`                          | Fact   | Delete removes row and subsequent get returns null.                               |
| `DeleteOfMissingRowThrowsNotFoundAsync`                                     | Fact   | Delete of missing row throws not found.                                           |
| `DeleteOfLockedRowThrowsNotFoundAsync`                                      | Fact   | Delete of locked row throws not found.                                            |
| `ListAsLandlordSeesTenantsCreatedByAnyoneAsync`                             | Fact   | List as landlord sees tenants created by anyone.                                  |
| `GetByFilterMatchesCaseInsensitiveSubstringAsync`                           | Fact   | Get by filter matches case insensitive substring.                                 |
| `GetByFilterWithBlankFilterReturnsAllAsync`                                 | Fact   | Get by filter with blank filter returns all.                                      |
| `GetByFilterWithNullFilterDoesNotThrowAsync`                                | Fact   | Get by filter with null filter does not throw.                                    |
| `ListAsyncClampsTakeAndPaginatesAsync`                                      | Fact   | List async clamps take and paginates.                                             |
| `InsertWithBlankNameThrowsValidationFailedAsync`                            | Fact   | Insert with blank name throws validation failed.                                  |
| `InsertWithNullNameThrowsValidationFailedAndDoesNotThrowNullReferenceAsync` | Fact   | Insert with null name throws validation failed and does not throw null reference. |
| `InsertWithWhitespaceNameThrowsValidationFailedAsync`                       | Fact   | Insert with whitespace name throws validation failed.                             |
| `InsertWithOverLongNameThrowsValidationFailedAsync`                         | Fact   | Insert with over long name throws validation failed.                              |
| `InsertWithNameAtMaximumLengthSucceedsAsync`                                | Fact   | Insert with name at maximum length succeeds.                                      |
| `InsertWithDuplicateNameThrowsValidationFailedAsync`                        | Fact   | Insert with duplicate name throws validation failed.                              |
| `InsertWithDuplicateNameDifferentCasingThrowsValidationFailedAsync`         | Fact   | Insert with duplicate name different casing throws validation failed.             |
| `UpdateWithOwnUnchangedNameDoesNotFailDuplicateCheckAsync`                  | Fact   | Update with own unchanged name does not fail duplicate check.                     |
| `InsertRoundTripsActiveAndLockedAsync`                                      | Fact   | Insert round trips active and locked.                                             |
| `DescriptionIsNeverPersistedAsync`                                          | Fact   | Description is never persisted.                                                   |
| `GetByIdForMissingIdReturnsNullAsync`                                       | Fact   | Get by id for missing id returns null.                                            |
| `ListHonoursCancellationAsync`                                              | Fact   | List honours cancellation.                                                        |
| `InsertPublishesCreatedChangeEventAsync`                                    | Fact   | Insert publishes created change event.                                            |
| `InsertDoesNotPublishChangeEventOnValidationFailureAsync`                   | Fact   | Insert does not publish change event on validation failure.                       |
| `DeletePublishesDeletedChangeEventAsync`                                    | Fact   | Delete publishes deleted change event.                                            |
| `ListDoesNotPublishAChangeEventAsync`                                       | Fact   | List does not publish a change event.                                             |
| `ForbiddenOperationDoesNotPublishAChangeEventAsync`                         | Fact   | Forbidden operation does not publish a change event.                              |
| `InsertWarnsNotErrorsOnForbiddenAsync`                                      | Fact   | Insert warns not errors on forbidden.                                             |
| `CreateWithNullUserNameWarnsNotErrorsAsync`                                 | Fact   | Create with null user name warns not errors.                                      |
| `InsertWritesOneAuditRecordOnSuccessAsync`                                  | Fact   | Insert writes one audit record on success.                                        |
| `ListWritesOneAuditRecordEvenThoughItIsAReadAsync`                          | Fact   | List writes one audit record even though it is a read.                            |
| `GetForcedFailureLogsExactlyOneErrorWithExceptionAttachedAsync`             | Fact   | Get forced failure logs exactly one error with exception attached.                |
| `PermissionDeniedMessageIsLocalisedToFrenchAsync`                           | Fact   | Permission denied message is localised to french.                                 |
| `CatalogueRegistersUniqueTenantRegistryToolNames`                           | Fact   | Catalogue registers unique tenant registry tool names.                            |

## User in tenant

### UserInTenantServiceTests

`Jube.Test.Service.Repository.UserInTenant.UserInTenantServiceTests` — 32 tests. Category: Service. Collection:
Database.

| Test                                                               | Kind   | Verifies                                                              |
|--------------------------------------------------------------------|--------|-----------------------------------------------------------------------|
| `CreateWithNullOrBlankUserNameThrowsNotAuthenticatedAsync`         | Theory | Create with null or blank user name throws not authenticated.         |
| `CreateWithUserHavingNoTenantThrowsNotAuthenticatedAsync`          | Fact   | Create with user having no tenant throws not authenticated.           |
| `CreateWithUnknownUserThrowsNotAuthenticatedAsync`                 | Fact   | Create with unknown user throws not authenticated.                    |
| `GetWithoutPermissionThrowsForbiddenAsync`                         | Fact   | Get without permission throws forbidden.                              |
| `GetCurrentTenantRegistryWithoutPermissionThrowsForbiddenAsync`    | Fact   | Get current tenant registry without permission throws forbidden.      |
| `UpdateWithoutLandlordThrowsForbiddenAndWritesNoRowAsync`          | Fact   | Update without landlord throws forbidden and writes no row.           |
| `GetReturnsRowsForCallersTenantAsync`                              | Fact   | Get returns rows for callers tenant.                                  |
| `GetIsScopedToCallersTenantAsync`                                  | Fact   | Get is scoped to callers tenant.                                      |
| `GetForLandlordDoesNotReturnEveryTenantsRowsAsync`                 | Fact   | Get for landlord does not return every tenants rows.                  |
| `GetCurrentTenantRegistryReturnsCallersOwnAllocationAsync`         | Fact   | Get current tenant registry returns callers own allocation.           |
| `MapperToDtoOfNullReturnsNull`                                     | Fact   | Mapper to dto of null returns null.                                   |
| `ListAsyncClampsTakeAsync`                                         | Fact   | List async clamps take.                                               |
| `ListAsyncPaginatesByAfterIdAsync`                                 | Fact   | List async paginates by after id.                                     |
| `UpdateWithNonPositiveTenantRegistryIdThrowsValidationFailedAsync` | Fact   | Update with non positive tenant registry id throws validation failed. |
| `UpdateWithUnknownTenantRegistryIdThrowsValidationFailedAsync`     | Fact   | Update with unknown tenant registry id throws validation failed.      |
| `UpdateAsLandlordSwitchesTenantAndPersistsAsync`                   | Fact   | Update as landlord switches tenant and persists.                      |
| `UpdateToSameTenantSucceedsIdempotentlyAsync`                      | Fact   | Update to same tenant succeeds idempotently.                          |
| `UpdatePublishesUpdatedChangeEventAsync`                           | Fact   | Update publishes updated change event.                                |
| `UpdateDoesNotPublishChangeEventOnValidationFailureAsync`          | Fact   | Update does not publish change event on validation failure.           |
| `UpdateDoesNotPublishChangeEventOnForbiddenAsync`                  | Fact   | Update does not publish change event on forbidden.                    |
| `RepositoryUpdateAsyncSetsUserWhenInsertingNewRowAsync`            | Fact   | Repository update async sets user when inserting new row.             |
| `GetHonoursCancellationAsync`                                      | Fact   | Get honours cancellation.                                             |
| `GetWarnsNotErrorsOnForbiddenAsync`                                | Fact   | Get warns not errors on forbidden.                                    |
| `UpdateLogsOneInfoEntryOnSuccessAsync`                             | Fact   | Update logs one info entry on success.                                |
| `GetLogsOneAuditRecordAsync`                                       | Fact   | Get logs one audit record.                                            |
| `UpdateLogsOneAuditRecordOnSuccessAsync`                           | Fact   | Update logs one audit record on success.                              |
| `GetWithDisabledGatesRecordsNoEntriesAsync`                        | Fact   | Get with disabled gates records no entries.                           |
| `GetOnDisposedContextRecordsErrorWithExceptionAsync`               | Fact   | Get on disposed context records error with exception.                 |
| `ValidationMessageMatchesNeutralResourceAsync`                     | Fact   | Validation message matches neutral resource.                          |
| `ValidationMessageResolvesFrenchCultureAsync`                      | Fact   | Validation message resolves french culture.                           |
| `CatalogueListsUniqueUserInTenantToolNames`                        | Fact   | Catalogue lists unique user in tenant tool names.                     |
| `UpdateWithTamperedUserOnDtoHasNoEffectAsync`                      | Fact   | Update with tampered user on dto has no effect.                       |

## User registry

### UserRegistryServiceTests

`Jube.Test.Service.Repository.UserRegistry.UserRegistryServiceTests` — 45 tests. Category: Service. Collection:
Database.

| Test                                                                  | Kind   | Verifies                                                                     |
|-----------------------------------------------------------------------|--------|------------------------------------------------------------------------------|
| `ConcurrentCreatesOfTheSameUserNameInTwoTenantsLetExactlyOneWinAsync` | Fact   | Concurrent creates of the same user name in two tenants let exactly one win. |
| `InsertPersistsAndReturnsVersionOneAsync`                             | Fact   | Insert persists and returns version one.                                     |
| `GetAllReturnsCreatedRowAndGetByIdReturnsItAsync`                     | Fact   | Get all returns created row and get by id returns it.                        |
| `GetByRoleRegistryGuidReturnsOnlyThatRolesUsersAsync`                 | Fact   | Get by role registry guid returns only that roles users.                     |
| `UpdateIncrementsVersionAndWritesAuditRowAsync`                       | Fact   | Update increments version and writes audit row.                              |
| `DeleteSoftDeletesAndRowDisappearsFromReadsAsync`                     | Fact   | Delete soft deletes and row disappears from reads.                           |
| `EveryMethodThrowsForbiddenWhenPermissionMissingAndWritesNoRowAsync`  | Fact   | Every method throws forbidden when permission missing and writes no row.     |
| `NullOrBlankUserNameThrowsNotAuthenticatedBeforeAnyQueryAsync`        | Theory | Null or blank user name throws not authenticated before any query.           |
| `UserWithNoUserInTenantRowThrowsNotAuthenticatedAsync`                | Fact   | User with no user in tenant row throws not authenticated.                    |
| `UnknownUserThrowsNotAuthenticatedAsync`                              | Fact   | Unknown user throws not authenticated.                                       |
| `UserInTenantBCannotGetUpdateOrDeleteTenantARowAsync`                 | Fact   | User in tenant b cannot get update or delete tenant a row.                   |
| `GetAllAsTenantBNeverContainsTenantARowsAsync`                        | Fact   | Get all as tenant b never contains tenant a rows.                            |
| `NameRequiredRejectsNullEmptyOrWhitespaceAsync`                       | Theory | Name required rejects null empty or whitespace.                              |
| `NameOverMaximumLengthIsRejectedWithErrorCodeAsync`                   | Fact   | Name over maximum length is rejected with error code.                        |
| `DuplicateNameFailsWithinTenantAndAcrossTenantsAsync`                 | Fact   | Duplicate name fails within tenant and across tenants.                       |
| `DuplicateNameDifferingOnlyByCaseWithinTenantFailsAsync`              | Fact   | Duplicate name differing only by case within tenant fails.                   |
| `EmptyRoleRegistryGuidIsRejectedAsync`                                | Fact   | Empty role registry guid is rejected.                                        |
| `NonExistentRoleRegistryGuidIsRejectedAsync`                          | Fact   | Non existent role registry guid is rejected.                                 |
| `RoleRegistryGuidBelongingToAnotherTenantIsRejectedAsync`             | Fact   | Role registry guid belonging to another tenant is rejected.                  |
| `InvalidEmailIsRejectedAsync`                                         | Theory | Invalid email is rejected.                                                   |
| `TamperedIdentityAndAuditFieldsOnInsertHaveNoEffectAsync`             | Fact   | Tampered identity and audit fields on insert have no effect.                 |
| `TamperedIdentityAndAuditFieldsOnUpdateHaveNoEffectAsync`             | Fact   | Tampered identity and audit fields on update have no effect.                 |
| `UpdateOfSoftDeletedRowThrowsNotFoundAsync`                           | Fact   | Update of soft deleted row throws not found.                                 |
| `UpdateOfNeverExistedIdThrowsNotFoundAsync`                           | Fact   | Update of never existed id throws not found.                                 |
| `DeleteOfMissingOrAlreadyDeletedRowThrowsNotFoundAsync`               | Fact   | Delete of missing or already deleted row throws not found.                   |
| `ActiveRoundTripsBoolToByteAndBackAsync`                              | Fact   | Active round trips bool to byte and back.                                    |
| `GetByIdForMissingIdReturnsNullNotExceptionAsync`                     | Fact   | Get by id for missing id returns null not exception.                         |
| `PreCancelledTokenOnReadThrowsOperationCanceledAsync`                 | Fact   | Pre cancelled token on read throws operation canceled.                       |
| `PermissionDeniedLogsWarnNotErrorAndGatesHoldAsync`                   | Fact   | Permission denied logs warn not error and gates hold.                        |
| `SuccessfulInsertLogsExactlyOneInfoAndReadsLogNoInfoAsync`            | Fact   | Successful insert logs exactly one info and reads log no info.               |
| `WithGatesDisabledHappyPathRecordsNoDebugInfoOrWarnAsync`             | Fact   | With gates disabled happy path records no debug info or warn.                |
| `FailureLogsErrorWithExceptionAttachedAndStillPropagatesAsync`        | Fact   | Failure logs error with exception attached and still propagates.             |
| `EachCallEmitsOneSpanWithOutcomeTagAsync`                             | Fact   | Each call emits one span with outcome tag.                                   |
| `EachCallRecordsOneDurationMeasurementAsync`                          | Fact   | Each call records one duration measurement.                                  |
| `AuditLogGetsExactlyOneLineIncludingReadsAsync`                       | Fact   | Audit log gets exactly one line including reads.                             |
| `InsertPublishesExactlyOneCreatedEventAsync`                          | Fact   | Insert publishes exactly one created event.                                  |
| `ReadsAndFailuresPublishNothingAsync`                                 | Fact   | Reads and failures publish nothing.                                          |
| `CatalogueRegistersUniquePascalCaseNoUnderscoreNames`                 | Fact   | Catalogue registers unique pascal case no underscore names.                  |
| `ListAsyncClampsTakeAndPaginatesDeterministicallyAsync`               | Fact   | List async clamps take and paginates deterministically.                      |
| `UpdatePasswordReturnsCleartextOnceAndPersistsArgon2HashAsync`        | Fact   | Update password returns cleartext once and persists argon2 hash.             |
| `UpdatePasswordWithWirePasswordHashSaltsWithNameBeforeHashingAsync`   | Fact   | Update password with wire password hash salts with name before hashing.      |
| `UpdatePasswordForMissingIdThrowsAndLogsErrorInsteadOfNotFoundAsync`  | Fact   | Update password for missing id throws and logs error instead of not found.   |
| `DuplicateNameErrorMessageResolvesFrenchTranslationAsync`             | Fact   | Duplicate name error message resolves french translation.                    |
| `UpdatePreservesPasswordLockedWhenDtoLeavesItUnsetAsync`              | Fact   | Update preserves password locked when dto leaves it unset.                   |
| `UpdateChangesPasswordLockedWhenDtoExplicitlySetsItAsync`             | Fact   | Update changes password locked when dto explicitly sets it.                  |

## User registry api key

### UserRegistryApiKeyServiceTests

`Jube.Test.Service.Repository.UserRegistryApiKey.UserRegistryApiKeyServiceTests` — 33 tests. Category: Service.
Collection: Database.

| Test                                                                            | Kind   | Verifies                                                                               |
|---------------------------------------------------------------------------------|--------|----------------------------------------------------------------------------------------|
| `CreateWithNullOrBlankUserNameThrowsNotAuthenticatedAsync`                      | Theory | Create with null or blank user name throws not authenticated.                          |
| `CreateWithUserHavingNoTenantThrowsNotAuthenticatedAsync`                       | Fact   | Create with user having no tenant throws not authenticated.                            |
| `ListWithoutPermissionThrowsForbiddenAsync`                                     | Fact   | List without permission throws forbidden.                                              |
| `InsertWithoutPermissionThrowsForbiddenAndWritesNoRowAsync`                     | Fact   | Insert without permission throws forbidden and writes no row.                          |
| `DeleteWithoutPermissionThrowsForbiddenAsync`                                   | Fact   | Delete without permission throws forbidden.                                            |
| `InsertPersistsAndReturnsFullPlaintextKeyOnceAsync`                             | Fact   | Insert persists and returns full plaintext key once.                                   |
| `InsertPersistsOnlyTheHashAndAnEightCharacterDisplayPrefixAsync`                | Fact   | Insert persists only the hash and an eight character display prefix.                   |
| `ListReturnsMaskedDisplayNeverTheFullPlaintextKeyAsync`                         | Fact   | List returns masked display never the full plaintext key.                              |
| `InsertWithMissingNameThrowsValidationFailedAsync`                              | Fact   | Insert with missing name throws validation failed.                                     |
| `InsertWithOverlongNameThrowsValidationFailedAsync`                             | Fact   | Insert with overlong name throws validation failed.                                    |
| `InsertWithOverlongDescriptionThrowsValidationFailedAsync`                      | Fact   | Insert with overlong description throws validation failed.                             |
| `InsertWithZeroUserRegistryIdThrowsValidationFailedAsync`                       | Fact   | Insert with zero user registry id throws validation failed.                            |
| `InsertWithUnknownUserRegistryIdThrowsValidationFailedAsync`                    | Fact   | Insert with unknown user registry id throws validation failed.                         |
| `InsertWithCrossTenantUserRegistryIdThrowsValidationFailedAsync`                | Fact   | Insert with cross tenant user registry id throws validation failed.                    |
| `InsertIgnoresClientSuppliedIdAndApiKeyDisplayAsync`                            | Fact   | Insert ignores client supplied id and api key display.                                 |
| `InsertPublishesCreatedChangeEventAsync`                                        | Fact   | Insert publishes created change event.                                                 |
| `InsertDoesNotPublishChangeEventOnValidationFailureAsync`                       | Fact   | Insert does not publish change event on validation failure.                            |
| `InsertPublishesCacheInvalidationWithTheHashAsync`                              | Fact   | Insert publishes cache invalidation with the hash.                                     |
| `ListIsScopedToCallersTenantAsync`                                              | Fact   | List is scoped to callers tenant.                                                      |
| `ListForUnknownUserRegistryIdReturnsEmptyAsync`                                 | Fact   | List for unknown user registry id returns empty.                                       |
| `ListExcludesRevokedKeysAsync`                                                  | Fact   | List excludes revoked keys.                                                            |
| `DeleteSoftDeletesAndPublishesDeletedChangeEventAsync`                          | Fact   | Delete soft deletes and publishes deleted change event.                                |
| `DeletePublishesCacheInvalidationWithTheHashAsync`                              | Fact   | Delete publishes cache invalidation with the hash.                                     |
| `DeleteOfMissingIdSucceedsAsANoOpAsync`                                         | Fact   | Delete of missing id succeeds as a no op.                                              |
| `DeleteOfCrossTenantKeyIsIndistinguishableFromMissingIdAndLeavesRowIntactAsync` | Fact   | Delete of cross tenant key is indistinguishable from missing id and leaves row intact. |
| `DeleteDoesNotPublishChangeEventOnCrossTenantFailureAsync`                      | Fact   | Delete does not publish change event on cross tenant failure.                          |
| `ListHonoursCancellationAsync`                                                  | Fact   | List honours cancellation.                                                             |
| `InsertWarnsNotErrorsOnForbiddenAsync`                                          | Fact   | Insert warns not errors on forbidden.                                                  |
| `InsertLogsOneAuditRecordOnSuccessAsync`                                        | Fact   | Insert logs one audit record on success.                                               |
| `ListLogsOneAuditRecordAsync`                                                   | Fact   | List logs one audit record.                                                            |
| `DeleteOfMissingIdLogsOneAuditRecordAsync`                                      | Fact   | Delete of missing id logs one audit record.                                            |
| `CatalogueListsOnlyTheNonSensitiveUserRegistryApiKeyToolNames`                  | Fact   | Catalogue lists only the non sensitive user registry api key tool names.               |
| `CatalogueDoesNotRegisterCreateAsAnAgentTool`                                   | Fact   | Catalogue does not register create as an agent tool.                                   |

## Visualisation registry

### VisualisationRegistryServiceTests

`Jube.Test.Service.Repository.VisualisationRegistry.VisualisationRegistryServiceTests` — 54 tests. Category: Service.
Collection: Database.

| Test                                                                            | Kind   | Verifies                                                                             |
|---------------------------------------------------------------------------------|--------|--------------------------------------------------------------------------------------|
| `CreateWithNullOrBlankUserNameThrowsNotAuthenticatedAsync`                      | Theory | Create with null or blank user name throws not authenticated.                        |
| `CreateWithUserHavingNoTenantThrowsNotAuthenticatedAsync`                       | Fact   | Create with user having no tenant throws not authenticated.                          |
| `CreateWithUnknownUserThrowsNotAuthenticatedAsync`                              | Fact   | Create with unknown user throws not authenticated.                                   |
| `ListWithoutPermissionThrowsForbiddenAsync`                                     | Fact   | List without permission throws forbidden.                                            |
| `InsertWithoutPermissionThrowsForbiddenAndWritesNoRowAsync`                     | Fact   | Insert without permission throws forbidden and writes no row.                        |
| `ListByShowInDirectoryRequiresExactSpecNotSatisfiedByAdminSpecAsync`            | Fact   | List by show in directory requires exact spec not satisfied by admin spec.           |
| `InsertRequiresAdminSpecNotSatisfiedByShowInDirectorySpecAsync`                 | Fact   | Insert requires admin spec not satisfied by show in directory spec.                  |
| `PermissionDeniedLogsWarnNotErrorAsync`                                         | Fact   | Permission denied logs warn not error.                                               |
| `InsertPersistsAndReturnsVersionOneWithAuditFieldsAsync`                        | Fact   | Insert persists and returns version one with audit fields.                           |
| `InsertWithBlankNameThrowsValidationFailedAsync`                                | Fact   | Insert with blank name throws validation failed.                                     |
| `InsertWithNameOverMaxLengthThrowsValidationFailedAsync`                        | Fact   | Insert with name over max length throws validation failed.                           |
| `InsertWithNameAtMaxLengthSucceedsAsync`                                        | Fact   | Insert with name at max length succeeds.                                             |
| `InsertWithDuplicateNameInSameTenantThrowsValidationFailedAsync`                | Fact   | Insert with duplicate name in same tenant throws validation failed.                  |
| `InsertWithDuplicateNameDifferentCasingInSameTenantThrowsValidationFailedAsync` | Fact   | Insert with duplicate name different casing in same tenant throws validation failed. |
| `InsertWithSameNameInDifferentTenantSucceedsAsync`                              | Fact   | Insert with same name in different tenant succeeds.                                  |
| `InsertWithNonPositiveColumnsThrowsValidationFailedAsync`                       | Theory | Insert with non positive columns throws validation failed.                           |
| `InsertWithNonPositiveColumnWidthThrowsValidationFailedAsync`                   | Theory | Insert with non positive column width throws validation failed.                      |
| `InsertWithNonPositiveRowHeightThrowsValidationFailedAsync`                     | Theory | Insert with non positive row height throws validation failed.                        |
| `InsertIgnoresTamperedIdentityAndAuditFieldsAsync`                              | Fact   | Insert ignores tampered identity and audit fields.                                   |
| `UpdatePreservesCreatedFieldsAndSetsUpdatedFieldsAsync`                         | Fact   | Update preserves created fields and sets updated fields.                             |
| `UpdateWritesAVersionAuditRowCarryingTheDeletedFieldsAsync`                     | Fact   | Update writes a version audit row carrying the deleted fields.                       |
| `UpdatePreservesImportIdAsync`                                                  | Fact   | Update preserves import id.                                                          |
| `UpdateOfUnknownIdThrowsNotFoundAsync`                                          | Fact   | Update of unknown id throws not found.                                               |
| `UpdateOfLockedRowThrowsNotFoundAsync`                                          | Fact   | Update of locked row throws not found.                                               |
| `UpdateOfSoftDeletedRowThrowsNotFoundAsync`                                     | Fact   | Update of soft deleted row throws not found.                                         |
| `UpdateOfCrossTenantRowThrowsNotFoundAsync`                                     | Fact   | Update of cross tenant row throws not found.                                         |
| `UpdateIgnoresTamperedIdentityAndAuditFieldsAsync`                              | Fact   | Update ignores tampered identity and audit fields.                                   |
| `DeleteSoftDeletesAndPublishesDeletedChangeEventAsync`                          | Fact   | Delete soft deletes and publishes deleted change event.                              |
| `DeleteOfUnknownIdThrowsNotFoundAsync`                                          | Fact   | Delete of unknown id throws not found.                                               |
| `DeleteOfLockedRowThrowsNotFoundAsync`                                          | Fact   | Delete of locked row throws not found.                                               |
| `DeleteOfAlreadyDeletedRowThrowsNotFoundAsync`                                  | Fact   | Delete of already deleted row throws not found.                                      |
| `GetByIdCrossTenantReturnsNullAsync`                                            | Fact   | Get by id cross tenant returns null.                                                 |
| `ListNeverContainsAnotherTenantsRowsAsync`                                      | Fact   | List never contains another tenants rows.                                            |
| `GetByGuidActiveOnlyWithoutRoleGrantReturnsNullAsync`                           | Fact   | Get by guid active only without role grant returns null.                             |
| `GetByGuidActiveOnlyWithRoleGrantReturnsRowAsync`                               | Fact   | Get by guid active only with role grant returns row.                                 |
| `GetByGuidActiveOnlyExcludesInactiveRowsAsync`                                  | Fact   | Get by guid active only excludes inactive rows.                                      |
| `GetByGuidActiveOnlyCrossTenantReturnsNullAsync`                                | Fact   | Get by guid active only cross tenant returns null.                                   |
| `ListByShowInDirectoryRequiresRoleGrantAsync`                                   | Fact   | List by show in directory requires role grant.                                       |
| `ListByShowInDirectoryExcludesRowsNotFlaggedAsync`                              | Fact   | List by show in directory excludes rows not flagged.                                 |
| `ListHonoursCancellationAsync`                                                  | Fact   | List honours cancellation.                                                           |
| `RoundTripPreservesLayoutAndFlagFieldsAsync`                                    | Fact   | Round trip preserves layout and flag fields.                                         |
| `GetByIdForMissingRowReturnsNullAsync`                                          | Fact   | Get by id for missing row returns null.                                              |
| `SuccessfulInsertLogsExactlyOneInfoAndReadsLogNoInfoAsync`                      | Fact   | Successful insert logs exactly one info and reads log no info.                       |
| `WithGatesDisabledHappyPathRecordsNoDebugInfoOrWarnAsync`                       | Fact   | With gates disabled happy path records no debug info or warn.                        |
| `FailureLogsErrorWithExceptionAttachedAndStillPropagatesAsync`                  | Fact   | Failure logs error with exception attached and still propagates.                     |
| `EachCallEmitsOneSpanWithOutcomeTagAsync`                                       | Fact   | Each call emits one span with outcome tag.                                           |
| `EachCallRecordsOneDurationMeasurementAsync`                                    | Fact   | Each call records one duration measurement.                                          |
| `AuditLogGetsExactlyOneLineIncludingReadsAsync`                                 | Fact   | Audit log gets exactly one line including reads.                                     |
| `ListDoesNotPublishAChangeEventAsync`                                           | Fact   | List does not publish a change event.                                                |
| `FailedInsertPublishesNothingAsync`                                             | Fact   | Failed insert publishes nothing.                                                     |
| `SuccessfulUpdatePublishesExactlyOneUpdatedEventAsync`                          | Fact   | Successful update publishes exactly one updated event.                               |
| `InsertWithBlankNameUsesFrenchMessageUnderFrenchCultureAsync`                   | Fact   | Insert with blank name uses french message under french culture.                     |
| `CatalogueRegistersExpectedToolNamesForThisArea`                                | Fact   | Catalogue registers expected tool names for this area.                               |
| `ListAsyncClampsTakeAndPaginatesDeterministicallyAsync`                         | Fact   | List async clamps take and paginates deterministically.                              |

## Visualisation registry datasource

### VisualisationRegistryDatasourceServiceTests

`Jube.Test.Service.Repository.VisualisationRegistryDatasource.VisualisationRegistryDatasourceServiceTests` — 47 tests.
Category: Service. Collection: Database.

| Test                                                                                     | Kind   | Verifies                                                                                         |
|------------------------------------------------------------------------------------------|--------|--------------------------------------------------------------------------------------------------|
| `InsertPersistsAndReturnsVersionOneAsync`                                                | Fact   | Insert persists and returns version one.                                                         |
| `GetAllReturnsCreatedRowAndGetByIdReturnsItAsync`                                        | Fact   | Get all returns created row and get by id returns it.                                            |
| `GetByVisualisationRegistryIdReturnsOnlyRowsForThatParentAsync`                          | Fact   | Get by visualisation registry id returns only rows for that parent.                              |
| `GetByVisualisationRegistryIdActiveOnlyRequiresRoleVisibilityOnBothParentAndRowAsync`    | Fact   | Get by visualisation registry id active only requires role visibility on both parent and row.    |
| `UpdatePreservesCreatedFieldsSetsUpdatedFieldsAndKeepsImportIdAsync`                     | Fact   | Update preserves created fields sets updated fields and keeps import id.                         |
| `UserInTenantBCannotUpdateOrDeleteTenantARowAsync`                                       | Fact   | User in tenant b cannot update or delete tenant a row.                                           |
| `GetAllAsTenantBNeverContainsTenantARowsAsync`                                           | Fact   | Get all as tenant b never contains tenant a rows.                                                |
| `DeleteSoftDeletesAndRowDisappearsFromReadsAsync`                                        | Fact   | Delete soft deletes and row disappears from reads.                                               |
| `EveryMethodThrowsForbiddenWhenPermissionMissingAndWritesNoRowAsync`                     | Fact   | Every method throws forbidden when permission missing and writes no row.                         |
| `NullOrBlankUserNameThrowsNotAuthenticatedBeforeAnyQueryAsync`                           | Theory | Null or blank user name throws not authenticated before any query.                               |
| `UserWithNoUserInTenantRowThrowsNotAuthenticatedAsync`                                   | Fact   | User with no user in tenant row throws not authenticated.                                        |
| `UnknownUserThrowsNotAuthenticatedAsync`                                                 | Fact   | Unknown user throws not authenticated.                                                           |
| `NameRequiredRejectsNullEmptyOrWhitespaceAsync`                                          | Theory | Name required rejects null empty or whitespace.                                                  |
| `NameOverMaximumLengthIsRejectedWithErrorCodeAsync`                                      | Fact   | Name over maximum length is rejected with error code.                                            |
| `DuplicateNameWithinSameParentFailsButDifferentParentSucceedsAsync`                      | Fact   | Duplicate name within same parent fails but different parent succeeds.                           |
| `InvalidVisualisationRegistryIdIsRejectedAsync`                                          | Fact   | Invalid visualisation registry id is rejected.                                                   |
| `VisualisationRegistryIdFromAnotherTenantIsRejectedAsNotFoundAsync`                      | Fact   | Visualisation registry id from another tenant is rejected as not found.                          |
| `CommandRequiredRejectsEmptyAsync`                                                       | Fact   | Command required rejects empty.                                                                  |
| `PriorityColumnSpanAndRowSpanNegativeAreRejectedAsync`                                   | Theory | Priority column span and row span negative are rejected.                                         |
| `VisualisationTypeAndTextAreValidatedOnlyWhenIncludeDisplayEnabledAsync`                 | Fact   | Visualisation type and text are validated only when include display enabled.                     |
| `InsertWithInvalidSqlThrowsSqlValidationFailedExceptionAsync`                            | Fact   | Insert with invalid sql throws sql validation failed exception.                                  |
| `InsertWithSqlTheDatabaseRefusesSurfacesTheDatabaseMessageAsync`                         | Theory | Insert with sql the database refuses surfaces the database message.                              |
| `InsertWithAnUnregisteredTableIsRefusedByTheTableAllowListAsync`                         | Fact   | Insert with an unregistered table is refused by the table allow list.                            |
| `InsertWithAnUnregisteredTableSurfacesTheDatabaseMessageWhenTheAllowListIsDisabledAsync` | Fact   | Insert with an unregistered table surfaces the database message when the allow list is disabled. |
| `InsertOverARegisteredTableSavesInAllowListModeAsync`                                    | Fact   | Insert over a registered table saves in allow list mode.                                         |
| `InsertWithNonSelectSqlThrowsSqlValidationFailedExceptionWhenSelectOnlyEnforcedAsync`    | Fact   | Insert with non select sql throws sql validation failed exception when select only enforced.     |
| `InsertWithValidSqlPopulatesSeriesAsync`                                                 | Fact   | Insert with valid sql populates series.                                                          |
| `TamperedGuidOnInsertHasNoEffectAsync`                                                   | Fact   | Tampered guid on insert has no effect.                                                           |
| `TamperedIdentityAndAuditFieldsOnUpdateHaveNoEffectAsync`                                | Fact   | Tampered identity and audit fields on update have no effect.                                     |
| `UpdateOfLockedRowThrowsNotFoundAsync`                                                   | Fact   | Update of locked row throws not found.                                                           |
| `UpdateOfSoftDeletedRowThrowsNotFoundAsync`                                              | Fact   | Update of soft deleted row throws not found.                                                     |
| `UpdateOfNeverExistedIdThrowsNotFoundAsync`                                              | Fact   | Update of never existed id throws not found.                                                     |
| `DeleteOfMissingOrAlreadyDeletedRowThrowsNotFoundAsync`                                  | Fact   | Delete of missing or already deleted row throws not found.                                       |
| `ActiveAndLockedRoundTripBoolToByteAndBackAsync`                                         | Fact   | Active and locked round trip bool to byte and back.                                              |
| `DescriptionAndColumnsLegacyFieldsAreNeverPersistedAsync`                                | Fact   | Description and columns legacy fields are never persisted.                                       |
| `GetByIdForMissingIdReturnsNullNotExceptionAsync`                                        | Fact   | Get by id for missing id returns null not exception.                                             |
| `PreCancelledTokenOnReadThrowsOperationCanceledAsync`                                    | Fact   | Pre cancelled token on read throws operation canceled.                                           |
| `PermissionDeniedLogsWarnNotErrorAndGatesHoldAsync`                                      | Fact   | Permission denied logs warn not error and gates hold.                                            |
| `SuccessfulInsertLogsExactlyOneInfoAndReadsLogNoInfoAsync`                               | Fact   | Successful insert logs exactly one info and reads log no info.                                   |
| `WithGatesDisabledHappyPathRecordsNoDebugInfoOrWarnAsync`                                | Fact   | With gates disabled happy path records no debug info or warn.                                    |
| `FailureLogsErrorWithExceptionAttachedAndStillPropagatesAsync`                           | Fact   | Failure logs error with exception attached and still propagates.                                 |
| `EachCallEmitsOneSpanWithOutcomeTagAsync`                                                | Fact   | Each call emits one span with outcome tag.                                                       |
| `EachCallRecordsOneDurationMeasurementAsync`                                             | Fact   | Each call records one duration measurement.                                                      |
| `AuditLogGetsExactlyOneLineIncludingReadsAsync`                                          | Fact   | Audit log gets exactly one line including reads.                                                 |
| `InsertPublishesExactlyOneCreatedEventAsync`                                             | Fact   | Insert publishes exactly one created event.                                                      |
| `ReadsAndFailuresPublishNothingAsync`                                                    | Fact   | Reads and failures publish nothing.                                                              |
| `ListAsyncClampsTakeAndPaginatesDeterministicallyAsync`                                  | Fact   | List async clamps take and paginates deterministically.                                          |

## Visualisation registry datasource role

### VisualisationRegistryDatasourceRoleServiceTests

`Jube.Test.Service.Repository.VisualisationRegistryDatasourceRole.VisualisationRegistryDatasourceRoleServiceTests` — 31
tests. Category: Service. Collection: Database.

| Test                                                                                  | Kind   | Verifies                                                                                  |
|---------------------------------------------------------------------------------------|--------|-------------------------------------------------------------------------------------------|
| `CreateWithNullOrBlankUserNameThrowsNotAuthenticatedAsync`                            | Theory | Create with null or blank user name throws not authenticated.                             |
| `CreateWithUserHavingNoTenantThrowsNotAuthenticatedAsync`                             | Fact   | Create with user having no tenant throws not authenticated.                               |
| `ListWithoutPermissionThrowsForbiddenAsync`                                           | Fact   | List without permission throws forbidden.                                                 |
| `InsertWithoutPermissionThrowsForbiddenAndWritesNoRowAsync`                           | Fact   | Insert without permission throws forbidden and writes no row.                             |
| `DeleteWithoutPermissionThrowsForbiddenAsync`                                         | Fact   | Delete without permission throws forbidden.                                               |
| `InsertPersistsAndReturnsGrantAsync`                                                  | Fact   | Insert persists and returns grant.                                                        |
| `InsertWithEmptyVisualisationRegistryDatasourceGuidThrowsValidationFailedAsync`       | Fact   | Insert with empty visualisation registry datasource guid throws validation failed.        |
| `InsertWithUnknownVisualisationRegistryDatasourceGuidThrowsValidationFailedAsync`     | Fact   | Insert with unknown visualisation registry datasource guid throws validation failed.      |
| `InsertWithCrossTenantVisualisationRegistryDatasourceGuidThrowsValidationFailedAsync` | Fact   | Insert with cross tenant visualisation registry datasource guid throws validation failed. |
| `InsertWithEmptyRoleRegistryGuidThrowsValidationFailedAsync`                          | Fact   | Insert with empty role registry guid throws validation failed.                            |
| `InsertWithCrossTenantRoleRegistryGuidThrowsValidationFailedAsync`                    | Fact   | Insert with cross tenant role registry guid throws validation failed.                     |
| `InsertPublishesCreatedChangeEventAsync`                                              | Fact   | Insert publishes created change event.                                                    |
| `InsertDoesNotPublishChangeEventOnValidationFailureAsync`                             | Fact   | Insert does not publish change event on validation failure.                               |
| `ListByVisualisationRegistryDatasourceGuidReturnsGrantsAsync`                         | Fact   | List by visualisation registry datasource guid returns grants.                            |
| `ListForUnknownDatasourceGuidReturnsEmptyAsync`                                       | Fact   | List for unknown datasource guid returns empty.                                           |
| `ListIsScopedToCallersTenantAsync`                                                    | Fact   | List is scoped to callers tenant.                                                         |
| `ListExcludesDeletedGrantsAsync`                                                      | Fact   | List excludes deleted grants.                                                             |
| `DeleteSoftDeletesAndPublishesDeletedChangeEventAsync`                                | Fact   | Delete soft deletes and publishes deleted change event.                                   |
| `DeleteOfMissingRowThrowsKeyNotFoundExceptionNotA404OrA204Async`                      | Fact   | Delete of missing row throws key not found exception not a 404 or a 204.                  |
| `DeleteOfCrossTenantRowThrowsKeyNotFoundExceptionAsync`                               | Fact   | Delete of cross tenant row throws key not found exception.                                |
| `DeleteDoesNotPublishChangeEventOnFailureAsync`                                       | Fact   | Delete does not publish change event on failure.                                          |
| `ListHonoursCancellationAsync`                                                        | Fact   | List honours cancellation.                                                                |
| `InsertWarnsNotErrorsOnForbiddenAsync`                                                | Fact   | Insert warns not errors on forbidden.                                                     |
| `InsertLogsOneAuditRecordOnSuccessAsync`                                              | Fact   | Insert logs one audit record on success.                                                  |
| `ListLogsOneAuditRecordAsync`                                                         | Fact   | List logs one audit record.                                                               |
| `CatalogueListsUniqueVisualisationRegistryDatasourceRoleToolNames`                    | Fact   | Catalogue lists unique visualisation registry datasource role tool names.                 |
| `RepeatedGrantReturnsTheExistingGrantAndWritesNoSecondRowAsync`                       | Fact   | Repeated grant returns the existing grant and writes no second row.                       |
| `RepeatedGrantAfterRevokeCreatesANewLiveGrantAsync`                                   | Fact   | Repeated grant after revoke creates a new live grant.                                     |
| `RepeatedGrantAndRevokeLeavesNoAccessBehindAsync`                                     | Fact   | Repeated grant and revoke leaves no access behind.                                        |
| `TheSameRoleOnTwoParentsIsTwoIndependentGrantsAsync`                                  | Fact   | The same role on two parents is two independent grants.                                   |
| `ConcurrentIdenticalGrantsProduceExactlyOneLiveRowAsync`                              | Fact   | Concurrent identical grants produce exactly one live row.                                 |

## Visualisation registry datasource series

### VisualisationRegistryDatasourceSeriesServiceTests

`Jube.Test.Service.Repository.VisualisationRegistryDatasourceSeries.VisualisationRegistryDatasourceSeriesServiceTests` — 12 tests. Category: Service. Collection: Database.

| Test                                                                 | Kind   | Verifies                                                                    |
|----------------------------------------------------------------------|--------|-----------------------------------------------------------------------------|
| `CreateWithNullOrBlankUserNameThrowsNotAuthenticatedAsync`           | Theory | Create with null or blank user name throws not authenticated.               |
| `CreateWithUserHavingNoTenantThrowsNotAuthenticatedAsync`            | Fact   | Create with user having no tenant throws not authenticated.                 |
| `ListWithoutPermissionThrowsForbiddenAsync`                          | Fact   | List without permission throws forbidden.                                   |
| `ListByVisualisationRegistryDatasourceIdReturnsSeriesAsync`          | Fact   | List by visualisation registry datasource id returns series.                |
| `ListForUnknownDatasourceIdReturnsEmptyAsync`                        | Fact   | List for unknown datasource id returns empty.                               |
| `ListIsScopedToCallersTenantAsync`                                   | Fact   | List is scoped to callers tenant.                                           |
| `ListExcludesSeriesOfSoftDeletedDatasourceAsync`                     | Fact   | List excludes series of soft deleted datasource.                            |
| `ListHonoursCancellationAsync`                                       | Fact   | List honours cancellation.                                                  |
| `ListWarnsNotErrorsOnForbiddenAsync`                                 | Fact   | List warns not errors on forbidden.                                         |
| `ListLogsOneAuditRecordAsync`                                        | Fact   | List logs one audit record.                                                 |
| `ListDoesNotPublishChangeEventAsync`                                 | Fact   | List does not publish change event.                                         |
| `CatalogueListsUniqueVisualisationRegistryDatasourceSeriesToolNames` | Fact   | Catalogue lists unique visualisation registry datasource series tool names. |

## Visualisation registry parameter

### VisualisationRegistryParameterServiceTests

`Jube.Test.Service.Repository.VisualisationRegistryParameter.VisualisationRegistryParameterServiceTests` — 46 tests.
Category: Service. Collection: Database.

| Test                                                                                      | Kind   | Verifies                                                                                          |
|-------------------------------------------------------------------------------------------|--------|---------------------------------------------------------------------------------------------------|
| `InsertPersistsAndReturnsVersionOneAsync`                                                 | Fact   | Insert persists and returns version one.                                                          |
| `GetAllReturnsCreatedRowAsync`                                                            | Fact   | Get all returns created row.                                                                      |
| `GetByVisualisationRegistryIdReturnsRowsForThatRegistryIncludingInactiveOrderedByIdAsync` | Fact   | Get by visualisation registry id returns rows for that registry including inactive ordered by id. |
| `GetByVisualisationRegistryIdWithZeroOrNonExistentIdReturnsEmptyListAsync`                | Theory | Get by visualisation registry id with zero or non existent id returns empty list.                 |
| `UpdateIncrementsVersionSetsUpdatedUserPreservesCreatedUserAndWritesAuditRowAsync`        | Fact   | Update increments version sets updated user preserves created user and writes audit row.          |
| `UpdatePreservesImportIdAsync`                                                            | Fact   | Update preserves import id.                                                                       |
| `DeleteSoftDeletesAndRowDisappearsFromByVisualisationRegistryIdAsync`                     | Fact   | Delete soft deletes and row disappears from by visualisation registry id.                         |
| `EveryMethodThrowsForbiddenWhenPermissionMissingAndWritesNoRowAsync`                      | Fact   | Every method throws forbidden when permission missing and writes no row.                          |
| `NullOrBlankUserNameThrowsNotAuthenticatedBeforeAnyQueryAsync`                            | Theory | Null or blank user name throws not authenticated before any query.                                |
| `UserWithNoUserInTenantRowThrowsNotAuthenticatedAsync`                                    | Fact   | User with no user in tenant row throws not authenticated.                                         |
| `UnknownUserThrowsNotAuthenticatedAsync`                                                  | Fact   | Unknown user throws not authenticated.                                                            |
| `UserInTenantBCannotUpdateOrDeleteTenantARowAsync`                                        | Fact   | User in tenant b cannot update or delete tenant a row.                                            |
| `GetByVisualisationRegistryIdAsTenantBNeverContainsTenantARowsAsync`                      | Fact   | Get by visualisation registry id as tenant b never contains tenant a rows.                        |
| `NameRequiredRejectsNullEmptyOrWhitespaceAsync`                                           | Theory | Name required rejects null empty or whitespace.                                                   |
| `NameOverMaximumLengthIsRejectedAsync`                                                    | Fact   | Name over maximum length is rejected.                                                             |
| `NameDuplicateWithinRegistryIsRejectedButAcrossRegistriesIsAcceptedAsync`                 | Fact   | Name duplicate within registry is rejected but across registries is accepted.                     |
| `InvalidDataTypeIdIsRejectedAsync`                                                        | Theory | Invalid data type id is rejected.                                                                 |
| `ValidDataTypeIdsAreAcceptedAsync`                                                        | Theory | Valid data type ids are accepted.                                                                 |
| `DefaultValueRequiredRejectsNullEmptyOrWhitespaceAsync`                                   | Theory | Default value required rejects null empty or whitespace.                                          |
| `DefaultValueNotParseableForDataTypeIsRejectedAsync`                                      | Theory | Default value not parseable for data type is rejected.                                            |
| `DefaultValueParseableForDataTypeIsAcceptedAsync`                                         | Theory | Default value parseable for data type is accepted.                                                |
| `DefaultValueOverMaximumLengthIsRejectedAsync`                                            | Fact   | Default value over maximum length is rejected.                                                    |
| `InvalidVisualisationRegistryIdIsRejectedAsync`                                           | Fact   | Invalid visualisation registry id is rejected.                                                    |
| `NonExistentVisualisationRegistryIdIsRejectedAsync`                                       | Fact   | Non existent visualisation registry id is rejected.                                               |
| `VisualisationRegistryIdBelongingToAnotherTenantIsRejectedAsync`                          | Fact   | Visualisation registry id belonging to another tenant is rejected.                                |
| `TamperedIdentityAndAuditFieldsOnInsertHaveNoEffectAsync`                                 | Fact   | Tampered identity and audit fields on insert have no effect.                                      |
| `TamperedIdentityAndAuditFieldsOnUpdateHaveNoEffectAsync`                                 | Fact   | Tampered identity and audit fields on update have no effect.                                      |
| `UpdateOfLockedRowThrowsNotFoundAsync`                                                    | Fact   | Update of locked row throws not found.                                                            |
| `UpdateOfSoftDeletedRowThrowsNotFoundAsync`                                               | Fact   | Update of soft deleted row throws not found.                                                      |
| `UpdateOfNeverExistedIdThrowsNotFoundAsync`                                               | Fact   | Update of never existed id throws not found.                                                      |
| `DeleteOfLockedRowThrowsNotFoundAsync`                                                    | Fact   | Delete of locked row throws not found.                                                            |
| `DeleteOfMissingOrAlreadyDeletedRowThrowsNotFoundAsync`                                   | Fact   | Delete of missing or already deleted row throws not found.                                        |
| `ActiveOnlyRequiresBothVisualisationRegistryAndParameterRoleGrantsAsync`                  | Fact   | Active only requires both visualisation registry and parameter role grants.                       |
| `ActiveOnlyExcludesInactiveParameterEvenWithBothRoleGrantsAsync`                          | Fact   | Active only excludes inactive parameter even with both role grants.                               |
| `PreCancelledTokenOnReadThrowsOperationCanceledAsync`                                     | Fact   | Pre cancelled token on read throws operation canceled.                                            |
| `PermissionDeniedLogsWarnNotErrorAndGatesHoldAsync`                                       | Fact   | Permission denied logs warn not error and gates hold.                                             |
| `SuccessfulInsertLogsExactlyOneInfoAndReadsLogNoInfoAsync`                                | Fact   | Successful insert logs exactly one info and reads log no info.                                    |
| `WithGatesDisabledHappyPathRecordsNoDebugInfoOrWarnAsync`                                 | Fact   | With gates disabled happy path records no debug info or warn.                                     |
| `FailureLogsErrorWithExceptionAttachedAndStillPropagatesAsync`                            | Fact   | Failure logs error with exception attached and still propagates.                                  |
| `EachCallEmitsOneSpanWithOutcomeTagAsync`                                                 | Fact   | Each call emits one span with outcome tag.                                                        |
| `EachCallRecordsOneDurationMeasurementAsync`                                              | Fact   | Each call records one duration measurement.                                                       |
| `AuditLogGetsExactlyOneLineIncludingReadsAsync`                                           | Fact   | Audit log gets exactly one line including reads.                                                  |
| `InsertPublishesExactlyOneCreatedEventAsync`                                              | Fact   | Insert publishes exactly one created event.                                                       |
| `ReadsAndFailuresPublishNothingAsync`                                                     | Fact   | Reads and failures publish nothing.                                                               |
| `CatalogueRegistersUniquePascalCaseNoUnderscoreNames`                                     | Fact   | Catalogue registers unique pascal case no underscore names.                                       |
| `ListAsyncClampsTakeAndPaginatesDeterministicallyAsync`                                   | Fact   | List async clamps take and paginates deterministically.                                           |

## Visualisation registry parameter role

### VisualisationRegistryParameterRoleServiceTests

`Jube.Test.Service.Repository.VisualisationRegistryParameterRole.VisualisationRegistryParameterRoleServiceTests` — 31
tests. Category: Service. Collection: Database.

| Test                                                                                 | Kind   | Verifies                                                                                 |
|--------------------------------------------------------------------------------------|--------|------------------------------------------------------------------------------------------|
| `CreateWithNullOrBlankUserNameThrowsNotAuthenticatedAsync`                           | Theory | Create with null or blank user name throws not authenticated.                            |
| `CreateWithUserHavingNoTenantThrowsNotAuthenticatedAsync`                            | Fact   | Create with user having no tenant throws not authenticated.                              |
| `ListWithoutPermissionThrowsForbiddenAsync`                                          | Fact   | List without permission throws forbidden.                                                |
| `InsertWithoutPermissionThrowsForbiddenAndWritesNoRowAsync`                          | Fact   | Insert without permission throws forbidden and writes no row.                            |
| `DeleteWithoutPermissionThrowsForbiddenAsync`                                        | Fact   | Delete without permission throws forbidden.                                              |
| `InsertPersistsAndReturnsGrantAsync`                                                 | Fact   | Insert persists and returns grant.                                                       |
| `InsertWithEmptyVisualisationRegistryParameterGuidThrowsValidationFailedAsync`       | Fact   | Insert with empty visualisation registry parameter guid throws validation failed.        |
| `InsertWithUnknownVisualisationRegistryParameterGuidThrowsValidationFailedAsync`     | Fact   | Insert with unknown visualisation registry parameter guid throws validation failed.      |
| `InsertWithCrossTenantVisualisationRegistryParameterGuidThrowsValidationFailedAsync` | Fact   | Insert with cross tenant visualisation registry parameter guid throws validation failed. |
| `InsertWithEmptyRoleRegistryGuidThrowsValidationFailedAsync`                         | Fact   | Insert with empty role registry guid throws validation failed.                           |
| `InsertWithCrossTenantRoleRegistryGuidThrowsValidationFailedAsync`                   | Fact   | Insert with cross tenant role registry guid throws validation failed.                    |
| `InsertPublishesCreatedChangeEventAsync`                                             | Fact   | Insert publishes created change event.                                                   |
| `InsertDoesNotPublishChangeEventOnValidationFailureAsync`                            | Fact   | Insert does not publish change event on validation failure.                              |
| `ListByVisualisationRegistryParameterGuidReturnsGrantsAsync`                         | Fact   | List by visualisation registry parameter guid returns grants.                            |
| `ListForUnknownParameterGuidReturnsEmptyAsync`                                       | Fact   | List for unknown parameter guid returns empty.                                           |
| `ListIsScopedToCallersTenantAsync`                                                   | Fact   | List is scoped to callers tenant.                                                        |
| `ListExcludesDeletedGrantsAsync`                                                     | Fact   | List excludes deleted grants.                                                            |
| `DeleteSoftDeletesAndPublishesDeletedChangeEventAsync`                               | Fact   | Delete soft deletes and publishes deleted change event.                                  |
| `DeleteOfMissingRowThrowsKeyNotFoundExceptionNotA404OrA204Async`                     | Fact   | Delete of missing row throws key not found exception not a 404 or a 204.                 |
| `DeleteOfCrossTenantRowThrowsKeyNotFoundExceptionAsync`                              | Fact   | Delete of cross tenant row throws key not found exception.                               |
| `DeleteDoesNotPublishChangeEventOnFailureAsync`                                      | Fact   | Delete does not publish change event on failure.                                         |
| `ListHonoursCancellationAsync`                                                       | Fact   | List honours cancellation.                                                               |
| `InsertWarnsNotErrorsOnForbiddenAsync`                                               | Fact   | Insert warns not errors on forbidden.                                                    |
| `InsertLogsOneAuditRecordOnSuccessAsync`                                             | Fact   | Insert logs one audit record on success.                                                 |
| `ListLogsOneAuditRecordAsync`                                                        | Fact   | List logs one audit record.                                                              |
| `CatalogueListsUniqueVisualisationRegistryParameterRoleToolNames`                    | Fact   | Catalogue lists unique visualisation registry parameter role tool names.                 |
| `RepeatedGrantReturnsTheExistingGrantAndWritesNoSecondRowAsync`                      | Fact   | Repeated grant returns the existing grant and writes no second row.                      |
| `RepeatedGrantAfterRevokeCreatesANewLiveGrantAsync`                                  | Fact   | Repeated grant after revoke creates a new live grant.                                    |
| `RepeatedGrantAndRevokeLeavesNoAccessBehindAsync`                                    | Fact   | Repeated grant and revoke leaves no access behind.                                       |
| `TheSameRoleOnTwoParentsIsTwoIndependentGrantsAsync`                                 | Fact   | The same role on two parents is two independent grants.                                  |
| `ConcurrentIdenticalGrantsProduceExactlyOneLiveRowAsync`                             | Fact   | Concurrent identical grants produce exactly one live row.                                |

## Visualisation registry role

### VisualisationRegistryRoleServiceTests

`Jube.Test.Service.Repository.VisualisationRegistryRole.VisualisationRegistryRoleServiceTests` — 31 tests. Category:
Service. Collection: Database.

| Test                                                                        | Kind   | Verifies                                                                       |
|-----------------------------------------------------------------------------|--------|--------------------------------------------------------------------------------|
| `CreateWithNullOrBlankUserNameThrowsNotAuthenticatedAsync`                  | Theory | Create with null or blank user name throws not authenticated.                  |
| `CreateWithUserHavingNoTenantThrowsNotAuthenticatedAsync`                   | Fact   | Create with user having no tenant throws not authenticated.                    |
| `ListWithoutPermissionThrowsForbiddenAsync`                                 | Fact   | List without permission throws forbidden.                                      |
| `InsertWithoutPermissionThrowsForbiddenAndWritesNoRowAsync`                 | Fact   | Insert without permission throws forbidden and writes no row.                  |
| `DeleteWithoutPermissionThrowsForbiddenAsync`                               | Fact   | Delete without permission throws forbidden.                                    |
| `InsertPersistsAndReturnsGrantAsync`                                        | Fact   | Insert persists and returns grant.                                             |
| `InsertWithEmptyVisualisationRegistryGuidThrowsValidationFailedAsync`       | Fact   | Insert with empty visualisation registry guid throws validation failed.        |
| `InsertWithUnknownVisualisationRegistryGuidThrowsValidationFailedAsync`     | Fact   | Insert with unknown visualisation registry guid throws validation failed.      |
| `InsertWithCrossTenantVisualisationRegistryGuidThrowsValidationFailedAsync` | Fact   | Insert with cross tenant visualisation registry guid throws validation failed. |
| `InsertWithEmptyRoleRegistryGuidThrowsValidationFailedAsync`                | Fact   | Insert with empty role registry guid throws validation failed.                 |
| `InsertWithCrossTenantRoleRegistryGuidThrowsValidationFailedAsync`          | Fact   | Insert with cross tenant role registry guid throws validation failed.          |
| `InsertPublishesCreatedChangeEventAsync`                                    | Fact   | Insert publishes created change event.                                         |
| `InsertDoesNotPublishChangeEventOnValidationFailureAsync`                   | Fact   | Insert does not publish change event on validation failure.                    |
| `ListByVisualisationRegistryGuidReturnsGrantsAsync`                         | Fact   | List by visualisation registry guid returns grants.                            |
| `ListForUnknownVisualisationRegistryGuidReturnsEmptyAsync`                  | Fact   | List for unknown visualisation registry guid returns empty.                    |
| `ListIsScopedToCallersTenantAsync`                                          | Fact   | List is scoped to callers tenant.                                              |
| `ListExcludesDeletedGrantsAsync`                                            | Fact   | List excludes deleted grants.                                                  |
| `DeleteSoftDeletesAndPublishesDeletedChangeEventAsync`                      | Fact   | Delete soft deletes and publishes deleted change event.                        |
| `DeleteOfMissingRowThrowsKeyNotFoundExceptionNotA404OrA204Async`            | Fact   | Delete of missing row throws key not found exception not a 404 or a 204.       |
| `DeleteOfCrossTenantRowThrowsKeyNotFoundExceptionAsync`                     | Fact   | Delete of cross tenant row throws key not found exception.                     |
| `DeleteDoesNotPublishChangeEventOnFailureAsync`                             | Fact   | Delete does not publish change event on failure.                               |
| `ListHonoursCancellationAsync`                                              | Fact   | List honours cancellation.                                                     |
| `InsertWarnsNotErrorsOnForbiddenAsync`                                      | Fact   | Insert warns not errors on forbidden.                                          |
| `InsertLogsOneAuditRecordOnSuccessAsync`                                    | Fact   | Insert logs one audit record on success.                                       |
| `ListLogsOneAuditRecordAsync`                                               | Fact   | List logs one audit record.                                                    |
| `CatalogueListsUniqueVisualisationRegistryRoleToolNames`                    | Fact   | Catalogue lists unique visualisation registry role tool names.                 |
| `RepeatedGrantReturnsTheExistingGrantAndWritesNoSecondRowAsync`             | Fact   | Repeated grant returns the existing grant and writes no second row.            |
| `RepeatedGrantAfterRevokeCreatesANewLiveGrantAsync`                         | Fact   | Repeated grant after revoke creates a new live grant.                          |
| `RepeatedGrantAndRevokeLeavesNoAccessBehindAsync`                           | Fact   | Repeated grant and revoke leaves no access behind.                             |
| `TheSameRoleOnTwoParentsIsTwoIndependentGrantsAsync`                        | Fact   | The same role on two parents is two independent grants.                        |
| `ConcurrentIdenticalGrantsProduceExactlyOneLiveRowAsync`                    | Fact   | Concurrent identical grants produce exactly one live row.                      |
