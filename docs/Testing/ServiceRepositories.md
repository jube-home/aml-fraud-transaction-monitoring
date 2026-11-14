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

`Jube.Test.Service.Repository.ActivationWatcher.ActivationWatcherServiceTests` — 11 tests. Category: Service.
Collection:
Database.

| Test                                                          | Kind | Verifies                                                         |
|---------------------------------------------------------------|------|------------------------------------------------------------------|
| `ReplayReturnsRowsInTenantAndProjectsFieldsAsync`             | Fact | Replay returns rows in tenant and projects fields.               |
| `ReplayIsScopedToCallersTenantAsync`                          | Fact | Replay is scoped to callers tenant.                              |
| `ReplayFiltersByDateRangeAsync`                               | Fact | Replay filters by date range.                                    |
| `ReplayReturnsOldestFirstAndCarriesThePreviousLocationAsync`  | Fact | Replay returns oldest first and carries the previous location.   |
| `ReplayCanBeNarrowedToOneKeyAndValueAsync`                    | Fact | Replay can be narrowed to one key and value.                     |
| `ReplayNarrowedToAnEntityNeverReachesAnotherTenantsRowsAsync` | Fact | Replay narrowed to an entity never reaches another tenants rows. |
| `ReplayWithoutPermissionThrowsForbiddenAsync`                 | Fact | Replay without permission throws forbidden.                      |
| `CreateWithNullUserNameThrowsNotAuthenticatedAsync`           | Fact | Create with null user name throws not authenticated.             |
| `CreateWithUnknownTenantThrowsNotAuthenticatedAsync`          | Fact | Create with unknown tenant throws not authenticated.             |
| `ReplayDoesNotPublishAChangeEventAsync`                       | Fact | Replay does not publish a change event.                          |
| `ReplayWithNoMatchingRowsReturnsEmptyAsync`                   | Fact | Replay with no matching rows returns empty.                      |

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

`Jube.Test.Service.Repository.Case.CaseServiceTests` — 24 tests. Category: Service. Collection: Database.

| Test                                                                          | Kind | Verifies                                                                             |
|-------------------------------------------------------------------------------|------|--------------------------------------------------------------------------------------|
| `CreateWithNullUserNameThrowsNotAuthenticatedAsync`                           | Fact | Create with null user name throws not authenticated.                                 |
| `CreateWithUnknownTenantThrowsNotAuthenticatedAsync`                          | Fact | Create with unknown tenant throws not authenticated.                                 |
| `GetWithoutPermissionThrowsForbiddenAsync`                                    | Fact | Get without permission throws forbidden.                                             |
| `ListIsScopedToCallersTenantAsync`                                            | Fact | List is scoped to callers tenant.                                                    |
| `InsertPersistsAndDropsIdentityFieldsAsync`                                   | Fact | Insert persists and drops identity fields.                                           |
| `InsertWithInvalidRatingThrowsValidationFailedAsync`                          | Fact | Insert with invalid rating throws validation failed.                                 |
| `CreateFromCaseKeyValueRaisesCaseFromArchiveAsync`                            | Fact | Create from case key value raises case from archive.                                 |
| `CreateFromCaseKeyValueWithExistingOpenCaseThrowsConflictAsync`               | Fact | Create from case key value with existing open case throws conflict.                  |
| `CreateFromCaseKeyValueWithUnknownWorkflowThrowsNotFoundAsync`                | Fact | Create from case key value with unknown workflow throws not found.                   |
| `CreateFromCaseKeyValueWithNoMatchingArchiveThrowsNotFoundAsync`              | Fact | Create from case key value with no matching archive throws not found.                |
| `UpdateWithUnknownIdThrowsNotFoundAsync`                                      | Fact | Update with unknown id throws not found.                                             |
| `UpdateLockingAnUnlockedCaseSetsLockedDateAndUserAsync`                       | Fact | Update locking an unlocked case sets locked date and user.                           |
| `CaseCreationDispatchesTheStatusFromItsSqlAndAuditsItAsync`                   | Fact | Case creation dispatches the status from its sql and audits it.                      |
| `CaseCreationAuditsTheStatusCallDrawnFromThePayloadAsync`                     | Fact | Case creation audits the status call drawn from the payload.                         |
| `AutomaticEscalationDispatchesTheTargetStatusFromItsSqlAndAuditsItAsync`      | Fact | Automatic escalation dispatches the target status from its sql and audits it.        |
| `UpdateUnlockingALockedCaseClearsLockedDateAsync`                             | Fact | Update unlocking a locked case clears locked date.                                   |
| `UpdateWithUnknownWorkflowStatusThrowsInvalidCaseWorkflowStatusAsync`         | Fact | Update with unknown workflow status throws invalid case workflow status.             |
| `UpdateChangingRatingWritesCaseEventAndPublishesChangeAsync`                  | Fact | Update changing rating writes case event and publishes change.                       |
| `UpdateDoesNotPublishChangeOnValidationFailureAsync`                          | Fact | Update does not publish change on validation failure.                                |
| `EveryStatusChangeCloseAndReopenIsRecordedAsAStayInTheStatusAsync`            | Fact | Every status change close and reopen is recorded as a stay in the status.            |
| `AHigherPriorityActivationEscalatesAnOpenCaseAndTheEngineRecordsTheMoveAsync` | Fact | A higher priority activation escalates an open case and the engine records the move. |

## Case file

| `AnEditToTheDiaryAsksForAFreshRankingAndTurningItOffReturnsItsTypeToReviewAsync` | Fact | An edit to the diary asks
for a fresh ranking and turning it off returns its type to review. |
| `UpdateCannotHandTheLockToAnAnalystWhoCouldNotSeeTheCaseAsync` | Fact | Update cannot hand the lock to an analyst who
could not see the case. |
| `TheCloseReasonIsStoredOnCloseKeptWhileClosedAndClearedOnReopenAsync` | Fact | The close reason and note are stored
when the case closes, kept while it stays closed and cleared when it is reopened. |

### CaseFileTypePolicyTests

`Jube.Test.Service.Repository.CaseFile.CaseFileTypePolicyTests` — 4 tests. Category: Unit. Collection:
EvalExpressionRegistry.

| Test                                                                   | Kind   | Verifies                                                                          |
|------------------------------------------------------------------------|--------|-----------------------------------------------------------------------------------|
| `TheSnifferNamesTheContentFromItsBytes`                                | Theory | The sniffer names the content from its bytes.                                     |
| `TheDefaultsRefuseExecutablesScriptsHtmlAndSvgByExtensionAndByContent` | Fact   | The defaults refuse executables scripts html and svg by extension and by content. |
| `TheCodeDefaultsMatchTheDynamicEnvironmentDefaults`                    | Fact   | The code defaults match the dynamic environment defaults.                         |
| `ASettingReplacesTheListAndAStarAcceptsAnything`                       | Fact   | A setting replaces the list and a star accepts anything.                          |

### CaseFileServiceTests

`Jube.Test.Service.Repository.CaseFile.CaseFileServiceTests` — 38 tests. Category: Service. Collection: Database.

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
| `UploadStripsBidiAndInvisibleCharactersFromTheStoredFileNameAsync`       | Theory | Upload strips bidi and invisible characters from the stored file name.            |
| `UploadRefusesAnExtensionOutsideTheAllowListAndStoresNothingAsync`       | Theory | Upload refuses an extension outside the allow list and stores nothing.            |
| `UploadRefusesContentThatIsNotOfAnAcceptedTypeWhateverTheNameSaysAsync`  | Theory | Upload refuses content that is not of an accepted type whatever the name says.    |
| `TheAllowListsAreSettingsAndAnOwnerCanOptInToSvgOrToEverythingAsync`     | Fact   | The allow lists are settings and an owner can opt in to svg or to everything.     |
| `AScannerThatRejectsOrFailsStopsTheUploadAndStoresNothingAsync`          | Fact   | A scanner that rejects or fails stops the upload and stores nothing.              |

### CaseFileNoteServiceTests

`Jube.Test.Service.Repository.CaseFile.CaseFileNoteServiceTests` — 16 tests. Category: Service. Collection: Database.

| Test                                                                                                 | Kind   | Verifies                                                                                                           |
|------------------------------------------------------------------------------------------------------|--------|--------------------------------------------------------------------------------------------------------------------|
| `AFileIsAttachedToTheCallersOwnNoteAsync`                                                            | Fact   | A file is attached to the callers own note.                                                                        |
| `OneNoteCarriesManyFilesAsync`                                                                       | Fact   | One note carries many files.                                                                                       |
| `ANoteIsCreatedForAFileUploadedWithoutOneAsync`                                                      | Fact   | A note is created for a file uploaded without one.                                                                 |
| `AnEmptyUploadCreatesNeitherAFileNorANoteAsync`                                                      | Fact   | An empty upload creates neither a file nor a note.                                                                 |
| `AFileCannotBeAttachedToSomeoneElsesNoteAsync`                                                       | Fact   | A file cannot be attached to someone elses note.                                                                   |
| `AFileCannotBeAttachedToANoteWrittenBySystemOrAskJoobyAsync`                                         | Theory | A file cannot be attached to a note written by system or ask jooby.                                                |
| `AFileCannotBeAttachedToANoteOfAnotherCaseAsync`                                                     | Fact   | A file cannot be attached to a note of another case.                                                               |
| `ANoteInAnotherTenantBehavesLikeAMissingNoteAsync`                                                   | Fact   | A note in another tenant behaves like a missing note.                                                              |
| `AFileIsAttachedToTheCallersOwnResearchAndListedWithItsParentAsync`                                  | Fact   | A file is attached to the callers own research and listed with its parent.                                         |
| `TheResearchListCarriesItsFilesAsync`                                                                | Fact   | The research list carries its files.                                                                               |
| `AFileCannotBeAttachedToSomeoneElsesAskJoobyOrAnotherCasesOrAnotherTenantsResearchAsync`             | Fact   | A file cannot be attached to someone elses ask jooby or another cases or another tenants research.                 |
| `ANoteAndResearchTogetherAreRefusedAsync`                                                            | Fact   | A note and research together are refused.                                                                          |
| `TheDatabaseRefusesAFileWithBothParentsOrNoneAsync`                                                  | Fact   | The database refuses a file with both parents or none.                                                             |
| `AFileOnANoteWhoseActionTheCallerLacksIsNeitherListedNorDownloadableNorRemovableAsync`               | Fact   | A file on a note whose action the caller lacks is neither listed nor downloadable nor removable.                   |
| `AFileCannotBeUploadedToACaseLockedToSomeoneElseAsync`                                               | Fact   | A file cannot be uploaded to a case locked to someone else.                                                        |
| `AFileCanOnlyBeRemovedByItsUploaderTheLockHolderOrASupervisorAndNeverAgainstAnotherHoldersLockAsync` | Fact   | A file can only be removed by its uploader the lock holder or a supervisor and never against another holders lock. |

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

### CaseNoteAgentSourceTests

`Jube.Test.Service.Repository.CaseNote.CaseNoteAgentSourceTests` — 5 tests. Category: Service. Collection: Database.

| Test                                                                  | Kind | Verifies                                                                      |
|-----------------------------------------------------------------------|------|-------------------------------------------------------------------------------|
| `APlainCallWritesAUserNoteAsync`                                      | Fact | A plain call writes a user note.                                              |
| `ACallInsideAnAgentScopeWritesAnAskJoobyNoteAuthoredByTheCallerAsync` | Fact | A call inside an agent scope writes an ask jooby note authored by the caller. |
| `TheScopeNeverLeaksIntoConcurrentCallsAsync`                          | Fact | The scope never leaks into concurrent calls.                                  |
| `TheBoundCaseNoteInsertToolWritesAnAskJoobyNoteAsync`                 | Fact | The bound case note insert tool writes an ask jooby note.                     |
| `AnAgentWrittenNoteIsReadOnlyToItsAuthorAsync`                        | Fact | An agent written note is read only to its author.                             |

### CaseNoteExtensionServiceTests

`Jube.Test.Service.Repository.CaseNote.CaseNoteExtensionServiceTests` — 15 tests. Category: Service. Collection:
Database.

| Test                                                                                 | Kind   | Verifies                                                                                             |
|--------------------------------------------------------------------------------------|--------|------------------------------------------------------------------------------------------------------|
| `ACallerCannotForgeTheSourceKindOrStateOfANoteAsync`                                 | Fact   | A caller cannot forge the source kind or state of a note.                                            |
| `TheAuthorCanMarkTheirOwnNoteAsAFindingAndClearItAsync`                              | Fact   | The author can mark their own note as a finding and clear it.                                        |
| `AnotherAnalystCannotMarkSomeoneElsesNoteAsync`                                      | Fact   | Another analyst cannot mark someone elses note.                                                      |
| `NotesWrittenBySystemOrAskJoobyAreReadOnlyAsync`                                     | Theory | Notes written by system or ask jooby are read only.                                                  |
| `ANoteInAnotherTenantBehavesLikeAMissingNoteAsync`                                   | Fact   | A note in another tenant behaves like a missing note.                                                |
| `MarkingAFindingNeedsThePermissionAsync`                                             | Fact   | Marking a finding needs the permission.                                                              |
| `TheListReturnsNotesWithoutAnActionAndTheirNewFieldsAsync`                           | Fact   | The list returns notes without an action and their new fields.                                       |
| `TheNoteListCarriesEachNotesFilesAsync`                                              | Fact   | The note list carries each notes files.                                                              |
| `TheQueryTheCasePageReadsCarriesFilesForEveryKindAndSourceAsync`                     | Fact   | The query the case page reads carries files for every kind and source.                               |
| `NarrativeVersionsChainAndAnUnchangedFactsHashWritesNothingAsync`                    | Fact   | Narrative versions chain and an unchanged facts hash writes nothing.                                 |
| `ParallelNarrativesNeverShareAVersionAsync`                                          | Fact   | Parallel narratives never share a version.                                                           |
| `ParallelIdenticalNarrativesWriteExactlyOneAsync`                                    | Fact   | Parallel identical narratives write exactly one.                                                     |
| `AnAnalystWithoutTheActionOfANoteDoesNotSeeItInTheRepositoryListAsync`               | Fact   | An analyst without the action of a note does not see it in the repository list.                      |
| `ANoteCannotBeAddedOrMarkedOnACaseLockedToSomeoneElseButCanOnYourOwnOrAFreeOneAsync` | Fact   | A note cannot be added or marked on a case locked to someone else but can on your own or a free one. |
| `AUserWithRolesButNoTenantMappingSeesNoCaseThroughTheCaseRepositoryAsync`            | Fact   | A user with roles but no tenant mapping sees no case through the case repository.                    |

## Case research

### CaseResearchServiceTests

`Jube.Test.Service.Repository.CaseResearch.CaseResearchServiceTests` — 15 tests. Category: Service. Collection:
Database.

| Test                                                                              | Kind   | Verifies                                                                                     |
|-----------------------------------------------------------------------------------|--------|----------------------------------------------------------------------------------------------|
| `CreateWithoutAUserOrAnUnknownTenantIsNotAuthenticatedAsync`                      | Fact   | Create without a user or an unknown tenant is not authenticated.                             |
| `EveryOperationWithoutPermissionIsForbiddenAndStoresNothingAsync`                 | Fact   | Every operation without permission is forbidden and stores nothing.                          |
| `ResearchIsStoredWithItsSourcesTheCasesKeyAndTheCallerAsAUserAsync`               | Fact   | Research is stored with its sources the cases key and the caller as a user.                  |
| `AForgedSourceKeyOrAuthorInTheBodyIsIgnoredAsync`                                 | Fact   | A forged source key or author in the body is ignored.                                        |
| `TextIsSanitisedLikeANoteAsync`                                                   | Fact   | Text is sanitised like a note.                                                               |
| `ATopicAndFindingsAreRequiredAsync`                                               | Theory | A topic and findings are required.                                                           |
| `ATopicOverFiveHundredOrFindingsOverTheLimitAreRefusedAsync`                      | Fact   | A topic over five hundred or findings over the limit are refused.                            |
| `AHostileSourceAddressRefusesTheWholeResearchAndNothingIsStoredAsync`             | Theory | A hostile source address refuses the whole research and nothing is stored.                   |
| `AnOverlongAddressOrTitleOrElevenSourcesAreRefusedAndTenAreAllowedAsync`          | Fact   | An overlong address or title or eleven sources are refused and ten are allowed.              |
| `AForeignTenantsCaseAMissingCaseAndACaseWithoutTheRoleBehaveTheSameAsync`         | Fact   | A foreign tenants case a missing case and a case without the role behave the same.           |
| `AnAgentCapturesResearchFlaggedAskJoobyAndAPlainCallIsAUserAndTheyNeverLeakAsync` | Fact   | An agent captures research flagged ask jooby and a plain call is a user and they never leak. |
| `TheBoundAgentToolCapturesResearchFlaggedAskJoobyAuthoredByTheCallerAsync`        | Fact   | The bound agent tool captures research flagged ask jooby authored by the caller.             |
| `TheListReturnsNewestFirstWithSourcesAndOnlyWebAddressesAsync`                    | Fact   | The list returns newest first with sources and only web addresses.                           |
| `TheListIsScopedToTheCallersTenantAndWorkflowRolesAsync`                          | Fact   | The list is scoped to the callers tenant and workflow roles.                                 |
| `ResearchCannotBeRecordedOnACaseLockedToSomeoneElseAsync`                         | Fact   | Research cannot be recorded on a case locked to someone else.                                |

## Case suppression

### CaseSuppressionServiceTests

`Jube.Test.Service.Repository.CaseSuppression.CaseSuppressionServiceTests` — 40 tests. Category: Service. Collection:
Database.

| Test                                                                             | Kind | Verifies                                                                                  |
|----------------------------------------------------------------------------------|------|-------------------------------------------------------------------------------------------|
| `CreateWithoutAUserOrAnUnknownTenantIsNotAuthenticatedAsync`                     | Fact | Create without a user or an unknown tenant is not authenticated.                          |
| `EveryOperationWithoutItsPermissionIsForbiddenAndStoresNothingAsync`             | Fact | Every operation without its permission is forbidden and stores nothing.                   |
| `ACaseKeyRequestIsStoredPendingWithItsCountsAndNothingIsAppliedAsync`            | Fact | A case key request is stored pending with its counts and nothing is applied.              |
| `ATransactionRequestKeepsItsEvidenceAndTheWiderCountAsync`                       | Fact | A transaction request keeps its evidence and the wider count.                             |
| `EvidenceFromAnotherValueAnotherCaseOrNowhereIsRefusedAndNothingIsStoredAsync`   | Fact | Evidence from another value another case or nowhere is refused and nothing is stored.     |
| `ASuppliedValueThatNoEvidenceCarriesIsRefusedAsync`                              | Fact | A supplied value that no evidence carries is refused.                                     |
| `AKeyThatIsNotFlaggedForOverrideCannotBeSuppressedAsync`                         | Fact | A key that is not flagged for override cannot be suppressed.                              |
| `TheCaseKeyPathAlwaysUsesTheCasesOwnValueAndOnlyForTheCaseKeyAsync`              | Fact | The case key path always uses the cases own value and only for the case key.              |
| `AReasonAnExpiryAndTargetsAreRequiredAndBoundedAsync`                            | Fact | A reason an expiry and targets are required and bounded.                                  |
| `ForcingNeedsPermissionSixtyTwoAtRequestAndAtApprovalAsync`                      | Fact | Forcing needs permission sixty two at request and at approval.                            |
| `ATargetTheRuleDoesNotAllowIsRefusedWithoutStoringAnythingAsync`                 | Fact | A target the rule does not allow is refused without storing anything.                     |
| `AnAgentRequestIsFlaggedAskJoobyAndAPlainOneIsAUserAndTheyNeverLeakAsync`        | Fact | An agent request is flagged ask jooby and a plain one is a user and they never leak.      |
| `TheMakerCannotDecideTheirOwnRequestButCanRetractItAsync`                        | Fact | The maker cannot decide their own request but can retract it.                             |
| `AnApproverApprovesAndTheOverridesAreCreatedForTheMakerWithTheExpiryAsync`       | Fact | An approver approves and the overrides are created for the maker with the expiry.         |
| `TwoApprovalsNeedTwoDifferentApproversAndNobodyApprovesTwiceAsync`               | Fact | Two approvals need two different approvers and nobody approves twice.                     |
| `AFailedCheckAtApprovalRollsBackEverythingIncludingTheApprovalAsync`             | Fact | A failed check at approval rolls back everything including the approval.                  |
| `ARequestWhoseExpiryHasPassedCannotBeApprovedAsync`                              | Fact | A request whose expiry has passed cannot be approved.                                     |
| `ANonFinalApprovalOfARequestWhoseExpiryHasPassedIsRefusedAndRecordsNothingAsync` | Fact | A non final approval of a request whose expiry has passed is refused and records nothing. |
| `ApprovalIsRefusedWhenTheKeyHasSinceStoppedBeingFlaggedForOverrideAsync`         | Fact | Approval is refused when the key has since stopped being flagged for override.            |
| `AReasonAndANoteAreSanitisedAndABlankNoteIsStoredAsNothingAsync`                 | Fact | A reason and a note are sanitised and a blank note is stored as nothing.                  |
| `TheVersionsListEveryEarlierStateAndFollowTheRequestsVisibilityAsync`            | Fact | The versions list every earlier state and follow the requests visibility.                 |
| `AnExpiryBeyondTheMaximumDurationIsRefusedAtRequestResubmitAndApprovalAsync`     | Fact | An expiry beyond the maximum duration is refused at request resubmit and approval.        |
| `TheMaximumDurationFollowsTheEnvironmentSettingAsync`                            | Fact | The maximum duration follows the environment setting.                                     |
| `APendingRequestLapsesAndCannotBeApprovedListedOrAppliedUntilResubmittedAsync`   | Fact | A pending request lapses and cannot be approved listed or applied until resubmitted.      |
| `ApplyReusesAnIdenticalOverrideAndRefusesADifferentKindAsync`                    | Fact | Apply reuses an identical override and refuses a different kind.                          |
| `RejectEndsTheVersionAndTheMakerResubmitsANewOneAsync`                           | Fact | Reject ends the version and the maker resubmits a new one.                                |
| `OnlyTheMakerRetractsAndANonPendingRequestCannotBeDecidedAsync`                  | Fact | Only the maker retracts and a non pending request cannot be decided.                      |
| `ParallelApprovalsApplyExactlyOnceAsync`                                         | Fact | Parallel approvals apply exactly once.                                                    |
| `AUserWhoCannotSeeTheCaseCannotListOpenOrDecideItsRequestsAsync`                 | Fact | A user who cannot see the case cannot list open or decide its requests.                   |
| `TheAwaitingListIsOldestFirstAndHoldsOnlyPendingRequestsAsync`                   | Fact | The awaiting list is oldest first and holds only pending requests.                        |
| `TheStandingFollowsTheLiveOverrideRowsAsync`                                     | Fact | The standing follows the live override rows.                                              |
| `TheDetailCarriesTheCountsNowAndWhatTheCallerCanDoAsync`                         | Fact | The detail carries the counts now and what the caller can do.                             |
| `CandidateValuesCountEachValueAndAddTheMergedKeysAsync`                          | Fact | Candidate values count each value and add the merged keys.                                |
| `CandidateValuesRefuseAKeyThatIsNotEnabledAndAMalformedMergeAsync`               | Fact | Candidate values refuse a key that is not enabled and a malformed merge.                  |
| `CandidateTransactionsListOnlyTheCaseKeysTransactionsThatCarryTheValueAsync`     | Fact | Candidate transactions list only the case keys transactions that carry the value.         |
| `InForceListsOverridesOnTheJournalsValuesWithTheirOriginAsync`                   | Fact | In force lists overrides on the journals values with their origin.                        |
| `TheReasonDraftIsBuiltFromTheFactsAndNeverFromTheCallersTextAsync`               | Fact | The reason draft is built from the facts and never from the callers text.                 |
| `ARequestOnAnotherTenantsCaseOrAMissingCaseBehavesTheSameAsync`                  | Fact | A request on another tenants case or a missing case behaves the same.                     |
| `TheBoundAgentToolsRequestFlaggedAskJoobyAndListWhatWasRequestedAsync`           | Fact | The bound agent tools request flagged ask jooby and list what was requested.              |
| `TheApprovalAndRejectionOperationsAreNotAgentToolsAsync`                         | Fact | The approval and rejection operations are not agent tools.                                |

### CaseSuppressionReasonBuilderTests

`Jube.Test.Service.Repository.CaseSuppression.CaseSuppressionReasonBuilderTests` — 7 tests. Category: Unit.

| Test                                                      | Kind | Verifies                                                             |
|-----------------------------------------------------------|------|----------------------------------------------------------------------|
| `TheReasonNamesWhatIsSuppressedTheKeyTheCaseAndTheExpiry` | Fact | The reason names what is suppressed the key the case and the expiry. |
| `AllForceSaysForceAndAMixSaysBoth`                        | Fact | All force says force and a mix says both.                            |
| `OneTransactionOnOneDayAndOneOtherKeyReadInTheSingular`   | Fact | One transaction on one day and one other key read in the singular.   |
| `ACappedWideCountIsMarkedWithAPlus`                       | Fact | A capped wide count is marked with a plus.                           |
| `AnUnindexedKeySaysTheWiderImpactIsUnknown`               | Fact | An unindexed key says the wider impact is unknown.                   |
| `ManyTargetsAreSummarisedAfterFive`                       | Fact | Many targets are summarised after five.                              |
| `TheReasonIsBuiltFromFactsOnlyAndIsRepeatable`            | Fact | The reason is built from facts only and is repeatable.               |

### CaseSuppressionLapseTests

`Jube.Test.Service.Repository.CaseSuppression.CaseSuppressionLapseTests` — 18 cases. Category: Unit.

| Test                                                                      | Kind   | Verifies                                                                |
|---------------------------------------------------------------------------|--------|-------------------------------------------------------------------------|
| `TheMaximumDurationFallsBackToTheDefaultWhenTheSettingIsMissingOrInvalid` | Theory | The maximum duration setting falls back to 366 when missing or invalid. |
| `ThePendingLapseFallsBackToTheDefaultWhenTheSettingIsMissingOrInvalid`    | Theory | The pending lapse setting falls back to 14 when missing or invalid.     |
| `APendingRequestLapsesOnceTheWindowHasPassedSinceItWasCreated`            | Fact   | A pending request lapses once the window has passed.                    |
| `AResubmissionRestartsTheWindowFromItsOwnDate`                            | Fact   | A resubmission restarts the window.                                     |
| `APendingRequestWhoseExpiryHasPassedIsLapsedWhateverItsAge`               | Fact   | A pending request whose expiry has passed is lapsed.                    |
| `OnlyAPendingRequestCanLapse`                                             | Theory | Only a pending request can lapse.                                       |
| `ALapsedRequestIsShownAsExpiredAndALiveOneKeepsItsStanding`               | Fact   | A lapsed request is shown as expired.                                   |

### CaseSuppressionStandingResolverTests

`Jube.Test.Service.Repository.CaseSuppression.CaseSuppressionStandingResolverTests` — 6 tests. Category: Unit.

| Test                                                              | Kind   | Verifies                                                                       |
|-------------------------------------------------------------------|--------|--------------------------------------------------------------------------------|
| `ARequestThatIsNotApprovedKeepsItsState`                          | Theory | A request that is not approved keeps its state.                                |
| `AnApprovedTargetWhoseOverrideIsLiveIsApproved`                   | Fact   | An approved target whose override is live is approved.                         |
| `AnApprovedTargetWhoseOverrideHasPassedItsExpiryIsExpired`        | Fact   | An approved target whose override has passed its expiry is expired.            |
| `AnOverrideDeletedBeforeItsExpiryOrMissingIsRemoved`              | Fact   | An override deleted before its expiry or missing is removed.                   |
| `ModelAndRuleOverridesWithTheSameIdAreNotConfused`                | Fact   | Model and rule overrides with the same id are not confused.                    |
| `TheRequestIsRemovedIfAnyTargetIsRemovedAndExpiredOnlyWhenAllAre` | Fact   | The request is removed if any target is removed and expired only when all are. |

## Case search filter

### CaseSearchFilterServiceTests

`Jube.Test.Service.Repository.CaseSearchFilter.CaseSearchFilterServiceTests` — 21 tests.
Category: Service. Collection: Database.

| Test                                                                               | Kind | Verifies                                                                                   |
|------------------------------------------------------------------------------------|------|--------------------------------------------------------------------------------------------|
| `CreateWithBlankUserOrUserWithoutTenantThrowsNotAuthenticatedAsync`                | Fact | Create with blank user or user without tenant throws not authenticated.                    |
| `EveryOperationWithoutPermissionThrowsForbiddenAndWritesNothingAsync`              | Fact | Every operation without permission throws forbidden and writes nothing.                    |
| `InsertStoresTheSearchForTheCallerMapsEveryFieldAndPublishesACreatedEventAsync`    | Fact | Insert stores the search for the caller maps every field and publishes a created event.    |
| `InsertIgnoresAGuidSuppliedByTheCallerAsync`                                       | Fact | Insert ignores a guid supplied by the caller.                                              |
| `InsertRefusesBadInputWithAValidationFailurePerFieldAndWritesNothingAsync`         | Fact | Insert refuses bad input with a validation failure per field and writes nothing.           |
| `InsertRefusesAWorkflowTheCallersRoleIsNotGrantedAsync`                            | Fact | Insert refuses a workflow the callers role is not granted.                                 |
| `InsertRefusesAWorkflowAndARoleFromAnotherTenantAsync`                             | Fact | Insert refuses a workflow and a role from another tenant.                                  |
| `NamesAreUniquePerOwnerAndWorkflowIgnoringCaseButNotAcrossOwnersAsync`             | Fact | Names are unique per owner and workflow ignoring case but not across owners.               |
| `ValidateReportsEveryFailureWithoutStoringAnythingAsync`                           | Fact | Validate reports every failure without storing anything.                                   |
| `ListReturnsOwnAndSharedSearchesOrderedByNameAndNeverAnotherUsersPrivateOnesAsync` | Fact | List returns own and shared searches ordered by name and never another users private ones. |
| `ListHidesSearchesOfAWorkflowTheCallersRoleNoLongerHoldsAsync`                     | Fact | List hides searches of a workflow the callers role no longer holds.                        |
| `ListNeverCrossesTenantsAsync`                                                     | Fact | List never crosses tenants.                                                                |
| `GetReturnsOwnAndSharedSearchesAndNotFoundForAnotherUsersPrivateOneAsync`          | Fact | Get returns own and shared searches and not found for another users private one.           |
| `UpdateByTheOwnerChangesTheSearchRaisesTheVersionAndPublishesAnUpdatedEventAsync`  | Fact | Update by the owner changes the search raises the version and publishes an updated event.  |
| `OnlyTheOwnerCanUpdateOrDeleteEvenWhenTheSearchIsSharedWithTheirRoleAsync`         | Fact | Only the owner can update or delete even when the search is shared with their role.        |
| `UpdateWithAStaleVersionIsRefusedAndAZeroVersionSkipsTheCheckAsync`                | Fact | Update with a stale version is refused and a zero version skips the check.                 |
| `UpdateCannotMoveASearchToAnotherWorkflowOrOntoAnExistingNameAsync`                | Fact | Update cannot move a search to another workflow or onto an existing name.                  |
| `DeleteRemovesTheSearchPublishesADeletedEventAndAMissingOneIsNotFoundAsync`        | Fact | Delete removes the search publishes a deleted event and a missing one is not found.        |
| `DeletingTheWorkflowRemovesItsSavedSearchesAsync`                                  | Fact | Deleting the workflow removes its saved searches.                                          |
| `ASavedSearchRoundTripsThroughStorageIntoTheCompilerWithTheCallersRightsAsync`     | Fact | A saved search round trips through storage into the compiler with the callers rights.      |
| `EveryOperationIsRegisteredAsAnAgentToolWithTheCasesPermission`                    | Fact | Every operation is registered as an agent tool with the cases permission.                  |

## Case workflow

### CaseWorkflowServiceTests

`Jube.Test.Service.Repository.CaseWorkflow.CaseWorkflowServiceTests` — 60 tests. Category: Service. Collection:
Database.

| Test                                                                         | Kind   | Verifies                                                                                             |
|------------------------------------------------------------------------------|--------|------------------------------------------------------------------------------------------------------|
| `CreateWithNullOrBlankUserNameThrowsNotAuthenticatedAsync`                   | Theory | Create with null or blank user name throws not authenticated.                                        |
| `CreateWithUserHavingNoTenantThrowsNotAuthenticatedAsync`                    | Fact   | Create with user having no tenant throws not authenticated.                                          |
| `CreateWithUnknownUserThrowsNotAuthenticatedAsync`                           | Fact   | Create with unknown user throws not authenticated.                                                   |
| `ListWithoutPermissionThrowsForbiddenAsync`                                  | Fact   | List without permission throws forbidden.                                                            |
| `GetByIdWithoutPermissionThrowsForbiddenAsync`                               | Fact   | Get by id without permission throws forbidden.                                                       |
| `InsertWithoutPermissionThrowsForbiddenAndWritesNoRowAsync`                  | Fact   | Insert without permission throws forbidden and writes no row.                                        |
| `UpdateWithoutPermissionThrowsForbiddenAsync`                                | Fact   | Update without permission throws forbidden.                                                          |
| `DeleteWithoutPermissionThrowsForbiddenAsync`                                | Fact   | Delete without permission throws forbidden.                                                          |
| `ListByModelActiveOnlyRequiresCaseWorkflowRoleNotJustTenantPermissionAsync`  | Fact   | List by model active only requires case workflow role not just tenant permission.                    |
| `ListByModelActiveOnlyReturnsRowsOnceRoleIsGrantedAsync`                     | Fact   | List by model active only returns rows once role is granted.                                         |
| `LandlordUserDoesNotAutomaticallySeeOtherTenantsRowsAsync`                   | Fact   | Landlord user does not automatically see other tenants rows.                                         |
| `InsertPersistsAndReturnsVersionOneAsync`                                    | Fact   | Insert persists and returns version one.                                                             |
| `GetByIdReturnsInsertedRowAsync`                                             | Fact   | Get by id returns inserted row.                                                                      |
| `GetByEntityAnalysisModelIdReturnsRowsForThatModelOnlyAsync`                 | Fact   | Get by entity analysis model id returns rows for that model only.                                    |
| `UpdateIncrementsVersionPreservesCreatedFieldsAndSetsUpdatedFieldsAsync`     | Fact   | Update increments version preserves created fields and sets updated fields.                          |
| `UpdateWritesAuditRowAsync`                                                  | Fact   | Update writes audit row.                                                                             |
| `DeleteSoftDeletesRowAsync`                                                  | Fact   | Delete soft deletes row.                                                                             |
| `TenantBCannotGetByIdTenantARowAsync`                                        | Fact   | Tenant b cannot get by id tenant a row.                                                              |
| `TenantBUpdateOfTenantARowThrowsNotFoundAndLeavesRowUntouchedAsync`          | Fact   | Tenant b update of tenant a row throws not found and leaves row untouched.                           |
| `TenantBDeleteOfTenantARowThrowsNotFoundAndRowRemainsAsync`                  | Fact   | Tenant b delete of tenant a row throws not found and row remains.                                    |
| `GetAllAsTenantBNeverContainsTenantARowsAsync`                               | Fact   | Get all as tenant b never contains tenant a rows.                                                    |
| `InsertWithBlankNameThrowsValidationFailedAsync`                             | Theory | Insert with blank name throws validation failed.                                                     |
| `InsertWithNameOverMaxLengthThrowsValidationFailedAsync`                     | Fact   | Insert with name over max length throws validation failed.                                           |
| `RankingSettingsLongerThanTheirColumnAreRefusedByTheValidatorAsync`          | Fact   | Ranking settings longer than their column are refused by the validator.                              |
| `InsertWithNameAtExactlyMaxLengthSucceedsAsync`                              | Fact   | Insert with name at exactly max length succeeds.                                                     |
| `InsertWithUnicodeAndSqlLikeNamePersistsSafelyAsync`                         | Fact   | Insert with unicode and sql like name persists safely.                                               |
| `InsertWithDuplicateNameInSameTenantAndModelThrowsValidationFailedAsync`     | Fact   | Insert with duplicate name in same tenant and model throws validation failed.                        |
| `InsertWithSameNameDifferentCasingInSameTenantThrowsValidationFailedAsync`   | Fact   | Insert with same name different casing in same tenant throws validation failed.                      |
| `InsertWithSameNameInDifferentTenantSucceedsAsync`                           | Fact   | Insert with same name in different tenant succeeds.                                                  |
| `InsertWithNonPositiveEntityAnalysisModelIdThrowsValidationFailedAsync`      | Theory | Insert with non positive entity analysis model id throws validation failed.                          |
| `InsertWithNonExistentEntityAnalysisModelIdThrowsValidationFailedAsync`      | Fact   | Insert with non existent entity analysis model id throws validation failed.                          |
| `InsertWithEntityAnalysisModelIdInAnotherTenantThrowsValidationFailedAsync`  | Fact   | Insert with entity analysis model id in another tenant throws validation failed.                     |
| `InsertWithVisualisationEnabledAndEmptyGuidThrowsValidationFailedAsync`      | Fact   | Insert with visualisation enabled and empty guid throws validation failed.                           |
| `InsertTamperingIdentityAndAuditFieldsHasNoEffectAsync`                      | Fact   | Insert tampering identity and audit fields has no effect.                                            |
| `UpdateTamperingIdentityAndAuditFieldsHasNoEffectAsync`                      | Fact   | Update tampering identity and audit fields has no effect.                                            |
| `UpdateOfLockedRowThrowsNotFoundAsync`                                       | Fact   | Update of locked row throws not found.                                                               |
| `UpdateOfSoftDeletedRowThrowsNotFoundAsync`                                  | Fact   | Update of soft deleted row throws not found.                                                         |
| `UpdateWithUnknownIdThrowsNotFoundAsync`                                     | Fact   | Update with unknown id throws not found.                                                             |
| `DeleteOfMissingRowThrowsNotFoundAsync`                                      | Fact   | Delete of missing row throws not found.                                                              |
| `DeleteOfLockedRowThrowsNotFoundAsync`                                       | Fact   | Delete of locked row throws not found.                                                               |
| `GetByIdForMissingRowReturnsNullAsync`                                       | Fact   | Get by id for missing row returns null.                                                              |
| `ActiveAndLockedByteRoundTripAsync`                                          | Fact   | Active and locked byte round trip.                                                                   |
| `GetWithCancelledTokenThrowsAsync`                                           | Fact   | Get with cancelled token throws.                                                                     |
| `InsertPublishesCreatedChangeEventAsync`                                     | Fact   | Insert publishes created change event.                                                               |
| `UpdatePublishesUpdatedChangeEventAsync`                                     | Fact   | Update publishes updated change event.                                                               |
| `DeletePublishesDeletedChangeEventAsync`                                     | Fact   | Delete publishes deleted change event.                                                               |
| `InsertDoesNotPublishChangeEventOnValidationFailureAsync`                    | Fact   | Insert does not publish change event on validation failure.                                          |
| `ListDoesNotPublishAChangeEventAsync`                                        | Fact   | List does not publish a change event.                                                                |
| `InsertWithoutPermissionLogsWarnNotErrorAsync`                               | Fact   | Insert without permission logs warn not error.                                                       |
| `InsertUnexpectedFailureLogsErrorWithExceptionAttachedAsync`                 | Fact   | Insert unexpected failure logs error with exception attached.                                        |
| `InsertSuccessLogsExactlyOneInfoEntryAsync`                                  | Fact   | Insert success logs exactly one info entry.                                                          |
| `ListWithGatesDisabledRecordsNoEntriesAsync`                                 | Fact   | List with gates disabled records no entries.                                                         |
| `ListEmitsExactlyOneAuditRecordAsync`                                        | Fact   | List emits exactly one audit record.                                                                 |
| `InsertEmitsExactlyOneAuditRecordWithEntityIdAsync`                          | Fact   | Insert emits exactly one audit record with entity id.                                                |
| `InsertWithBlankNameUsesFrenchMessageUnderFrenchCultureAsync`                | Fact   | Insert with blank name uses french message under french culture.                                     |
| `ServiceToolCatalogueRegistersCaseWorkflowOperationsWithGloballyUniqueNames` | Fact   | Service tool catalogue registers case workflow operations with globally unique names.                |
| `ListAsyncClampsTakeAndPaginatesAsync`                                       | Fact   | List async clamps take and paginates.                                                                |
| `UpdateCannotMoveAWorkflowToAnotherModelOfTheSameTenantAsync`                | Fact   | Update cannot move a workflow to another model of the same tenant (EntityAnalysisModelId is pinned). |

## Case workflow action

| `RankingSettingsPersistThroughInsertAndUpdateAndTheListsAreTidiedAsync` | Fact | Ranking settings persist through
insert and update and the lists are tidied. |
| `InvalidRankingSettingsAreRefusedAndEveryProblemIsReportedAsync` | Fact | Invalid ranking settings are refused and
every problem is reported. |

### CaseWorkflowActionServiceTests

`Jube.Test.Service.Repository.CaseWorkflowAction.CaseWorkflowActionServiceTests` — 40 tests. Category: Service.
Collection: Database.

| Test                                                                                | Kind   | Verifies                                                                                                                                                   |
|-------------------------------------------------------------------------------------|--------|------------------------------------------------------------------------------------------------------------------------------------------------------------|
| `CreateWithNullOrBlankUserNameThrowsNotAuthenticatedAsync`                          | Theory | Create with null or blank user name throws not authenticated.                                                                                              |
| `CreateWithUserHavingNoTenantThrowsNotAuthenticatedAsync`                           | Fact   | Create with user having no tenant throws not authenticated.                                                                                                |
| `ListWithoutPermissionThrowsForbiddenAsync`                                         | Fact   | List without permission throws forbidden.                                                                                                                  |
| `GetByIdWithoutPermissionThrowsForbiddenAsync`                                      | Fact   | Get by id without permission throws forbidden.                                                                                                             |
| `InsertWithoutPermissionThrowsForbiddenAndWritesNoRowAsync`                         | Fact   | Insert without permission throws forbidden and writes no row.                                                                                              |
| `UpdateWithoutPermissionThrowsForbiddenAsync`                                       | Fact   | Update without permission throws forbidden.                                                                                                                |
| `DeleteWithoutPermissionThrowsForbiddenAsync`                                       | Fact   | Delete without permission throws forbidden.                                                                                                                |
| `InsertPersistsAndDropsIdentityFieldsAsync`                                         | Fact   | Insert persists and drops identity fields.                                                                                                                 |
| `UpdateIncrementsVersionPreservesCreatedFieldsAndSetsUpdatedFieldsAsync`            | Fact   | Update increments version preserves created fields and sets updated fields.                                                                                |
| `UpdateTamperingIdentityAndAuditFieldsHasNoEffectAsync`                             | Fact   | Update tampering identity and audit fields has no effect.                                                                                                  |
| `UpdateWithUnknownIdThrowsNotFoundAsync`                                            | Fact   | Update with unknown id throws not found.                                                                                                                   |
| `UpdateOfLockedRowThrowsNotFoundAsync`                                              | Fact   | Update of locked row throws not found.                                                                                                                     |
| `DeleteRemovesRowAndSubsequentGetReturnsNullAsync`                                  | Fact   | Delete removes row and subsequent get returns null.                                                                                                        |
| `DeleteOfMissingRowThrowsNotFoundAsync`                                             | Fact   | Delete of missing row throws not found.                                                                                                                    |
| `ListIsScopedToCallersTenantAsync`                                                  | Fact   | List is scoped to callers tenant.                                                                                                                          |
| `UpdateForRowInAnotherTenantThrowsValidationFailedOnTheReferentialCheckAsync`       | Fact   | Update for row in another tenant throws validation failed on the referential check.                                                                        |
| `InsertWithBlankNameThrowsValidationFailedAsync`                                    | Fact   | Insert with blank name throws validation failed.                                                                                                           |
| `InsertWithOverLongNameThrowsValidationFailedAsync`                                 | Fact   | Insert with over long name throws validation failed.                                                                                                       |
| `InsertWithDuplicateNameInSameWorkflowThrowsValidationFailedAsync`                  | Fact   | Insert with duplicate name in same workflow throws validation failed.                                                                                      |
| `InsertWithNonExistentCaseWorkflowIdThrowsValidationFailedAsync`                    | Fact   | Insert with non existent case workflow id throws validation failed.                                                                                        |
| `InsertWithEnableHttpEndpointAndNoUrlThrowsValidationFailedAsync`                   | Fact   | Insert with enable http endpoint and no url throws validation failed.                                                                                      |
| `InsertWithEnableHttpEndpointAndInvalidTypeThrowsValidationFailedAsync`             | Fact   | Insert with enable http endpoint and invalid type throws validation failed.                                                                                |
| `InsertWithEnableHttpEndpointAndValidDataSucceedsAsync`                             | Fact   | Insert with enable http endpoint and valid data succeeds.                                                                                                  |
| `InsertWithEnableNotificationAndNoDestinationThrowsValidationFailedAsync`           | Fact   | Insert with enable notification and no destination throws validation failed.                                                                               |
| `InsertWithEnableNotificationAndInvalidTypeThrowsValidationFailedAsync`             | Fact   | Insert with enable notification and invalid type throws validation failed.                                                                                 |
| `InsertWithEnableNotificationAndValidDataSucceedsAsync`                             | Fact   | Insert with enable notification and valid data succeeds.                                                                                                   |
| `InsertIgnoresDescriptionFieldAsync`                                                | Fact   | Insert ignores description field.                                                                                                                          |
| `ListByWorkflowIncludesInactiveRowsAsync`                                           | Fact   | List by workflow includes inactive rows.                                                                                                                   |
| `ListByWorkflowIdActiveOnlyRequiresBothWorkflowAndActionRoleGrantsAsync`            | Fact   | List by workflow id active only requires both workflow and action role grants.                                                                             |
| `ListByWorkflowGuidActiveOnlyReturnsRowsOnceBothRolesGrantedAsync`                  | Fact   | List by workflow guid active only returns rows once both roles granted.                                                                                    |
| `ListHonoursCancellationAsync`                                                      | Fact   | List honours cancellation.                                                                                                                                 |
| `InsertPublishesCreatedChangeEventAsync`                                            | Fact   | Insert publishes created change event.                                                                                                                     |
| `InsertDoesNotPublishChangeEventOnValidationFailureAsync`                           | Fact   | Insert does not publish change event on validation failure.                                                                                                |
| `DeletePublishesDeletedChangeEventAsync`                                            | Fact   | Delete publishes deleted change event.                                                                                                                     |
| `ListDoesNotPublishAChangeEventAsync`                                               | Fact   | List does not publish a change event.                                                                                                                      |
| `InsertWarnsNotErrorsOnForbiddenAsync`                                              | Fact   | Insert warns not errors on forbidden.                                                                                                                      |
| `InsertWritesOneAuditRecordOnSuccessAsync`                                          | Fact   | Insert writes one audit record on success.                                                                                                                 |
| `CatalogueRegistersUniqueCaseWorkflowActionToolNames`                               | Fact   | Catalogue registers unique case workflow action tool names.                                                                                                |
| `UpdateCannotMoveAActionToAnotherWorkflowOfTheSameTenantAsync`                      | Fact   | Update cannot move a action to another workflow of the same tenant (CaseWorkflowId is pinned).                                                             |
| `InsertWithAnHttpEndpointThatIsNotAWellFormedLiteralUrlThrowsValidationFailedAsync` | Theory | An enabled HTTP endpoint that is not an absolute http or https URL with a literal authority (relative, ftp, file, token in the host, userinfo) is refused. |

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

`Jube.Test.Service.Repository.CaseWorkflowDisplay.CaseWorkflowDisplayServiceTests` — 34 tests. Category: Service.
Collection: Database.

| Test                                                                          | Kind   | Verifies                                                                                        |
|-------------------------------------------------------------------------------|--------|-------------------------------------------------------------------------------------------------|
| `CreateWithNullOrBlankUserNameThrowsNotAuthenticatedAsync`                    | Theory | Create with null or blank user name throws not authenticated.                                   |
| `CreateWithUserHavingNoTenantThrowsNotAuthenticatedAsync`                     | Fact   | Create with user having no tenant throws not authenticated.                                     |
| `ListWithoutPermissionThrowsForbiddenAsync`                                   | Fact   | List without permission throws forbidden.                                                       |
| `GetByIdWithoutPermissionThrowsForbiddenAsync`                                | Fact   | Get by id without permission throws forbidden.                                                  |
| `InsertWithoutPermissionThrowsForbiddenAndWritesNoRowAsync`                   | Fact   | Insert without permission throws forbidden and writes no row.                                   |
| `UpdateWithoutPermissionThrowsForbiddenAsync`                                 | Fact   | Update without permission throws forbidden.                                                     |
| `DeleteWithoutPermissionThrowsForbiddenAsync`                                 | Fact   | Delete without permission throws forbidden.                                                     |
| `InsertPersistsAndDropsIdentityFieldsAsync`                                   | Fact   | Insert persists and drops identity fields.                                                      |
| `UpdateIncrementsVersionPreservesCreatedFieldsAndSetsUpdatedFieldsAsync`      | Fact   | Update increments version preserves created fields and sets updated fields.                     |
| `UpdateTamperingIdentityAndAuditFieldsHasNoEffectAsync`                       | Fact   | Update tampering identity and audit fields has no effect.                                       |
| `UpdateWithUnknownIdThrowsNotFoundAsync`                                      | Fact   | Update with unknown id throws not found.                                                        |
| `UpdateOfLockedRowThrowsNotFoundAsync`                                        | Fact   | Update of locked row throws not found.                                                          |
| `DeleteRemovesRowAndSubsequentGetReturnsNullAsync`                            | Fact   | Delete removes row and subsequent get returns null.                                             |
| `DeleteOfMissingRowThrowsNotFoundAsync`                                       | Fact   | Delete of missing row throws not found.                                                         |
| `ListIsScopedToCallersTenantAsync`                                            | Fact   | List is scoped to callers tenant.                                                               |
| `UpdateForRowInAnotherTenantThrowsValidationFailedOnTheReferentialCheckAsync` | Fact   | Update for row in another tenant throws validation failed on the referential check.             |
| `InsertWithBlankNameThrowsValidationFailedAsync`                              | Fact   | Insert with blank name throws validation failed.                                                |
| `InsertWithOverLongNameThrowsValidationFailedAsync`                           | Fact   | Insert with over long name throws validation failed.                                            |
| `InsertWithDuplicateNameInSameWorkflowThrowsValidationFailedAsync`            | Fact   | Insert with duplicate name in same workflow throws validation failed.                           |
| `InsertWithNonExistentCaseWorkflowIdThrowsValidationFailedAsync`              | Fact   | Insert with non existent case workflow id throws validation failed.                             |
| `InsertWithBlankHtmlThrowsValidationFailedAsync`                              | Fact   | Insert with blank html throws validation failed.                                                |
| `InsertIgnoresDescriptionAndHttpNotificationFieldsAsync`                      | Fact   | Insert ignores description and http notification fields.                                        |
| `ListByWorkflowIncludesInactiveRowsAsync`                                     | Fact   | List by workflow includes inactive rows.                                                        |
| `ListByWorkflowIdActiveOnlyRequiresBothWorkflowAndDisplayRoleGrantsAsync`     | Fact   | List by workflow id active only requires both workflow and display role grants.                 |
| `ListByWorkflowGuidActiveOnlyReturnsRowsOnceBothRolesGrantedAsync`            | Fact   | List by workflow guid active only returns rows once both roles granted.                         |
| `ListHonoursCancellationAsync`                                                | Fact   | List honours cancellation.                                                                      |
| `InsertPublishesCreatedChangeEventAsync`                                      | Fact   | Insert publishes created change event.                                                          |
| `InsertDoesNotPublishChangeEventOnValidationFailureAsync`                     | Fact   | Insert does not publish change event on validation failure.                                     |
| `DeletePublishesDeletedChangeEventAsync`                                      | Fact   | Delete publishes deleted change event.                                                          |
| `ListDoesNotPublishAChangeEventAsync`                                         | Fact   | List does not publish a change event.                                                           |
| `InsertWarnsNotErrorsOnForbiddenAsync`                                        | Fact   | Insert warns not errors on forbidden.                                                           |
| `InsertWritesOneAuditRecordOnSuccessAsync`                                    | Fact   | Insert writes one audit record on success.                                                      |
| `CatalogueRegistersUniqueCaseWorkflowDisplayToolNames`                        | Fact   | Catalogue registers unique case workflow display tool names.                                    |
| `UpdateCannotMoveADisplayToAnotherWorkflowOfTheSameTenantAsync`               | Fact   | Update cannot move a display to another workflow of the same tenant (CaseWorkflowId is pinned). |

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

`Jube.Test.Service.Repository.CaseWorkflowFilter.CaseWorkflowFilterServiceTests` — 24 tests. Category: Service.
Collection: Database.

| Test                                                                     | Kind   | Verifies                                                                                       |
|--------------------------------------------------------------------------|--------|------------------------------------------------------------------------------------------------|
| `CreateWithNullOrBlankUserNameThrowsNotAuthenticatedAsync`               | Theory | Create with null or blank user name throws not authenticated.                                  |
| `CreateWithUserHavingNoTenantThrowsNotAuthenticatedAsync`                | Fact   | Create with user having no tenant throws not authenticated.                                    |
| `ListWithoutPermissionThrowsForbiddenAsync`                              | Fact   | List without permission throws forbidden.                                                      |
| `InsertWithoutPermissionThrowsForbiddenAndWritesNoRowAsync`              | Fact   | Insert without permission throws forbidden and writes no row.                                  |
| `InsertPersistsAndReturnsVersionOneWithAuditFieldsAsync`                 | Fact   | Insert persists and returns version one with audit fields.                                     |
| `InsertWithBlankNameThrowsValidationFailedAsync`                         | Fact   | Insert with blank name throws validation failed.                                               |
| `InsertWithUnknownCaseWorkflowIdThrowsValidationFailedAsync`             | Fact   | Insert with unknown case workflow id throws validation failed.                                 |
| `InsertWithBlankSelectJsonThrowsValidationFailedAsync`                   | Fact   | Insert with blank select json throws validation failed.                                        |
| `InsertWithDuplicateNameInSameWorkflowThrowsValidationFailedAsync`       | Fact   | Insert with duplicate name in same workflow throws validation failed.                          |
| `InsertIgnoresDescriptionAndHttpNotificationFieldsAsync`                 | Fact   | Insert ignores description and http notification fields.                                       |
| `UpdatePreservesCreatedFieldsAndSetsUpdatedFieldsAsync`                  | Fact   | Update preserves created fields and sets updated fields.                                       |
| `UpdatePreservesImportIdAsync`                                           | Fact   | Update preserves import id.                                                                    |
| `UpdateOfUnknownIdThrowsNotFoundAsync`                                   | Fact   | Update of unknown id throws not found.                                                         |
| `UpdateOfCrossTenantRowThrowsNotFoundAsync`                              | Fact   | Update of cross tenant row throws not found.                                                   |
| `UpdateWithCaseWorkflowIdFromAnotherTenantThrowsValidationFailedAsync`   | Fact   | Update with case workflow id from another tenant throws validation failed.                     |
| `DeleteSoftDeletesAndPublishesDeletedChangeEventAsync`                   | Fact   | Delete soft deletes and publishes deleted change event.                                        |
| `DeleteOfUnknownIdThrowsNotFoundAsync`                                   | Fact   | Delete of unknown id throws not found.                                                         |
| `ListByWorkflowIdIsScopedToCallersTenantAsync`                           | Fact   | List by workflow id is scoped to callers tenant.                                               |
| `ListByWorkflowIdActiveOnlyRequiresBothWorkflowAndFilterRoleGrantsAsync` | Fact   | List by workflow id active only requires both workflow and filter role grants.                 |
| `GetByGuidReturnsMatchingFilterAsync`                                    | Fact   | Get by guid returns matching filter.                                                           |
| `ListHonoursCancellationAsync`                                           | Fact   | List honours cancellation.                                                                     |
| `ListDoesNotPublishAChangeEventAsync`                                    | Fact   | List does not publish a change event.                                                          |
| `CatalogueRegistersExpectedToolNamesForThisArea`                         | Fact   | Catalogue registers expected tool names for this area.                                         |
| `UpdateCannotMoveAFilterToAnotherWorkflowOfTheSameTenantAsync`           | Fact   | Update cannot move a filter to another workflow of the same tenant (CaseWorkflowId is pinned). |

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

`Jube.Test.Service.Repository.CaseWorkflowForm.CaseWorkflowFormServiceTests` — 30 tests. Category: Service. Collection:
Database.

| Test                                                                                | Kind   | Verifies                                                                                                                                                   |
|-------------------------------------------------------------------------------------|--------|------------------------------------------------------------------------------------------------------------------------------------------------------------|
| `CreateWithNullOrBlankUserNameThrowsNotAuthenticatedAsync`                          | Theory | Create with null or blank user name throws not authenticated.                                                                                              |
| `CreateWithUserHavingNoTenantThrowsNotAuthenticatedAsync`                           | Fact   | Create with user having no tenant throws not authenticated.                                                                                                |
| `ListWithoutPermissionThrowsForbiddenAsync`                                         | Fact   | List without permission throws forbidden.                                                                                                                  |
| `InsertWithoutPermissionThrowsForbiddenAndWritesNoRowAsync`                         | Fact   | Insert without permission throws forbidden and writes no row.                                                                                              |
| `InsertPersistsAndReturnsVersionOneWithAuditFieldsAsync`                            | Fact   | Insert persists and returns version one with audit fields.                                                                                                 |
| `InsertWithBlankNameThrowsValidationFailedAsync`                                    | Fact   | Insert with blank name throws validation failed.                                                                                                           |
| `InsertWithUnknownCaseWorkflowIdThrowsValidationFailedAsync`                        | Fact   | Insert with unknown case workflow id throws validation failed.                                                                                             |
| `InsertWithBlankHtmlThrowsValidationFailedAsync`                                    | Fact   | Insert with blank html throws validation failed.                                                                                                           |
| `InsertWithDuplicateNameInSameWorkflowThrowsValidationFailedAsync`                  | Fact   | Insert with duplicate name in same workflow throws validation failed.                                                                                      |
| `InsertIgnoresDescriptionFieldAsync`                                                | Fact   | Insert ignores description field.                                                                                                                          |
| `InsertWithEnableHttpEndpointAndNoUrlThrowsValidationFailedAsync`                   | Fact   | Insert with enable http endpoint and no url throws validation failed.                                                                                      |
| `InsertWithEnableHttpEndpointAndInvalidTypeThrowsValidationFailedAsync`             | Fact   | Insert with enable http endpoint and invalid type throws validation failed.                                                                                |
| `InsertWithEnableHttpEndpointAndValidDataSucceedsAsync`                             | Fact   | Insert with enable http endpoint and valid data succeeds.                                                                                                  |
| `InsertWithEnableNotificationAndNoDestinationThrowsValidationFailedAsync`           | Fact   | Insert with enable notification and no destination throws validation failed.                                                                               |
| `InsertWithEnableNotificationAndInvalidTypeThrowsValidationFailedAsync`             | Fact   | Insert with enable notification and invalid type throws validation failed.                                                                                 |
| `InsertWithEnableNotificationAndValidDataSucceedsAsync`                             | Fact   | Insert with enable notification and valid data succeeds.                                                                                                   |
| `UpdatePreservesCreatedFieldsAndSetsUpdatedFieldsAsync`                             | Fact   | Update preserves created fields and sets updated fields.                                                                                                   |
| `UpdatePreservesImportIdAsync`                                                      | Fact   | Update preserves import id.                                                                                                                                |
| `UpdateOfUnknownIdThrowsNotFoundAsync`                                              | Fact   | Update of unknown id throws not found.                                                                                                                     |
| `UpdateOfCrossTenantRowThrowsNotFoundAsync`                                         | Fact   | Update of cross tenant row throws not found.                                                                                                               |
| `UpdateWithCaseWorkflowIdFromAnotherTenantThrowsValidationFailedAsync`              | Fact   | Update with case workflow id from another tenant throws validation failed.                                                                                 |
| `DeleteSoftDeletesAndPublishesDeletedChangeEventAsync`                              | Fact   | Delete soft deletes and publishes deleted change event.                                                                                                    |
| `DeleteOfUnknownIdThrowsNotFoundAsync`                                              | Fact   | Delete of unknown id throws not found.                                                                                                                     |
| `ListByWorkflowIdIsScopedToCallersTenantAsync`                                      | Fact   | List by workflow id is scoped to callers tenant.                                                                                                           |
| `ListByWorkflowIdActiveOnlyRequiresBothWorkflowAndFormRoleGrantsAsync`              | Fact   | List by workflow id active only requires both workflow and form role grants.                                                                               |
| `ListHonoursCancellationAsync`                                                      | Fact   | List honours cancellation.                                                                                                                                 |
| `ListDoesNotPublishAChangeEventAsync`                                               | Fact   | List does not publish a change event.                                                                                                                      |
| `CatalogueRegistersExpectedToolNamesForThisArea`                                    | Fact   | Catalogue registers expected tool names for this area.                                                                                                     |
| `UpdateCannotMoveAFormToAnotherWorkflowOfTheSameTenantAsync`                        | Fact   | Update cannot move a form to another workflow of the same tenant (CaseWorkflowId is pinned).                                                               |
| `InsertWithAnHttpEndpointThatIsNotAWellFormedLiteralUrlThrowsValidationFailedAsync` | Theory | An enabled HTTP endpoint that is not an absolute http or https URL with a literal authority (relative, ftp, file, token in the host, userinfo) is refused. |

## Case workflow form entry

### CaseWorkflowFormEntryServiceTests

`Jube.Test.Service.Repository.CaseWorkflowFormEntry.CaseWorkflowFormEntryServiceTests` — 34 tests. Category: Service.
Collection: Database.

| Test                                                               | Kind | Verifies                                                                   |
|--------------------------------------------------------------------|------|----------------------------------------------------------------------------|
| `CreateWithNullUserNameThrowsNotAuthenticatedAsync`                | Fact | Create with null user name throws not authenticated.                       |
| `CreateWithUnknownTenantThrowsNotAuthenticatedAsync`               | Fact | Create with unknown tenant throws not authenticated.                       |
| `InsertWithoutPermissionThrowsForbiddenAsync`                      | Fact | Insert without permission throws forbidden.                                |
| `GetByCaseKeyValueWithoutPermissionThrowsForbiddenAsync`           | Fact | Get by case key value without permission throws forbidden.                 |
| `InsertWithZeroCaseWorkflowFormIdThrowsValidationFailedAsync`      | Fact | Insert with zero case workflow form id throws validation failed.           |
| `InsertWithEmptyPayloadThrowsValidationFailedAsync`                | Fact | Insert with empty payload throws validation failed.                        |
| `InsertWithZeroCaseIdThrowsValidationFailedAsync`                  | Fact | Insert with zero case id throws validation failed.                         |
| `InsertWithUnknownCaseIdThrowsNotFoundAsync`                       | Fact | Insert with unknown case id throws not found.                              |
| `InsertWithUnknownCaseWorkflowFormIdThrowsNotFoundAsync`           | Fact | Insert with unknown case workflow form id throws not found.                |
| `InsertForCaseInAnotherTenantThrowsNotFoundAsync`                  | Fact | Insert for case in another tenant throws not found.                        |
| `InsertPersistsEntryAndValueRowsAsync`                             | Fact | Insert persists entry and value rows.                                      |
| `InsertRejectsAKeyThatIsNotAFieldOfTheFormWithoutNamingItAsync`    | Fact | Insert rejects a key that is not a field of the form without naming it.    |
| `InsertEnforcesTheChoicesOfASelectAndTheTypeOfAFieldAsync`         | Fact | Insert enforces the choices of a select and the type of a field.           |
| `InsertStoresNothingForAFieldTheTemplateHidesAsync`                | Fact | Insert stores nothing for a field the template hides.                      |
| `InsertRefusesAFormTemplateThatDoesNotCompileAsync`                | Fact | Insert refuses a form template that does not compile.                      |
| `InsertRefusesAValueThatFailsAValidatorOfTheFormAsync`             | Fact | Insert refuses a value that fails a validator of the form.                 |
| `InsertAcceptsAValueThatPassesTheValidatorsOfTheFormAsync`         | Fact | Insert accepts a value that passes the validators of the form.             |
| `InsertRunsASqlValidatorAgainstTheRealDatabaseAsync`               | Fact | Insert runs a sql validator against the real database.                     |
| `InsertFailsClosedWhenASqlValidatorCannotRunAsync`                 | Fact | Insert fails closed when a sql validator cannot run.                       |
| `InsertInSqlModeStoresASnapshotOfTheSqlTheTemplateAndTheRowsAsync` | Fact | Insert in sql mode stores a snapshot of the sql the template and the rows. |
| `InsertInSqlModeRefusesASubmissionWhenTheFormSqlFailsAsync`        | Fact | Insert in sql mode refuses a submission when the form sql fails.           |
| `InsertInSqlModeAuditsARefusedNotificationWithItsRowsAsync`        | Fact | Insert in sql mode audits a refused notification with its rows.            |
| `InsertSkipsNullPayloadValuesAsync`                                | Fact | Insert skips null payload values.                                          |
| `InsertPublishesCreatedChangeEventAsync`                           | Fact | Insert publishes created change event.                                     |
| `InsertDoesNotPublishChangeEventOnValidationFailureAsync`          | Fact | Insert does not publish change event on validation failure.                |
| `GetByCaseKeyValueReturnsNewestFirstAsync`                         | Fact | Get by case key value returns newest first.                                |
| `GetByCaseKeyValueIsScopedToCallersTenantAsync`                    | Fact | Get by case key value is scoped to callers tenant.                         |
| `GetByCaseKeyValueWithNoMatchesReturnsEmptyAsync`                  | Fact | Get by case key value with no matches returns empty.                       |
| `GetByCaseKeyValueHonoursCancellationAsync`                        | Fact | Get by case key value honours cancellation.                                |
| `InsertWarnsNotErrorsOnForbiddenAsync`                             | Fact | Insert warns not errors on forbidden.                                      |
| `InsertLogsOneAuditRecordOnSuccessAsync`                           | Fact | Insert logs one audit record on success.                                   |
| `CatalogueRegistersExpectedToolNames`                              | Fact | Catalogue registers expected tool names.                                   |
| `InsertOnACaseLockedToSomeoneElseIsRefusedAndStoresNothingAsync`   | Fact | Insert on a case locked to someone else is refused and stores nothing.     |
| `GetByCaseKeyValueHidesEntriesOfAFormTheCallersRolesLoseAsync`     | Fact | Get by case key value hides entries of a form the callers roles lose.      |

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

`Jube.Test.Service.Repository.CaseWorkflowMacro.CaseWorkflowMacroServiceTests` — 30 tests. Category: Service.
Collection: Database.

| Test                                                                                | Kind   | Verifies                                                                                                                                                   |
|-------------------------------------------------------------------------------------|--------|------------------------------------------------------------------------------------------------------------------------------------------------------------|
| `CreateWithNullOrBlankUserNameThrowsNotAuthenticatedAsync`                          | Theory | Create with null or blank user name throws not authenticated.                                                                                              |
| `CreateWithUserHavingNoTenantThrowsNotAuthenticatedAsync`                           | Fact   | Create with user having no tenant throws not authenticated.                                                                                                |
| `ListWithoutPermissionThrowsForbiddenAsync`                                         | Fact   | List without permission throws forbidden.                                                                                                                  |
| `InsertWithoutPermissionThrowsForbiddenAndWritesNoRowAsync`                         | Fact   | Insert without permission throws forbidden and writes no row.                                                                                              |
| `InsertPersistsAndReturnsVersionOneWithAuditFieldsAsync`                            | Fact   | Insert persists and returns version one with audit fields.                                                                                                 |
| `InsertWithBlankNameThrowsValidationFailedAsync`                                    | Fact   | Insert with blank name throws validation failed.                                                                                                           |
| `InsertWithUnknownCaseWorkflowIdThrowsValidationFailedAsync`                        | Fact   | Insert with unknown case workflow id throws validation failed.                                                                                             |
| `InsertWithBlankJavascriptThrowsValidationFailedAsync`                              | Fact   | Insert with blank javascript throws validation failed.                                                                                                     |
| `InsertWithDuplicateNameInSameWorkflowThrowsValidationFailedAsync`                  | Fact   | Insert with duplicate name in same workflow throws validation failed.                                                                                      |
| `InsertIgnoresDescriptionFieldAsync`                                                | Fact   | Insert ignores description field.                                                                                                                          |
| `InsertWithEnableHttpEndpointAndNoUrlThrowsValidationFailedAsync`                   | Fact   | Insert with enable http endpoint and no url throws validation failed.                                                                                      |
| `InsertWithEnableHttpEndpointAndInvalidTypeThrowsValidationFailedAsync`             | Fact   | Insert with enable http endpoint and invalid type throws validation failed.                                                                                |
| `InsertWithEnableHttpEndpointAndValidDataSucceedsAsync`                             | Fact   | Insert with enable http endpoint and valid data succeeds.                                                                                                  |
| `InsertWithEnableNotificationAndNoDestinationThrowsValidationFailedAsync`           | Fact   | Insert with enable notification and no destination throws validation failed.                                                                               |
| `InsertWithEnableNotificationAndInvalidTypeThrowsValidationFailedAsync`             | Fact   | Insert with enable notification and invalid type throws validation failed.                                                                                 |
| `InsertWithEnableNotificationAndValidDataSucceedsAsync`                             | Fact   | Insert with enable notification and valid data succeeds.                                                                                                   |
| `UpdatePreservesCreatedFieldsAndSetsUpdatedFieldsAsync`                             | Fact   | Update preserves created fields and sets updated fields.                                                                                                   |
| `UpdatePreservesImportIdAsync`                                                      | Fact   | Update preserves import id.                                                                                                                                |
| `UpdateOfUnknownIdThrowsNotFoundAsync`                                              | Fact   | Update of unknown id throws not found.                                                                                                                     |
| `UpdateOfCrossTenantRowThrowsNotFoundAsync`                                         | Fact   | Update of cross tenant row throws not found.                                                                                                               |
| `UpdateWithCaseWorkflowIdFromAnotherTenantThrowsValidationFailedAsync`              | Fact   | Update with case workflow id from another tenant throws validation failed.                                                                                 |
| `DeleteSoftDeletesAndPublishesDeletedChangeEventAsync`                              | Fact   | Delete soft deletes and publishes deleted change event.                                                                                                    |
| `DeleteOfUnknownIdThrowsNotFoundAsync`                                              | Fact   | Delete of unknown id throws not found.                                                                                                                     |
| `ListByWorkflowIdIsScopedToCallersTenantAsync`                                      | Fact   | List by workflow id is scoped to callers tenant.                                                                                                           |
| `ListByWorkflowIdActiveOnlyRequiresBothWorkflowAndFormRoleGrantsAsync`              | Fact   | List by workflow id active only requires both workflow and form role grants.                                                                               |
| `ListHonoursCancellationAsync`                                                      | Fact   | List honours cancellation.                                                                                                                                 |
| `ListDoesNotPublishAChangeEventAsync`                                               | Fact   | List does not publish a change event.                                                                                                                      |
| `CatalogueRegistersExpectedToolNamesForThisArea`                                    | Fact   | Catalogue registers expected tool names for this area.                                                                                                     |
| `UpdateCannotMoveAMacroToAnotherWorkflowOfTheSameTenantAsync`                       | Fact   | Update cannot move a macro to another workflow of the same tenant (CaseWorkflowId is pinned).                                                              |
| `InsertWithAnHttpEndpointThatIsNotAWellFormedLiteralUrlThrowsValidationFailedAsync` | Theory | An enabled HTTP endpoint that is not an absolute http or https URL with a literal authority (relative, ftp, file, token in the host, userinfo) is refused. |

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

`Jube.Test.Service.Repository.CaseWorkflowStatus.CaseWorkflowStatusServiceTests` — 50 tests. Category: Service.
Collection: Database.

| Test                                                                                | Kind   | Verifies                                                                                                                                                   |
|-------------------------------------------------------------------------------------|--------|------------------------------------------------------------------------------------------------------------------------------------------------------------|
| `CreateWithNullOrBlankUserNameThrowsNotAuthenticatedAsync`                          | Theory | Create with null or blank user name throws not authenticated.                                                                                              |
| `CreateWithUserHavingNoTenantThrowsNotAuthenticatedAsync`                           | Fact   | Create with user having no tenant throws not authenticated.                                                                                                |
| `ListWithoutPermissionThrowsForbiddenAsync`                                         | Fact   | List without permission throws forbidden.                                                                                                                  |
| `GetByIdWithoutPermissionThrowsForbiddenAsync`                                      | Fact   | Get by id without permission throws forbidden.                                                                                                             |
| `InsertWithoutPermissionThrowsForbiddenAndWritesNoRowAsync`                         | Fact   | Insert without permission throws forbidden and writes no row.                                                                                              |
| `UpdateWithoutPermissionThrowsForbiddenAsync`                                       | Fact   | Update without permission throws forbidden.                                                                                                                |
| `DeleteWithoutPermissionThrowsForbiddenAsync`                                       | Fact   | Delete without permission throws forbidden.                                                                                                                |
| `InsertPersistsAndDropsIdentityFieldsAsync`                                         | Fact   | Insert persists and drops identity fields.                                                                                                                 |
| `UpdateIncrementsVersionPreservesCreatedFieldsAndSetsUpdatedFieldsAsync`            | Fact   | Update increments version preserves created fields and sets updated fields.                                                                                |
| `UpdateTamperingIdentityAndAuditFieldsHasNoEffectAsync`                             | Fact   | Update tampering identity and audit fields has no effect.                                                                                                  |
| `UpdateWithUnknownIdThrowsNotFoundAsync`                                            | Fact   | Update with unknown id throws not found.                                                                                                                   |
| `UpdateOfLockedRowThrowsNotFoundAsync`                                              | Fact   | Update of locked row throws not found.                                                                                                                     |
| `UpdatePreservesImportIdAsync`                                                      | Fact   | Update preserves import id.                                                                                                                                |
| `DeleteRemovesRowAndSubsequentGetReturnsNullAsync`                                  | Fact   | Delete removes row and subsequent get returns null.                                                                                                        |
| `DeleteOfMissingRowThrowsNotFoundAsync`                                             | Fact   | Delete of missing row throws not found.                                                                                                                    |
| `ListIsScopedToCallersTenantAsync`                                                  | Fact   | List is scoped to callers tenant.                                                                                                                          |
| `UpdateForRowInAnotherTenantThrowsValidationFailedOnTheReferentialCheckAsync`       | Fact   | Update for row in another tenant throws validation failed on the referential check.                                                                        |
| `InsertWithBlankNameThrowsValidationFailedAsync`                                    | Fact   | Insert with blank name throws validation failed.                                                                                                           |
| `InsertWithNullNameThrowsValidationFailedAndDoesNotThrowNullReferenceAsync`         | Fact   | Insert with null name throws validation failed and does not throw null reference.                                                                          |
| `InsertWithOverLongNameThrowsValidationFailedAsync`                                 | Fact   | Insert with over long name throws validation failed.                                                                                                       |
| `InsertWithDuplicateNameInSameWorkflowThrowsValidationFailedAsync`                  | Fact   | Insert with duplicate name in same workflow throws validation failed.                                                                                      |
| `InsertWithNonExistentCaseWorkflowIdThrowsValidationFailedAsync`                    | Fact   | Insert with non existent case workflow id throws validation failed.                                                                                        |
| `InsertWithInvalidPriorityThrowsValidationFailedAsync`                              | Fact   | Insert with invalid priority throws validation failed.                                                                                                     |
| `InsertWithBlankForeColorThrowsValidationFailedAsync`                               | Fact   | Insert with blank fore color throws validation failed.                                                                                                     |
| `InsertWithMalformedBackColorThrowsValidationFailedAsync`                           | Fact   | Insert with malformed back color throws validation failed.                                                                                                 |
| `InsertWithEnableHttpEndpointAndNoUrlThrowsValidationFailedAsync`                   | Fact   | Insert with enable http endpoint and no url throws validation failed.                                                                                      |
| `InsertWithEnableHttpEndpointAndInvalidTypeThrowsValidationFailedAsync`             | Fact   | Insert with enable http endpoint and invalid type throws validation failed.                                                                                |
| `InsertWithEnableHttpEndpointAndValidDataSucceedsAsync`                             | Fact   | Insert with enable http endpoint and valid data succeeds.                                                                                                  |
| `InsertWithEnableNotificationAndNoDestinationThrowsValidationFailedAsync`           | Fact   | Insert with enable notification and no destination throws validation failed.                                                                               |
| `InsertWithEnableNotificationAndInvalidTypeThrowsValidationFailedAsync`             | Fact   | Insert with enable notification and invalid type throws validation failed.                                                                                 |
| `InsertWithEnableNotificationAndValidDataSucceedsAsync`                             | Fact   | Insert with enable notification and valid data succeeds.                                                                                                   |
| `InsertRoundTripsPriorityAndColoursAsync`                                           | Fact   | Insert round trips priority and colours.                                                                                                                   |
| `ListByWorkflowIncludesInactiveRowsAsync`                                           | Fact   | List by workflow includes inactive rows.                                                                                                                   |
| `ListByWorkflowGuidIncludesInactiveRowsAsync`                                       | Fact   | List by workflow guid includes inactive rows.                                                                                                              |
| `ListByWorkflowIdActiveOnlyRequiresBothWorkflowAndStatusRoleGrantsAsync`            | Fact   | List by workflow id active only requires both workflow and status role grants.                                                                             |
| `ListByWorkflowGuidActiveOnlyReturnsRowsOnceBothRolesGrantedAsync`                  | Fact   | List by workflow guid active only returns rows once both roles granted.                                                                                    |
| `ListHonoursCancellationAsync`                                                      | Fact   | List honours cancellation.                                                                                                                                 |
| `InsertPublishesCreatedChangeEventAsync`                                            | Fact   | Insert publishes created change event.                                                                                                                     |
| `InsertDoesNotPublishChangeEventOnValidationFailureAsync`                           | Fact   | Insert does not publish change event on validation failure.                                                                                                |
| `DeletePublishesDeletedChangeEventAsync`                                            | Fact   | Delete publishes deleted change event.                                                                                                                     |
| `ListDoesNotPublishAChangeEventAsync`                                               | Fact   | List does not publish a change event.                                                                                                                      |
| `InsertWarnsNotErrorsOnForbiddenAsync`                                              | Fact   | Insert warns not errors on forbidden.                                                                                                                      |
| `InsertWritesOneAuditRecordOnSuccessAsync`                                          | Fact   | Insert writes one audit record on success.                                                                                                                 |
| `CatalogueRegistersUniqueCaseWorkflowStatusToolNames`                               | Fact   | Catalogue registers unique case workflow status tool names.                                                                                                |
| `UpdateCannotMoveAStatusToAnotherWorkflowOfTheSameTenantAsync`                      | Fact   | Update cannot move a status to another workflow of the same tenant (CaseWorkflowId is pinned).                                                             |
| `InsertWithAnHttpEndpointThatIsNotAWellFormedLiteralUrlThrowsValidationFailedAsync` | Theory | An enabled HTTP endpoint that is not an absolute http or https URL with a literal authority (relative, ftp, file, token in the host, userinfo) is refused. |

## Case workflow status role

| `AnEscalationRulePersistsWithAStrongerTargetAndCanBeClearedAsync` | Fact | An escalation rule persists with a stronger
target and can be cleared. |
| `AnEscalationTriggerWithABadThresholdOrNumberIsRefusedAsync` | Theory | An escalation trigger with a bad threshold or
number is refused. |
| `EscalationNeedsBothATriggerAndATargetAndNeverAnUnusedThresholdProblemAsync` | Fact | Escalation needs both a trigger
and a target and never an unused threshold problem. |
| `TheEscalationTargetMustBeAnotherActiveStatusOfTheSameWorkflowAndAStrongerPriorityAsync` | Fact | The escalation
target must be another active status of the same workflow and a stronger priority. |

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

`Jube.Test.Service.Repository.CaseWorkflowXPath.CaseWorkflowXPathServiceTests` — 45 tests. Category: Service.
Collection: Database.

| Test                                                                         | Kind   | Verifies                                                                                                                               |
|------------------------------------------------------------------------------|--------|----------------------------------------------------------------------------------------------------------------------------------------|
| `CreateWithNullOrBlankUserNameThrowsNotAuthenticatedAsync`                   | Theory | Create with null or blank user name throws not authenticated.                                                                          |
| `CreateWithUserHavingNoTenantThrowsNotAuthenticatedAsync`                    | Fact   | Create with user having no tenant throws not authenticated.                                                                            |
| `ListWithoutPermissionThrowsForbiddenAsync`                                  | Fact   | List without permission throws forbidden.                                                                                              |
| `InsertWithoutPermissionThrowsForbiddenAndWritesNoRowAsync`                  | Fact   | Insert without permission throws forbidden and writes no row.                                                                          |
| `PermissionDeniedLogsWarnNotErrorAsync`                                      | Fact   | Permission denied logs warn not error.                                                                                                 |
| `InsertPersistsAndReturnsVersionOneWithAuditFieldsAsync`                     | Fact   | Insert persists and returns version one with audit fields.                                                                             |
| `InsertWithBlankNameThrowsValidationFailedAsync`                             | Fact   | Insert with blank name throws validation failed.                                                                                       |
| `InsertWithUnknownCaseWorkflowIdThrowsValidationFailedAsync`                 | Fact   | Insert with unknown case workflow id throws validation failed.                                                                         |
| `InsertWithCaseWorkflowUnderSoftDeletedModelThrowsValidationFailedAsync`     | Fact   | Insert with case workflow under soft deleted model throws validation failed.                                                           |
| `InsertWithBlankXPathThrowsValidationFailedAsync`                            | Fact   | Insert with blank x path throws validation failed.                                                                                     |
| `InsertWithDuplicateNameInSameWorkflowThrowsValidationFailedAsync`           | Fact   | Insert with duplicate name in same workflow throws validation failed.                                                                  |
| `InsertWithSameNameInDifferentWorkflowSucceedsAsync`                         | Fact   | Insert with same name in different workflow succeeds.                                                                                  |
| `InsertWithBoldLineMatchedRequiresColorsAsync`                               | Fact   | Insert with bold line matched requires colors.                                                                                         |
| `InsertWithConditionalFormattingRequiresRegexAndColorsAsync`                 | Fact   | Insert with conditional formatting requires regex and colors.                                                                          |
| `InsertIgnoresDisplayNameDeadFieldAsync`                                     | Fact   | Insert ignores display name dead field.                                                                                                |
| `InsertIgnoresTamperedIdentityAndAuditFieldsAsync`                           | Fact   | Insert ignores tampered identity and audit fields.                                                                                     |
| `UpdatePreservesCreatedFieldsAndSetsUpdatedFieldsAsync`                      | Fact   | Update preserves created fields and sets updated fields.                                                                               |
| `UpdateWritesAVersionAuditRowCarryingTheDeletedFieldsAsync`                  | Fact   | Update writes a version audit row carrying the deleted fields.                                                                         |
| `UpdatePreservesImportIdAsync`                                               | Fact   | Update preserves import id.                                                                                                            |
| `UpdateOfUnknownIdThrowsNotFoundAsync`                                       | Fact   | Update of unknown id throws not found.                                                                                                 |
| `UpdateOfLockedRowThrowsNotFoundAsync`                                       | Fact   | Update of locked row throws not found.                                                                                                 |
| `UpdateOfCrossTenantRowThrowsNotFoundAsync`                                  | Fact   | Update of cross tenant row throws not found.                                                                                           |
| `UpdateWithCaseWorkflowIdFromAnotherTenantThrowsValidationFailedAsync`       | Fact   | Update with case workflow id from another tenant throws validation failed.                                                             |
| `UpdateIgnoresTamperedIdentityAndAuditFieldsAsync`                           | Fact   | Update ignores tampered identity and audit fields.                                                                                     |
| `DeleteSoftDeletesAndPublishesDeletedChangeEventAsync`                       | Fact   | Delete soft deletes and publishes deleted change event.                                                                                |
| `DeleteOfUnknownIdThrowsNotFoundAsync`                                       | Fact   | Delete of unknown id throws not found.                                                                                                 |
| `DeleteOfLockedRowThrowsNotFoundAsync`                                       | Fact   | Delete of locked row throws not found.                                                                                                 |
| `ListByWorkflowIdIsScopedToCallersTenantAsync`                               | Fact   | List by workflow id is scoped to callers tenant.                                                                                       |
| `ListByWorkflowIdActiveDrillOnlyRequiresBothWorkflowAndXPathRoleGrantsAsync` | Fact   | List by workflow id active drill only requires both workflow and x path role grants.                                                   |
| `ListByWorkflowIdActiveDrillOnlyExcludesNonDrillRowsAsync`                   | Fact   | List by workflow id active drill only excludes non drill rows.                                                                         |
| `ListByWorkflowGuidActiveDrillOnlyReturnsMatchingAsync`                      | Fact   | List by workflow guid active drill only returns matching.                                                                              |
| `ListHonoursCancellationAsync`                                               | Fact   | List honours cancellation.                                                                                                             |
| `RoundTripPreservesAllExtractionAndColorFieldsAsync`                         | Fact   | Round trip preserves all extraction and color fields.                                                                                  |
| `SuccessfulInsertLogsExactlyOneInfoAndReadsLogNoInfoAsync`                   | Fact   | Successful insert logs exactly one info and reads log no info.                                                                         |
| `WithGatesDisabledHappyPathRecordsNoDebugInfoOrWarnAsync`                    | Fact   | With gates disabled happy path records no debug info or warn.                                                                          |
| `FailureLogsErrorWithExceptionAttachedAndStillPropagatesAsync`               | Fact   | Failure logs error with exception attached and still propagates.                                                                       |
| `EachCallEmitsOneSpanWithOutcomeTagAsync`                                    | Fact   | Each call emits one span with outcome tag.                                                                                             |
| `EachCallRecordsOneDurationMeasurementAsync`                                 | Fact   | Each call records one duration measurement.                                                                                            |
| `AuditLogGetsExactlyOneLineIncludingReadsAsync`                              | Fact   | Audit log gets exactly one line including reads.                                                                                       |
| `ListDoesNotPublishAChangeEventAsync`                                        | Fact   | List does not publish a change event.                                                                                                  |
| `FailedInsertPublishesNothingAsync`                                          | Fact   | Failed insert publishes nothing.                                                                                                       |
| `InsertWithBlankNameUsesFrenchMessageUnderFrenchCultureAsync`                | Fact   | Insert with blank name uses french message under french culture.                                                                       |
| `CatalogueRegistersExpectedToolNamesForThisArea`                             | Fact   | Catalogue registers expected tool names for this area.                                                                                 |
| `ListAsyncClampsTakeAndPaginatesDeterministicallyAsync`                      | Fact   | List async clamps take and paginates deterministically.                                                                                |
| `ConditionalRegularExpressionThatBacktracksOrDoesNotCompileIsRefusedAsync`   | Theory | A conditional formatting regular expression with a back reference, look-around or invalid syntax is refused (non-backtracking engine). |

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

`Jube.Test.Service.Repository.EntityAnalysisModelSynchronisationSchedule.EntityAnalysisModelSynchronisationScheduleServiceTests` —
19 tests. Category: Service. Collection: Database.

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
| `InsertBeyondTheTenantRateLimitIsRefusedAndWritesNothingAsync`           | Fact | Insert beyond the tenant rate limit is refused and writes nothing.           |
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

### ModelSynchronisationScheduleServiceTests

`Jube.Test.Service.Repository.EntityAnalysisModelSynchronisationSchedule.ModelSynchronisationScheduleServiceTests` — 9
tests. Category: Service. Collection: Database.

| Test                                                                | Kind | Verifies                                                                      |
|---------------------------------------------------------------------|------|-------------------------------------------------------------------------------|
| `AScheduleForOneModelIsStoredWithThatModelAsync`                    | Fact | A schedule for one model is stored with that model.                           |
| `AScheduleWithNoModelStaysWholeTenantAsync`                         | Fact | A schedule with no model stays whole tenant.                                  |
| `AModelThatDoesNotExistIsRefusedAsync`                              | Fact | A model that does not exist is refused.                                       |
| `AModelOfAnotherTenantIsRefusedAsync`                               | Fact | A model of another tenant is refused.                                         |
| `ValidateReportsAnUnknownModelWithoutSavingAnythingAsync`           | Fact | Validate reports an unknown model without saving anything.                    |
| `GetCurrentForAModelIsTheNewerOfItsOwnRowAndTheWholeTenantRowAsync` | Fact | Get current for a model is the newer of its own row and the whole tenant row. |
| `GetCurrentWithoutAModelIgnoresRowsForOneModelAsync`                | Fact | Get current without a model ignores rows for one model.                       |
| `GetCurrentForAnotherModelIgnoresARowForThisOneAsync`               | Fact | Get current for another model ignores a row for this one.                     |
| `AScheduleForAModelWithoutPermissionIsForbiddenAsync`               | Fact | A schedule for a model without permission is forbidden.                       |

## Exhaustive search instance promoted trial instance

### ExhaustiveSearchInstancePromotedTrialInstanceServiceTests

`Jube.Test.Service.Repository.ExhaustiveSearchInstancePromotedTrialInstance.ExhaustiveSearchInstancePromotedTrialInstanceServiceTests` —
28 tests. Category: Service. Collection: Database.

| Test                                                                               | Kind   | Verifies                                                                             |
|------------------------------------------------------------------------------------|--------|--------------------------------------------------------------------------------------|
| `UpdateActiveSetsActiveTrueAsync`                                                  | Fact   | Update active sets active true.                                                      |
| `UpdateActiveSetsActiveFalseAsync`                                                 | Fact   | Update active sets active false.                                                     |
| `UpdateActiveIsIdempotentAsync`                                                    | Fact   | Update active is idempotent.                                                         |
| `UpdateOfSoftDeletedRowThrowsNotFoundAndDoesNotReactivateItAsync`                  | Fact   | Update of a soft deleted promoted trial throws not found and does not reactivate it. |
| `UpdateOfMissingIdThrowsNotFoundAsync`                                             | Fact   | Update of missing id throws not found.                                               |
| `UserInTenantBCannotUpdateTenantARowAsync`                                         | Fact   | User in tenant b cannot update tenant a row.                                         |
| `EveryCallThrowsForbiddenWhenPermissionMissingAndWritesNoRowAsync`                 | Fact   | Every call throws forbidden when permission missing and writes no row.               |
| `NullOrBlankUserNameThrowsNotAuthenticatedBeforeAnyQueryAsync`                     | Theory | Null or blank user name throws not authenticated before any query.                   |
| `UserWithNoUserInTenantRowThrowsNotAuthenticatedAsync`                             | Fact   | User with no user in tenant row throws not authenticated.                            |
| `UnknownUserThrowsNotAuthenticatedAsync`                                           | Fact   | Unknown user throws not authenticated.                                               |
| `InvalidIdIsRejectedWithErrorCodeAsync`                                            | Theory | Invalid id is rejected with error code.                                              |
| `NullModelThrowsArgumentNullExceptionAsync`                                        | Fact   | Null model throws argument null exception.                                           |
| `PreCancelledTokenThrowsOperationCanceledAsync`                                    | Fact   | Pre cancelled token throws operation canceled.                                       |
| `PermissionDeniedLogsWarnNotErrorAsync`                                            | Fact   | Permission denied logs warn not error.                                               |
| `NotFoundLogsWarnNotErrorAsync`                                                    | Fact   | Not found logs warn not error.                                                       |
| `SuccessfulUpdateLogsExactlyOneInfoAsync`                                          | Fact   | Successful update logs exactly one info.                                             |
| `WithGatesDisabledHappyPathRecordsNoDebugInfoOrWarnAsync`                          | Fact   | With gates disabled happy path records no debug info or warn.                        |
| `FailureLogsErrorWithExceptionAttachedAndStillPropagatesAsync`                     | Fact   | Failure logs error with exception attached and still propagates.                     |
| `CallEmitsOneSpanWithOutcomeTagAsync`                                              | Fact   | Call emits one span with outcome tag.                                                |
| `CallRecordsOneDurationMeasurementAsync`                                           | Fact   | Call records one duration measurement.                                               |
| `AuditLogGetsExactlyOneLineAsync`                                                  | Fact   | Audit log gets exactly one line.                                                     |
| `SuccessfulUpdatePublishesExactlyOneUpdatedEventAsync`                             | Fact   | Successful update publishes exactly one updated event.                               |
| `FailedUpdatePublishesNothingAsync`                                                | Fact   | Failed update publishes nothing.                                                     |
| `CatalogueRegistersUniquePascalCaseNoUnderscoreName`                               | Fact   | Catalogue registers unique pascal case no underscore name.                           |
| `IdInvalidMessageResolvesFromFrenchResourceAsync`                                  | Fact   | Id invalid message resolves from french resource.                                    |
| `ChangingThePromotedModelOfALiveInstanceIsRefusedAndChangesNothingAsync`           | Fact   | Activating a promoted model of a live instance is refused with a conflict.           |
| `DeactivatingTheActivePromotedModelOfALiveInstanceIsRefusedAndChangesNothingAsync` | Fact   | Deactivating the active promoted model of a live instance is refused.                |
| `RequestingTheStateAPromotedModelAlreadyHasIsANoOpEvenOnALiveInstanceAsync`        | Fact   | Requesting the existing state is a no-op.                                            |

## Permission specification

### PermissionSpecificationServiceTests

`Jube.Test.Service.Repository.PermissionSpecification.PermissionSpecificationServiceTests` — 26 tests. Category:
Service. Collection: Database.

| Test                                                                               | Kind   | Verifies                                                                                |
|------------------------------------------------------------------------------------|--------|-----------------------------------------------------------------------------------------|
| `CreateWithNullOrBlankUserNameThrowsNotAuthenticatedAsync`                         | Theory | Create with null or blank user name throws not authenticated.                           |
| `CreateWithUserHavingNoTenantThrowsNotAuthenticatedAsync`                          | Fact   | Create with user having no tenant throws not authenticated.                             |
| `CreateWithUnknownUserThrowsNotAuthenticatedAsync`                                 | Fact   | Create with unknown user throws not authenticated.                                      |
| `GetAsyncThrowsForbiddenWhenPermissionMissingAsync`                                | Fact   | Get async throws forbidden when permission missing.                                     |
| `ListAsyncThrowsForbiddenWhenPermissionMissingAsync`                               | Fact   | List async throws forbidden when permission missing.                                    |
| `GetAsyncSucceedsForAUserGrantedExactlyPermissionThirtySixAsync`                   | Fact   | Get async succeeds for a user granted exactly permission thirty six.                    |
| `GetAsyncDoesNotListTheRetiredObservabilitySpecificationAsync`                     | Fact   | Get async does not list the retired observability specification.                        |
| `GetAsyncSucceedsForLandlordUserAsync`                                             | Fact   | Get async succeeds for landlord user.                                                   |
| `GetAsyncReturnsTheKnownBaselineCatalogueRowsAsync`                                | Fact   | Get async returns the known baseline catalogue rows.                                    |
| `CatalogueIsNotTenantScopedAndIsIdenticalFromEveryTenantAsync`                     | Fact   | Catalogue is not tenant scoped and is identical from every tenant.                      |
| `ListAsyncClampsTakeAndOrdersByIdAscendingAsync`                                   | Fact   | List async clamps take and orders by id ascending.                                      |
| `ListAsyncClampsTakeToAtLeastOneAsync`                                             | Fact   | List async clamps take to at least one.                                                 |
| `ListAsyncClampsTakeToAtMostTwoHundredAsync`                                       | Fact   | List async clamps take to at most two hundred.                                          |
| `ListAsyncPagesWithAfterIdAsync`                                                   | Fact   | List async pages with after id.                                                         |
| `PreCancelledTokenOnGetThrowsOperationCanceledAsync`                               | Fact   | Pre cancelled token on get throws operation canceled.                                   |
| `PermissionDeniedLogsWarnNotErrorAsync`                                            | Fact   | Permission denied logs warn not error.                                                  |
| `SuccessfulGetLogsNoInfoAsync`                                                     | Fact   | Successful get logs no info.                                                            |
| `WithGatesDisabledHappyPathRecordsNoDebugInfoOrWarnAsync`                          | Fact   | With gates disabled happy path records no debug info or warn.                           |
| `FailureLogsErrorWithExceptionAttachedAndStillPropagatesAsync`                     | Fact   | Failure logs error with exception attached and still propagates.                        |
| `EachCallEmitsOneSpanWithOutcomeTagAsync`                                          | Fact   | Each call emits one span with outcome tag.                                              |
| `EachCallRecordsOneDurationMeasurementAsync`                                       | Fact   | Each call records one duration measurement.                                             |
| `AuditLogGetsExactlyOneLineIncludingReadsAsync`                                    | Fact   | Audit log gets exactly one line including reads.                                        |
| `ReadsAndFailuresPublishNothingAsync`                                              | Fact   | Reads and failures publish nothing.                                                     |
| `CatalogueRegistersUniquePascalCaseNoUnderscoreNames`                              | Fact   | Catalogue registers unique pascal case no underscore names.                             |
| `PermissionDeniedMessageResolvesForFrenchCultureAsync`                             | Fact   | Permission denied message resolves for french culture.                                  |
| `TheTelemetrySpecificationsAreLabelledRetiredBecauseTheRoutesAreLandlordOnlyAsync` | Fact   | The telemetry specifications are labelled retired because the routes are landlord only. |

## Preservation

### PreservationServiceTests

`Jube.Test.Service.Repository.Preservation.PreservationServiceTests` — 55 tests. Category: Service. Collection:

| Test                                                                                        | Kind   | Verifies                                                                                                     |
|---------------------------------------------------------------------------------------------|--------|--------------------------------------------------------------------------------------------------------------|
| `ImportOfRolesCarryingAPermissionTheCallerLacksIsRefusedAndChangesNothingAsync`             | Fact   | Import of roles carrying a permission the caller lacks is refused.                                           |
| `ImportThatWouldReplaceARoleCarryingAPermissionTheCallerLacksIsRefusedAsync`                | Fact   | Import that would replace a higher role is refused.                                                          |
| `CreateWithNullOrBlankUserNameThrowsNotAuthenticatedAsync`                                  | Theory | Create with null or blank user name throws not authenticated.                                                |
| `CreateWithUnmappedOrTenantlessUserThrowsNotAuthenticatedAsync`                             | Fact   | Create with unmapped or tenantless user throws not authenticated.                                            |
| `WithoutPermissionAllOperationsForbiddenAndNothingWrittenAsync`                             | Fact   | Without permission all operations forbidden and nothing written.                                             |
| `ExportContainsOnlyCallersTenantAndTheLivePayloadMatchesAsync`                              | Fact   | Export contains only callers tenant and the live payload matches.                                            |
| `ExportWithDifferentPasswordCannotBeDecryptedWithOthersAsync`                               | Fact   | Export with different password cannot be decrypted with others.                                              |
| `ExportReturnsJempFileNameFromExportGuidAndPersistsAuditRowAsync`                           | Fact   | Export returns jemp file name from export guid and persists audit row.                                       |
| `RoundTripIntoSecondTenantRemapsIdsTenantAndKeepsSourceUntouchedAsync`                      | Fact   | Round trip into second tenant remaps ids tenant and keeps source untouched.                                  |
| `ExhaustiveClassHistogramsAreExportedAndRoundTripIntoASecondTenantAsync`                    | Fact   | Exhaustive class histograms are exported and round trip into a second tenant.                                |
| `ExhaustiveUploadedDatasetsRoundTripWithRowsAndRemappedInstanceReferenceAsync`              | Fact   | Exhaustive uploaded datasets round trip with rows and remapped instance reference.                           |
| `ExhaustiveUploadedJsonDatasetsRoundTripWithRowsAndRemappedInstanceReferenceAsync`          | Fact   | Exhaustive uploaded json datasets round trip with rows and remapped instance reference.                      |
| `ExhaustiveInstanceWhoseDatasetIsAbsentFromTheFileFallsBackToArchiveAsync`                  | Fact   | Exhaustive instance whose dataset is absent from the file falls back to archive.                             |
| `ExhaustiveJsonDatasetExportKeepsOnlyTheRowsTrainingCanReadAsync`                           | Fact   | Exhaustive json dataset export keeps only the rows training can read.                                        |
| `PreservationCoversEveryExhaustiveInstanceTable`                                            | Fact   | Preservation covers every exhaustive instance table.                                                         |
| `ExhaustiveDatasetExportKeepsOnlyTheRowsTrainingCanReadAsync`                               | Fact   | Exhaustive dataset export keeps only the rows training can read.                                             |
| `RolesImportedIntoOwnTenantKeepsRoleAndPermissionAsync`                                     | Fact   | Roles imported into own tenant keeps role and permission.                                                    |
| `RolesImportedFromAnotherTenantIsRefusedWhileTargetHasUsersAndRollsBackAsync`               | Fact   | Roles imported from another tenant is refused while target has users and rolls back.                         |
| `ImportRestoresGuidWhenSameTenantReimportsItsOwnExportAsync`                                | Fact   | Import restores guid when same tenant reimports its own export.                                              |
| `WrongPasswordThrowsInvalidFileLeavesDataAndMarksImportInErrorAsync`                        | Fact   | Wrong password throws invalid file leaves data and marks import in error.                                    |
| `CorruptOrEmptyFileFailsLeavesDataUntouchedAndPublishesNothingAsync`                        | Theory | Corrupt or empty file fails leaves data untouched and publishes nothing.                                     |
| `TamperedFileIsInvalidFileAsync`                                                            | Fact   | Tampered file is invalid file.                                                                               |
| `ExportWithAPasswordShorterThanTheMinimumIsRefusedAndRecordsNothingAsync`                   | Theory | Export with a password shorter than the minimum is refused and records nothing.                              |
| `ImportOfAFileFromAnEarlierReleaseIsAnInvalidFileAsync`                                     | Fact   | Import of a file from an earlier release is an invalid file.                                                 |
| `NoFilesThrowsNoFileExceptionAsync`                                                         | Fact   | No files throws no file exception.                                                                           |
| `FailedImportMidTransactionRollsBackEverythingAsync`                                        | Fact   | Failed import mid transaction rolls back everything.                                                         |
| `CancelledTokenIsHonouredAndLeavesDataUntouchedAsync`                                       | Fact   | Cancelled token is honoured and leaves data untouched.                                                       |
| `ExportAndSummaryWriteExactlyOneAuditRecordEachAndPublishNothingAsync`                      | Fact   | Export and summary write exactly one audit record each and publish nothing.                                  |
| `ForbiddenOperationAuditsForbiddenOutcomeAsync`                                             | Fact   | Forbidden operation audits forbidden outcome.                                                                |
| `RoundTripKeepsTheCategoricalVocabularyAndTheRecallInputsAreIdenticalAsync`                 | Fact   | Round trip keeps the categorical vocabulary and the recall inputs are identical.                             |
| `ToolCatalogueDoesNotExposeWholeTenantExportOrImport`                                       | Fact   | Tool catalogue does not expose whole tenant export or import.                                                |
| `SnapshotStoresTheExportBodyAsQueryableJsonAndReturnsItsMetadataAsync`                      | Fact   | Snapshot stores the export body as queryable json and returns its metadata.                                  |
| `SnapshotBodyIsQueryableInPostgresAsync`                                                    | Fact   | Snapshot body is queryable in postgres.                                                                      |
| `SnapshotListReturnsMetadataOnlyAndIsTenantScopedAsync`                                     | Fact   | Snapshot list returns metadata only and is tenant scoped.                                                    |
| `SnapshotImportRestoresTheConfigurationItHoldsAsync`                                        | Fact   | Snapshot import restores the configuration it holds.                                                         |
| `SnapshotOperationsAreRefusedWithoutThePreservationPermissionAsync`                         | Fact   | Snapshot operations are refused without the preservation permission.                                         |
| `DeletedSnapshotStopsBeingListedAsync`                                                      | Fact   | Deleted snapshot stops being listed.                                                                         |
| `SnapshotImportIsRefusedWithoutItsOwnPermissionEvenWithPreservationAsync`                   | Fact   | Snapshot import is refused without its own permission even with preservation.                                |
| `ProcessSnapshotBypassesPermissionButStillRecordsTheUserAndStaysInTenantAsync`              | Fact   | Process snapshot bypasses permission but still records the user and stays in tenant.                         |
| `ProcessSnapshotRefusesToActForThePreservationPageSourceAsync`                              | Fact   | Process snapshot refuses to act for the preservation page source.                                            |
| `ProcessImportBypassesThePermissionAndRestoresAsync`                                        | Fact   | Process import bypasses the permission and restores.                                                         |
| `SnapshotBodyIsReturnedWholeAndIsTenantScopedAsync`                                         | Fact   | Snapshot body is returned whole and is tenant scoped.                                                        |
| `SnapshotBodyIsRefusedWithoutThePreservationPermissionAsync`                                | Fact   | Snapshot body is refused without the preservation permission.                                                |
| `ImportStampsTheImportersTenantAndDropsPermissionRowsThatPointOutsideThePackageAsync`       | Fact   | A foreign tenant id in the package is replaced and role grants naming guids outside the package are dropped. |
| `TwoConcurrentImportsIntoOneTenantLeaveASingleCopyOfTheConfigurationAsync`                  | Fact   | Two concurrent imports into one tenant serialise and leave one live copy.                                    |
| `ImportPreviewBlocksAPackageCarryingAPermissionTheCallerLacksAsync`                         | Fact   | The import preview carries a Privilege blocker for a package the caller could not import.                    |
| `SameTenantReimportKeepsTheGuidOfAVisualisationAsync`                                       | Fact   | A same-tenant re-import keeps the guid of a visualisation.                                                   |
| `SnapshotRestoreThatWouldReplaceARoleCarryingAPermissionTheCallerLacksIsRefusedAsync`       | Fact   | A snapshot restore, direct or by process, that would replace a higher role is refused.                       |
| `SnapshotCreateRefusesUnknownAndReservedSourcesAndOverlongNamesAsync`                       | Fact   | Snapshot create refuses unknown and reserved sources and names over 200 characters.                          |
| `ImportAndPreviewOfAPackageOverTheSizeLimitAreRefusedBeforeAnythingIsReadAsync`             | Fact   | Import and preview over the package size limit are refused while reading; exactly at the limit is accepted.  |
| `ImportOfAPackageDeclaringMoreDecompressedBytesThanTheLimitIsRefusedAndChangesNothingAsync` | Fact   | A package declaring more decompressed bytes than the limit is refused and changes nothing.                   |
| `SnapshotRetentionKeepsTheNewestAndTheLastBeforeImportAndPurgesTheRestAsync`                | Fact   | Retention keeps the newest snapshots and the last before-import one, purges bodies and audits the prune.     |
| `SnapshotRetentionRemovesSnapshotsOlderThanTheMaximumAgeAsync`                              | Fact   | Retention removes snapshots older than the maximum age.                                                      |
| `SnapshotRetentionDoesNothingWhileAnImportOfTheTenantIsRunningAsync`                        | Fact   | Retention prunes nothing while an import of the tenant is in flight.                                         |
| `DeletingASnapshotPurgesItsBodyAsync`                                                       | Fact   | Deleting a snapshot purges its body.                                                                         |

### RoleRegistryServiceTests

`Jube.Test.Service.Repository.RoleRegistry.RoleRegistryServiceTests` — 37 tests. Category: Service. Collection:
Database.

| Test                                                                        | Kind   | Verifies                                                                                           |
|-----------------------------------------------------------------------------|--------|----------------------------------------------------------------------------------------------------|
| `CreateWithNullOrBlankUserNameThrowsNotAuthenticatedAsync`                  | Theory | Create with null or blank user name throws not authenticated.                                      |
| `CreateWithUserHavingNoTenantThrowsNotAuthenticatedAsync`                   | Fact   | Create with user having no tenant throws not authenticated.                                        |
| `ListWithoutPermissionThrowsForbiddenAsync`                                 | Fact   | List without permission throws forbidden.                                                          |
| `GetByIdWithoutPermissionThrowsForbiddenAsync`                              | Fact   | Get by id without permission throws forbidden.                                                     |
| `InsertWithoutPermissionThrowsForbiddenAndWritesNoRowAsync`                 | Fact   | Insert without permission throws forbidden and writes no row.                                      |
| `UpdateWithoutPermissionThrowsForbiddenAsync`                               | Fact   | Update without permission throws forbidden.                                                        |
| `DeleteWithoutPermissionThrowsForbiddenAsync`                               | Fact   | Delete without permission throws forbidden.                                                        |
| `InsertPersistsAndDropsIdentityFieldsAsync`                                 | Fact   | Insert persists and drops identity fields.                                                         |
| `UpdateIncrementsVersionPreservesCreatedFieldsAndSetsUpdatedFieldsAsync`    | Fact   | Update increments version preserves created fields and sets updated fields.                        |
| `UpdateTamperingIdentityAndAuditFieldsHasNoEffectAsync`                     | Fact   | Update tampering identity and audit fields has no effect.                                          |
| `UpdateWithAStaleVersionIsRefusedAndAZeroVersionSkipsTheCheckAsync`         | Fact   | Update with a stale version is refused with a version conflict and a zero version skips the check. |
| `UpdateWithUnknownIdThrowsNotFoundAsync`                                    | Fact   | Update with unknown id throws not found.                                                           |
| `UpdateOfLockedRowThrowsNotFoundAsync`                                      | Fact   | Update of locked row throws not found.                                                             |
| `UpdatePreservesImportIdAsync`                                              | Fact   | Update preserves import id.                                                                        |
| `DeleteRemovesRowAndSubsequentGetReturnsNullAsync`                          | Fact   | Delete removes row and subsequent get returns null.                                                |
| `DeleteSetsDeletedUserAsync`                                                | Fact   | Delete sets deleted user.                                                                          |
| `DeleteOfMissingRowThrowsNotFoundAsync`                                     | Fact   | Delete of missing row throws not found.                                                            |
| `ListIsScopedToCallersTenantAsync`                                          | Fact   | List is scoped to callers tenant.                                                                  |
| `UpdateForRowInAnotherTenantThrowsNotFoundAsync`                            | Fact   | Update for row in another tenant throws not found.                                                 |
| `InsertWithBlankNameThrowsValidationFailedAsync`                            | Fact   | Insert with blank name throws validation failed.                                                   |
| `InsertWithNullNameThrowsValidationFailedAndDoesNotThrowNullReferenceAsync` | Fact   | Insert with null name throws validation failed and does not throw null reference.                  |
| `InsertWithOverLongNameThrowsValidationFailedAsync`                         | Fact   | Insert with over long name throws validation failed.                                               |
| `InsertWithDuplicateNameInSameTenantThrowsValidationFailedAsync`            | Fact   | Insert with duplicate name in same tenant throws validation failed.                                |
| `InsertWithDuplicateNameAcrossTenantsSucceedsAsync`                         | Fact   | Insert with duplicate name across tenants succeeds.                                                |
| `InsertRoundTripsActiveAndLockedAsync`                                      | Fact   | Insert round trips active and locked.                                                              |
| `InsertDropsUnpersistedDescriptionFieldAsync`                               | Fact   | Insert drops unpersisted description field.                                                        |
| `ListHonoursCancellationAsync`                                              | Fact   | List honours cancellation.                                                                         |
| `ListAsyncClampsTakeAndPaginatesAsync`                                      | Fact   | List async clamps take and paginates.                                                              |
| `InsertPublishesCreatedChangeEventAsync`                                    | Fact   | Insert publishes created change event.                                                             |
| `InsertDoesNotPublishChangeEventOnValidationFailureAsync`                   | Fact   | Insert does not publish change event on validation failure.                                        |
| `UpdatePublishesUpdatedChangeEventAsync`                                    | Fact   | Update publishes updated change event.                                                             |
| `DeletePublishesDeletedChangeEventAsync`                                    | Fact   | Delete publishes deleted change event.                                                             |
| `ListDoesNotPublishAChangeEventAsync`                                       | Fact   | List does not publish a change event.                                                              |
| `InsertWarnsNotErrorsOnForbiddenAsync`                                      | Fact   | Insert warns not errors on forbidden.                                                              |
| `InsertWritesOneAuditRecordOnSuccessAsync`                                  | Fact   | Insert writes one audit record on success.                                                         |
| `ListWritesOneAuditRecordOnSuccessAsync`                                    | Fact   | List writes one audit record on success.                                                           |
| `CatalogueRegistersUniqueRoleRegistryToolNames`                             | Fact   | Catalogue registers unique role registry tool names.                                               |

## Role registry permission

### RoleRegistryPermissionServiceTests

`Jube.Test.Service.Repository.RoleRegistryPermission.RoleRegistryPermissionServiceTests` — 35 tests. Category: Service.
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
| `GrantsOfALockedRoleCannotBeAddedChangedOrRemovedAsync`                       | Fact   | Grants of a locked role cannot be added changed or removed.                         |
| `UpdateWithAStaleVersionThrowsVersionConflictAndChangesNothingAsync`          | Fact   | Update with a stale version throws version conflict and changes nothing.            |

## Sanction entry source

### SanctionEntrySourceServiceTests

`Jube.Test.Service.Repository.SanctionEntrySource.SanctionEntrySourceServiceTests` — 17 tests. Category: Service.
Collection: Database.

| Test                                                                           | Kind | Verifies                                                                       |
|--------------------------------------------------------------------------------|------|--------------------------------------------------------------------------------|
| `ImportOfAFileOverTheSizeCapThrowsDtoValidationExceptionAndWritesNothingAsync` | Fact | A file over 64 MiB is refused with FilesTooLarge and no import row is written. |
| `CreateWithNullUserNameThrowsNotAuthenticatedAsync`                            | Fact | Create with null user name throws not authenticated.                           |
| `CreateWithUnknownTenantThrowsNotAuthenticatedAsync`                           | Fact | Create with unknown tenant throws not authenticated.                           |
| `GetWithoutLandlordThrowsForbiddenAsync`                                       | Fact | Get without landlord throws forbidden.                                         |
| `GetAsLandlordIncludesTheConfiguredSourceAsync`                                | Fact | Get as landlord includes the configured source.                                |
| `ListWithoutLandlordThrowsForbiddenAsync`                                      | Fact | List without landlord throws forbidden.                                        |
| `ListClampsTakeToTwoHundredAsync`                                              | Fact | List clamps take to two hundred.                                               |
| `ListWithAfterIdExcludesThatRowAndIncludesLaterOnesAsync`                      | Fact | List with after id excludes that row and includes later ones.                  |
| `ImportWithoutLandlordThrowsForbiddenAsync`                                    | Fact | Import without landlord throws forbidden.                                      |
| `ImportWithNoFileThrowsDtoValidationExceptionAsync`                            | Fact | Import with no file throws dto validation exception.                           |
| `ImportWithEmptyFileThrowsDtoValidationExceptionAsync`                         | Fact | Import with empty file throws dto validation exception.                        |
| `ImportWithUnknownSanctionEntrySourceIdThrowsDtoValidationExceptionAsync`      | Fact | Import with unknown sanction entry source id throws dto validation exception.  |
| `ImportWithNoDelimiterConfiguredSkipsSilentlyAsync`                            | Fact | Import with no delimiter configured skips silently.                            |
| `ImportInsertsEntryAndRecordsSuccessfulImportRowAsync`                         | Fact | Import inserts entry and records successful import row.                        |
| `ImportDoesNotPublishChangeEventOnValidationFailureAsync`                      | Fact | Import does not publish change event on validation failure.                    |
| `PermissionDeniedIsLoggedAsWarnNotErrorAsync`                                  | Fact | Permission denied is logged as warn not error.                                 |
| `GetWritesExactlyOneAuditRecordAsync`                                          | Fact | Get writes exactly one audit record.                                           |

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
| `InsertWithEmptyFilterRulesCompilesToASearchThatMatchesEveryCaseTheUserMaySeeAsync`      | Fact   | Insert with empty filter rules compiles to a search that matches every case the user may see.    |
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

`Jube.Test.Service.Repository.TenantRegistry.TenantRegistryServiceTests` — 62 tests. Category: Service. Collection:
Database.

| Test                                                                           | Kind   | Verifies                                                                              |
|--------------------------------------------------------------------------------|--------|---------------------------------------------------------------------------------------|
| `CreateWithNullOrBlankUserNameThrowsNotAuthenticatedAsync`                     | Theory | Create with null or blank user name throws not authenticated.                         |
| `CreateWithUserHavingNoTenantThrowsNotAuthenticatedAsync`                      | Fact   | Create with user having no tenant throws not authenticated.                           |
| `CreateWithUnknownUserThrowsNotAuthenticatedAsync`                             | Fact   | Create with unknown user throws not authenticated.                                    |
| `ListWithoutLandlordThrowsForbiddenAsync`                                      | Fact   | List without landlord throws forbidden.                                               |
| `GetByIdWithoutLandlordThrowsForbiddenAsync`                                   | Fact   | Get by id without landlord throws forbidden.                                          |
| `GetByFilterWithoutLandlordThrowsForbiddenAsync`                               | Fact   | Get by filter without landlord throws forbidden.                                      |
| `ListPagedWithoutLandlordThrowsForbiddenAsync`                                 | Fact   | List paged without landlord throws forbidden.                                         |
| `InsertWithoutLandlordThrowsForbiddenAndWritesNoRowAsync`                      | Fact   | Insert without landlord throws forbidden and writes no row.                           |
| `UpdateWithoutLandlordThrowsForbiddenAsync`                                    | Fact   | Update without landlord throws forbidden.                                             |
| `DeleteWithoutLandlordThrowsForbiddenAsync`                                    | Fact   | Delete without landlord throws forbidden.                                             |
| `InsertAsLandlordPersistsAndDropsIdentityFieldsAsync`                          | Fact   | Insert as landlord persists and drops identity fields.                                |
| `UpdateIncrementsVersionPreservesCreatedFieldsAndSetsUpdatedFieldsAsync`       | Fact   | Update increments version preserves created fields and sets updated fields.           |
| `UpdateTamperingIdentityAndAuditFieldsHasNoEffectAsync`                        | Fact   | Update tampering identity and audit fields has no effect.                             |
| `UpdateWithUnknownIdThrowsNotFoundAsync`                                       | Fact   | Update with unknown id throws not found.                                              |
| `UpdateOfLockedRowThrowsNotFoundAsync`                                         | Fact   | Update of locked row throws not found.                                                |
| `DeleteRemovesRowAndSubsequentGetReturnsNullAsync`                             | Fact   | Delete removes row and subsequent get returns null.                                   |
| `DeleteOfMissingRowThrowsNotFoundAsync`                                        | Fact   | Delete of missing row throws not found.                                               |
| `DeleteOfLockedRowThrowsNotFoundAsync`                                         | Fact   | Delete of locked row throws not found.                                                |
| `ListAsLandlordSeesTenantsCreatedByAnyoneAsync`                                | Fact   | List as landlord sees tenants created by anyone.                                      |
| `GetByFilterMatchesCaseInsensitiveSubstringAsync`                              | Fact   | Get by filter matches case insensitive substring.                                     |
| `GetByFilterWithBlankFilterReturnsAllAsync`                                    | Fact   | Get by filter with blank filter returns all.                                          |
| `GetByFilterWithNullFilterDoesNotThrowAsync`                                   | Fact   | Get by filter with null filter does not throw.                                        |
| `ListAsyncClampsTakeAndPaginatesAsync`                                         | Fact   | List async clamps take and paginates.                                                 |
| `InsertWithBlankNameThrowsValidationFailedAsync`                               | Fact   | Insert with blank name throws validation failed.                                      |
| `InsertWithNullNameThrowsValidationFailedAndDoesNotThrowNullReferenceAsync`    | Fact   | Insert with null name throws validation failed and does not throw null reference.     |
| `InsertWithWhitespaceNameThrowsValidationFailedAsync`                          | Fact   | Insert with whitespace name throws validation failed.                                 |
| `InsertWithOverLongNameThrowsValidationFailedAsync`                            | Fact   | Insert with over long name throws validation failed.                                  |
| `InsertWithNameAtMaximumLengthSucceedsAsync`                                   | Fact   | Insert with name at maximum length succeeds.                                          |
| `InsertWithDuplicateNameThrowsValidationFailedAsync`                           | Fact   | Insert with duplicate name throws validation failed.                                  |
| `InsertWithDuplicateNameDifferentCasingThrowsValidationFailedAsync`            | Fact   | Insert with duplicate name different casing throws validation failed.                 |
| `UpdateWithOwnUnchangedNameDoesNotFailDuplicateCheckAsync`                     | Fact   | Update with own unchanged name does not fail duplicate check.                         |
| `InsertRoundTripsActiveAndLockedAsync`                                         | Fact   | Insert round trips active and locked.                                                 |
| `DescriptionIsNeverPersistedAsync`                                             | Fact   | Description is never persisted.                                                       |
| `GetByIdForMissingIdReturnsNullAsync`                                          | Fact   | Get by id for missing id returns null.                                                |
| `ListHonoursCancellationAsync`                                                 | Fact   | List honours cancellation.                                                            |
| `InsertPublishesCreatedChangeEventAsync`                                       | Fact   | Insert publishes created change event.                                                |
| `InsertDoesNotPublishChangeEventOnValidationFailureAsync`                      | Fact   | Insert does not publish change event on validation failure.                           |
| `DeletePublishesDeletedChangeEventAsync`                                       | Fact   | Delete publishes deleted change event.                                                |
| `ListDoesNotPublishAChangeEventAsync`                                          | Fact   | List does not publish a change event.                                                 |
| `ForbiddenOperationDoesNotPublishAChangeEventAsync`                            | Fact   | Forbidden operation does not publish a change event.                                  |
| `InsertWarnsNotErrorsOnForbiddenAsync`                                         | Fact   | Insert warns not errors on forbidden.                                                 |
| `CreateWithNullUserNameWarnsNotErrorsAsync`                                    | Fact   | Create with null user name warns not errors.                                          |
| `InsertWritesOneAuditRecordOnSuccessAsync`                                     | Fact   | Insert writes one audit record on success.                                            |
| `ListWritesOneAuditRecordEvenThoughItIsAReadAsync`                             | Fact   | List writes one audit record even though it is a read.                                |
| `GetForcedFailureLogsExactlyOneErrorWithExceptionAttachedAsync`                | Fact   | Get forced failure logs exactly one error with exception attached.                    |
| `PermissionDeniedMessageIsLocalisedToFrenchAsync`                              | Fact   | Permission denied message is localised to french.                                     |
| `UpdateWithStaleVersionThrowsVersionConflictAndLeavesRowUnchangedAsync`        | Fact   | Update with stale version throws version conflict and leaves row unchanged.           |
| `DeleteOfALandlordTenantThrowsNotFoundAndTheRowSurvivesAsync`                  | Fact   | Delete of a landlord tenant throws not found and the row survives.                    |
| `UpdateDeactivatingALandlordTenantFailsValidationAndKeepsItActiveAsync`        | Fact   | Update deactivating a landlord tenant fails validation and keeps it active.           |
| `UpdateRenamingAnActiveLandlordTenantStillSucceedsAndKeepsTheFlagAsync`        | Fact   | Update renaming an active landlord tenant still succeeds and keeps the flag.          |
| `InsertWithControlBidirectionalOrPaddedNameFailsValidationAndWritesNoRowAsync` | Theory | Insert with control, bidirectional or padded name fails validation and writes no row. |
| `InsertWithOrdinaryInternationalNamesSucceedsAsync`                            | Theory | Insert with ordinary international names succeeds.                                    |
| `CatalogueRegistersUniqueTenantRegistryToolNames`                              | Fact   | Catalogue registers unique tenant registry tool names.                                |

## User in tenant

| `ALandlordSwitchedIntoATenantThatIsThenDisabledIsReturnedToTheirOwnTenantAsync` | Theory | Alandlord switched into
atenant that is then disabled is returned to their own tenant. |
| `InsertWithAnUnsafeHttpAdaptationUrlThrowsValidationFailedAndWritesNoRowAsync` | Theory | Insert with an unsafe http
adaptation url throws validation failed and writes no row. |
| `InsertWithAnHttpAdaptationTimeoutOutsideTheRangeThrowsValidationFailedAsync` | Theory | Insert with an http
adaptation timeout outside the range throws validation failed. |
| `HttpAdaptationSettingsRoundTripAndAreSnapshottedToTheVersionTableOnUpdateAsync` | Fact | Http adaptation settings
round trip and are snapshotted to the version table on update. |
| `ANewTenantInheritsTheGlobalHttpAdaptationSettingsAndValidatesSslAsync` | Fact | Anew tenant inherits the global http
adaptation settings and validates ssl. |
| `ANewTenantAllowsRemoteSvirfdLookupsByDefaultAsync` | Fact | A new tenant stores and reads the Svirfd remote lookups
switch as on. |
| `ARowWithNoStoredSvirfdSwitchReadsAsEnabledAsync` | Fact | A row whose switch is null (older data) reads as enabled. |
| `SvirfdRemoteLookupsSwitchRoundTripsAndIsSnapshottedToTheVersionTableAsync` | Fact | The switch round trips and the
previous value is copied to the version table on update. |
| `OnlyALandlordCanChangeTheSvirfdRemoteLookupsSwitchAsync` | Fact | A tenant administrator who is not a landlord is
refused and the stored switch is unchanged. |

### UserInTenantServiceTests

`Jube.Test.Service.Repository.UserInTenant.UserInTenantServiceTests` — 33 tests. Category: Service. Collection:
Database.

| Test                                                                   | Kind   | Verifies                                                              |
|------------------------------------------------------------------------|--------|-----------------------------------------------------------------------|
| `CreateWithNullOrBlankUserNameThrowsNotAuthenticatedAsync`             | Theory | Create with null or blank user name throws not authenticated.         |
| `CreateWithUserHavingNoTenantThrowsNotAuthenticatedAsync`              | Fact   | Create with user having no tenant throws not authenticated.           |
| `CreateWithUnknownUserThrowsNotAuthenticatedAsync`                     | Fact   | Create with unknown user throws not authenticated.                    |
| `GetWithoutPermissionThrowsForbiddenAsync`                             | Fact   | Get without permission throws forbidden.                              |
| `GetCurrentTenantRegistryWithoutPermissionThrowsForbiddenAsync`        | Fact   | Get current tenant registry without permission throws forbidden.      |
| `UpdateWithoutLandlordThrowsForbiddenAndWritesNoRowAsync`              | Fact   | Update without landlord throws forbidden and writes no row.           |
| `GetReturnsRowsForCallersTenantAsync`                                  | Fact   | Get returns rows for callers tenant.                                  |
| `GetIsScopedToCallersTenantAsync`                                      | Fact   | Get is scoped to callers tenant.                                      |
| `GetForLandlordDoesNotReturnEveryTenantsRowsAsync`                     | Fact   | Get for landlord does not return every tenants rows.                  |
| `GetCurrentTenantRegistryReturnsCallersOwnAllocationAsync`             | Fact   | Get current tenant registry returns callers own allocation.           |
| `MapperToDtoOfNullReturnsNull`                                         | Fact   | Mapper to dto of null returns null.                                   |
| `ListAsyncClampsTakeAsync`                                             | Fact   | List async clamps take.                                               |
| `ListAsyncPaginatesByAfterIdAsync`                                     | Fact   | List async paginates by after id.                                     |
| `UpdateWithNonPositiveTenantRegistryIdThrowsValidationFailedAsync`     | Fact   | Update with non positive tenant registry id throws validation failed. |
| `UpdateWithUnknownTenantRegistryIdThrowsValidationFailedAsync`         | Fact   | Update with unknown tenant registry id throws validation failed.      |
| `UpdateToADisabledTenantIsRefusedAndTheLandlordStaysWhereTheyAreAsync` | Theory | Switching into an inactive or deleted tenant is refused as not found. |
| `UpdateAsLandlordSwitchesTenantAndPersistsAsync`                       | Fact   | Update as landlord switches tenant and persists.                      |
| `UpdateToSameTenantSucceedsIdempotentlyAsync`                          | Fact   | Update to same tenant succeeds idempotently.                          |
| `UpdatePublishesUpdatedChangeEventAsync`                               | Fact   | Update publishes updated change event.                                |
| `UpdateDoesNotPublishChangeEventOnValidationFailureAsync`              | Fact   | Update does not publish change event on validation failure.           |
| `UpdateDoesNotPublishChangeEventOnForbiddenAsync`                      | Fact   | Update does not publish change event on forbidden.                    |
| `RepositoryUpdateAsyncSetsUserWhenInsertingNewRowAsync`                | Fact   | Repository update async sets user when inserting new row.             |
| `GetHonoursCancellationAsync`                                          | Fact   | Get honours cancellation.                                             |
| `GetWarnsNotErrorsOnForbiddenAsync`                                    | Fact   | Get warns not errors on forbidden.                                    |
| `UpdateLogsOneInfoEntryOnSuccessAsync`                                 | Fact   | Update logs one info entry on success.                                |
| `GetLogsOneAuditRecordAsync`                                           | Fact   | Get logs one audit record.                                            |
| `UpdateLogsOneAuditRecordOnSuccessAsync`                               | Fact   | Update logs one audit record on success.                              |
| `GetWithDisabledGatesRecordsNoEntriesAsync`                            | Fact   | Get with disabled gates records no entries.                           |
| `GetOnDisposedContextRecordsErrorWithExceptionAsync`                   | Fact   | Get on disposed context records error with exception.                 |
| `ValidationMessageMatchesNeutralResourceAsync`                         | Fact   | Validation message matches neutral resource.                          |
| `ValidationMessageResolvesFrenchCultureAsync`                          | Fact   | Validation message resolves french culture.                           |
| `CatalogueListsUniqueUserInTenantToolNames`                            | Fact   | Catalogue lists unique user in tenant tool names.                     |
| `UpdateWithTamperedUserOnDtoHasNoEffectAsync`                          | Fact   | Update with tampered user on dto has no effect.                       |

## User registry

### UserRegistryServiceTests

`Jube.Test.Service.Repository.UserRegistry.UserRegistryServiceTests` — 46 tests. Category: Service. Collection:
Database.

| Test                                                                  | Kind   | Verifies                                                                     |
|-----------------------------------------------------------------------|--------|------------------------------------------------------------------------------|
| `ConcurrentCreatesOfTheSameUserNameInTwoTenantsLetExactlyOneWinAsync` | Fact   | Concurrent creates of the same user name in two tenants let exactly one win. |
| `InsertPersistsAndReturnsVersionOneAsync`                             | Fact   | Insert persists and returns version one.                                     |
| `GetAllReturnsCreatedRowAndGetByIdReturnsItAsync`                     | Fact   | Get all returns created row and get by id returns it.                        |
| `GetByRoleRegistryGuidReturnsOnlyThatRolesUsersAsync`                 | Fact   | Get by role registry guid returns only that roles users.                     |
| `UpdateIncrementsVersionAndWritesAuditRowAsync`                       | Fact   | Update increments version and writes audit row.                              |
| `RenamingAUserMovesItsTenantMappingToTheNewNameAsync`                 | Fact   | Renaming a user moves its tenant mapping to the new name.                    |
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
| `ListByShowInDirectoryNeedsNoPermissionOnlyTheRoleGrantAsync`                   | Fact   | List by show in directory needs no permission, only the role grant.                  |
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

`Jube.Test.Service.Repository.VisualisationRegistryDatasource.VisualisationRegistryDatasourceServiceTests` — 49 tests.
Category: Service. Collection: Database.

| Test                                                                                         | Kind   | Verifies                                                                                              |
|----------------------------------------------------------------------------------------------|--------|-------------------------------------------------------------------------------------------------------|
| `InsertPersistsAndReturnsVersionOneAsync`                                                    | Fact   | Insert persists and returns version one.                                                              |
| `GetAllReturnsCreatedRowAndGetByIdReturnsItAsync`                                            | Fact   | Get all returns created row and get by id returns it.                                                 |
| `GetByVisualisationRegistryIdReturnsOnlyRowsForThatParentAsync`                              | Fact   | Get by visualisation registry id returns only rows for that parent.                                   |
| `GetByVisualisationRegistryIdActiveOnlyRequiresRoleVisibilityOnBothParentAndRowAsync`        | Fact   | Get by visualisation registry id active only requires role visibility on both parent and row.         |
| `UpdatePreservesCreatedFieldsSetsUpdatedFieldsAndKeepsImportIdAsync`                         | Fact   | Update preserves created fields sets updated fields and keeps import id.                              |
| `UserInTenantBCannotUpdateOrDeleteTenantARowAsync`                                           | Fact   | User in tenant b cannot update or delete tenant a row.                                                |
| `GetAllAsTenantBNeverContainsTenantARowsAsync`                                               | Fact   | Get all as tenant b never contains tenant a rows.                                                     |
| `DeleteSoftDeletesAndRowDisappearsFromReadsAsync`                                            | Fact   | Delete soft deletes and row disappears from reads.                                                    |
| `EveryMethodThrowsForbiddenWhenPermissionMissingAndWritesNoRowAsync`                         | Fact   | Every method throws forbidden when permission missing and writes no row.                              |
| `NullOrBlankUserNameThrowsNotAuthenticatedBeforeAnyQueryAsync`                               | Theory | Null or blank user name throws not authenticated before any query.                                    |
| `UserWithNoUserInTenantRowThrowsNotAuthenticatedAsync`                                       | Fact   | User with no user in tenant row throws not authenticated.                                             |
| `UnknownUserThrowsNotAuthenticatedAsync`                                                     | Fact   | Unknown user throws not authenticated.                                                                |
| `NameRequiredRejectsNullEmptyOrWhitespaceAsync`                                              | Theory | Name required rejects null empty or whitespace.                                                       |
| `NameOverMaximumLengthIsRejectedWithErrorCodeAsync`                                          | Fact   | Name over maximum length is rejected with error code.                                                 |
| `DuplicateNameWithinSameParentFailsButDifferentParentSucceedsAsync`                          | Fact   | Duplicate name within same parent fails but different parent succeeds.                                |
| `InvalidVisualisationRegistryIdIsRejectedAsync`                                              | Fact   | Invalid visualisation registry id is rejected.                                                        |
| `VisualisationRegistryIdFromAnotherTenantIsRejectedAsNotFoundAsync`                          | Fact   | Visualisation registry id from another tenant is rejected as not found.                               |
| `CommandRequiredRejectsEmptyAsync`                                                           | Fact   | Command required rejects empty.                                                                       |
| `PriorityColumnSpanAndRowSpanNegativeAreRejectedAsync`                                       | Theory | Priority column span and row span negative are rejected.                                              |
| `ADatasourceMustShowAChartAGridOrBothAsync`                                                  | Fact   | A datasource must show a chart a grid or both.                                                        |
| `InsertWithInvalidSqlThrowsSqlValidationFailedExceptionAsync`                                | Fact   | Insert with invalid sql throws sql validation failed exception.                                       |
| `InsertWithSqlTheCheckerRefusesSurfacesOnlyTheCheckersOwnMessageAsync`                       | Theory | Insert with sql the checker refuses surfaces only the checkers own message.                           |
| `InsertWithAnUnregisteredTableIsRefusedByTheTableAllowListAsync`                             | Fact   | Insert with an unregistered table is refused by the table allow list.                                 |
| `InsertWithAnUnregisteredTableSurfacesOnlyTheCheckersMessageWhenTheAllowListIsDisabledAsync` | Fact   | Insert with an unregistered table surfaces only the checkers message when the allow list is disabled. |
| `InsertOverARegisteredTableSavesInAllowListModeAsync`                                        | Fact   | Insert over a registered table saves in allow list mode.                                              |
| `InsertWithNonSelectSqlThrowsSqlValidationFailedExceptionWhenSelectOnlyEnforcedAsync`        | Fact   | Insert with non select sql throws sql validation failed exception when select only enforced.          |
| `InsertWithValidSqlPopulatesSeriesAsync`                                                     | Fact   | Insert with valid sql populates series.                                                               |
| `TamperedGuidOnInsertHasNoEffectAsync`                                                       | Fact   | Tampered guid on insert has no effect.                                                                |
| `TamperedIdentityAndAuditFieldsOnUpdateHaveNoEffectAsync`                                    | Fact   | Tampered identity and audit fields on update have no effect.                                          |
| `UpdateCannotMoveADatasourceToAnotherRegistryOfTheSameTenantAsync`                           | Fact   | An update cannot move a datasource to another registry of the same tenant.                            |
| `GetByVisualisationRegistryIdActiveOnlyWithholdsTheSqlFromACallerWhoCannotAuthorAsync`       | Fact   | The active-only list withholds the SQL from a caller without permission 33.                           |
| `UpdateOfLockedRowThrowsNotFoundAsync`                                                       | Fact   | Update of locked row throws not found.                                                                |
| `UpdateOfSoftDeletedRowThrowsNotFoundAsync`                                                  | Fact   | Update of soft deleted row throws not found.                                                          |
| `UpdateOfNeverExistedIdThrowsNotFoundAsync`                                                  | Fact   | Update of never existed id throws not found.                                                          |
| `DeleteOfMissingOrAlreadyDeletedRowThrowsNotFoundAsync`                                      | Fact   | Delete of missing or already deleted row throws not found.                                            |
| `ActiveAndLockedRoundTripBoolToByteAndBackAsync`                                             | Fact   | Active and locked round trip bool to byte and back.                                                   |
| `DescriptionAndColumnsLegacyFieldsAreNeverPersistedAsync`                                    | Fact   | Description and columns legacy fields are never persisted.                                            |
| `GetByIdForMissingIdReturnsNullNotExceptionAsync`                                            | Fact   | Get by id for missing id returns null not exception.                                                  |
| `PreCancelledTokenOnReadThrowsOperationCanceledAsync`                                        | Fact   | Pre cancelled token on read throws operation canceled.                                                |
| `PermissionDeniedLogsWarnNotErrorAndGatesHoldAsync`                                          | Fact   | Permission denied logs warn not error and gates hold.                                                 |
| `SuccessfulInsertLogsExactlyOneInfoAndReadsLogNoInfoAsync`                                   | Fact   | Successful insert logs exactly one info and reads log no info.                                        |
| `WithGatesDisabledHappyPathRecordsNoDebugInfoOrWarnAsync`                                    | Fact   | With gates disabled happy path records no debug info or warn.                                         |
| `FailureLogsErrorWithExceptionAttachedAndStillPropagatesAsync`                               | Fact   | Failure logs error with exception attached and still propagates.                                      |
| `EachCallEmitsOneSpanWithOutcomeTagAsync`                                                    | Fact   | Each call emits one span with outcome tag.                                                            |
| `EachCallRecordsOneDurationMeasurementAsync`                                                 | Fact   | Each call records one duration measurement.                                                           |
| `AuditLogGetsExactlyOneLineIncludingReadsAsync`                                              | Fact   | Audit log gets exactly one line including reads.                                                      |
| `InsertPublishesExactlyOneCreatedEventAsync`                                                 | Fact   | Insert publishes exactly one created event.                                                           |
| `ReadsAndFailuresPublishNothingAsync`                                                        | Fact   | Reads and failures publish nothing.                                                                   |
| `ListAsyncClampsTakeAndPaginatesDeterministicallyAsync`                                      | Fact   | List async clamps take and paginates deterministically.                                               |

### VisualisationRegistryDatasourceIntrospectionServiceTests

`Jube.Test.Service.Repository.VisualisationRegistryDatasource.VisualisationRegistryDatasourceIntrospectionServiceTests` —
28 tests. Category: Service. Collection: Database.

| Test                                                                      | Kind   | Verifies                                                                       |
|---------------------------------------------------------------------------|--------|--------------------------------------------------------------------------------|
| `ARejectedCommandSaysWhereTheDatabaseFoundTheProblemAsync`                | Fact   | A rejected command says where the database found the problem.                  |
| `AnAcceptedCommandHasNoErrorPositionAsync`                                | Fact   | An accepted command has no error position.                                     |
| `IntrospectReturnsEachColumnInSelectOrderWithItsClassifiedTypeAsync`      | Fact   | Introspect returns each column in select order with its classified type.       |
| `IntrospectKeepsSelectOrderWhenItDiffersFromAlphabeticalOrderAsync`       | Fact   | Introspect keeps select order when it differs from alphabetical order.         |
| `IntrospectReportsADuplicateColumnNameAsInvalidWithAHintAsync`            | Fact   | Introspect reports a duplicate column name as invalid with a hint.             |
| `IntrospectAcceptsASingleUnnamedColumnUnderThePostgresDefaultNameAsync`   | Fact   | Introspect accepts a single unnamed column under the postgres default name.    |
| `IntrospectReportsMoreThanOneUnnamedColumnAsInvalidWithAnAliasHintAsync`  | Fact   | Introspect reports more than one unnamed column as invalid with an alias hint. |
| `IntrospectReportsADatabaseSyntaxErrorAsInvalidNotAsAnExceptionAsync`     | Fact   | Introspect reports a database syntax error as invalid not as an exception.     |
| `IntrospectRefusesAnythingButASingleAllowedSelectAsync`                   | Theory | Introspect refuses anything but a single allowed select.                       |
| `IntrospectBindsTheRegistryParametersAtTheirTypesAsync`                   | Fact   | Introspect binds the registry parameters at their types.                       |
| `IntrospectSaysForEveryKindWhetherItFitsTheColumnsByPositionAsync`        | Fact   | Introspect says for every kind whether it fits the columns by position.        |
| `IntrospectLeavesTheShapeEmptyWhenTheSqlIsRejectedAsync`                  | Fact   | Introspect leaves the shape empty when the sql is rejected.                    |
| `IntrospectResolvesTheAskedAboutKindByPositionWhenNothingIsAssignedAsync` | Fact   | Introspect resolves the asked about kind by position when nothing is assigned. |
| `IntrospectMarksTheRolesTheCallerAssignedAsExplicitAsync`                 | Fact   | Introspect marks the roles the caller assigned as explicit.                    |
| `IntrospectReportsAnUnknownKindAsAMappingErrorNotACrashAsync`             | Theory | Introspect reports an unknown kind as a mapping error not a crash.             |
| `IntrospectReportsAChartTheColumnsCannotSatisfyAsAMappingErrorAsync`      | Fact   | Introspect reports a chart the columns cannot satisfy as a mapping error.      |
| `IntrospectReportsABadMappingEntryAsAMappingErrorNotACrashAsync`          | Fact   | Introspect reports a bad mapping entry as a mapping error not a crash.         |
| `IntrospectExplainsWhyAKindDoesNotFitInThePageLanguageAsync`              | Fact   | Introspect explains why a kind does not fit in the page language.              |
| `IntrospectRefusesARegistryOfAnotherTenantAsNotFoundAsync`                | Fact   | Introspect refuses a registry of another tenant as not found.                  |
| `IntrospectRefusesAnUnknownRegistryAsNotFoundAsync`                       | Fact   | Introspect refuses an unknown registry as not found.                           |
| `IntrospectRequiresTheDatasourcePermissionAsync`                          | Fact   | Introspect requires the datasource permission.                                 |
| `IntrospectRefusesACallerWhoIsNotSignedInAsync`                           | Fact   | Introspect refuses a caller who is not signed in.                              |
| `IntrospectRejectsAnIncompleteRequestAsync`                               | Theory | Introspect rejects an incomplete request.                                      |
| `IntrospectRejectsACommandOverTheMaximumLengthAsync`                      | Fact   | Introspect rejects a command over the maximum length.                          |
| `IntrospectStoresNothingAsync`                                            | Fact   | Introspect stores nothing.                                                     |
| `SaveStoresTheSeriesInSelectOrderWithTheClassifiedTypesAsync`             | Fact   | Save stores the series in select order with the classified types.              |
| `UpdateReplacesTheSeriesWithTheNewColumnsInOrderAsync`                    | Fact   | Update replaces the series with the new columns in order.                      |
| `SaveWithADuplicateColumnNameIsRefusedAndStoresNoDatasourceAsync`         | Fact   | Save with a duplicate column name is refused and stores no datasource.         |

### VisualisationRegistryDatasourcePreviewServiceTests

`Jube.Test.Service.Repository.VisualisationRegistryDatasource.VisualisationRegistryDatasourcePreviewServiceTests` — 26
tests. Category: Service. Collection: Database.

| Test                                                                       | Kind   | Verifies                                                                            |
|----------------------------------------------------------------------------|--------|-------------------------------------------------------------------------------------|
| `AChartIsReturnedAsAnSvgWithTheStylesheetAndNoGridWhenTheGridIsOffAsync`   | Fact   | A chart is returned as an svg with the stylesheet and no grid when the grid is off. |
| `WithTheGridOnTheRowsComeAlongAlignedToTheColumnsInSelectOrderAsync`       | Fact   | With the grid on the rows come along aligned to the columns in select order.        |
| `AGridWithNoChartReturnsTheRowsAndNoDisplayAsync`                          | Fact   | A grid with no chart returns the rows and no display.                               |
| `TheStylesheetIsNotResentWhenTheCallerAlreadyHoldsItsVersionAsync`         | Fact   | The stylesheet is not resent when the caller already holds its version.             |
| `AStaleStylesheetVersionGetsTheCurrentStylesheetAsync`                     | Fact   | A stale stylesheet version gets the current stylesheet.                             |
| `InlineStyleIsSelfContainedAndCarriesNoStylesheetAsync`                    | Fact   | Inline style is self contained and carries no stylesheet.                           |
| `TheDarkThemeIsAppliedToTheSvgAsync`                                       | Fact   | The dark theme is applied to the svg.                                               |
| `ARequestedSizeBecomesTheCanvasAndIsClampedAsync`                          | Fact   | A requested size becomes the canvas and is clamped.                                 |
| `AQueryWithNoRowsIsEmptyWithAReasonAndKeepsTheGridColumnsAsync`            | Fact   | A query with no rows is empty with a reason and keeps the grid columns.             |
| `AQueryThatFailsIsReportedInTheCardNotThrownAsync`                         | Theory | A query that fails is reported in the card not thrown.                              |
| `AChartThatCannotBeDrawnIsAnErrorButTheGridStillComesAsync`                | Fact   | A chart that cannot be drawn is an error but the grid still comes.                  |
| `RowsOverTheCardLimitAreDroppedAndFlaggedNotFailedAsync`                   | Fact   | Rows over the card limit are dropped and flagged not failed.                        |
| `AnExplicitMappingChoosesTheSeriesAndLeavesTheOtherColumnsForTheGridAsync` | Fact   | An explicit mapping chooses the series and leaves the other columns for the grid.   |
| `ParametersAreBoundAtTheirDeclaredTypesAndTheSuppliedValueWinsAsync`       | Fact   | Parameters are bound at their declared types and the supplied value wins.           |
| `AParameterValueOfTheWrongTypeIsAValidationErrorAsync`                     | Fact   | A parameter value of the wrong type is a validation error.                          |
| `AParameterOfAnotherRegistryIsRefusedAsync`                                | Fact   | A parameter of another registry is refused.                                         |
| `SpansAndPriorityAreCarriedThroughToTheCardAsync`                          | Fact   | Spans and priority are carried through to the card.                                 |
| `ValuesAreNormalisedForJsonDatesAsIsoTextAndNonFiniteNumbersAsNullAsync`   | Fact   | Values are normalised for json dates as iso text and non finite numbers as null.    |
| `ALongFormResultIsSplitIntoSeriesByTheSeriesRoleAsync`                     | Fact   | A long form result is split into series by the series role.                         |
| `AForeignRegistryIsNotFoundAsync`                                          | Fact   | A foreign registry is not found.                                                    |
| `PreviewRequiresTheDatasourcePermissionAsync`                              | Fact   | Preview requires the datasource permission.                                         |
| `AnIncompletePreviewRequestIsAValidationErrorAsync`                        | Theory | An incomplete preview request is a validation error.                                |
| `AMappingNamingAnUnknownRoleIsAValidationErrorAsync`                       | Fact   | A mapping naming an unknown role is a validation error.                             |
| `AnEmptyMappingEntryOrParameterIsAValidationErrorNotACrashAsync`           | Fact   | An empty mapping entry or parameter is a validation error not a crash.              |
| `ChartKindsListsEveryKindWithItsRolesAsync`                                | Fact   | Chart kinds lists every kind with its roles.                                        |
| `ChartKindsRequiresTheDatasourcePermissionAsync`                           | Fact   | Chart kinds requires the datasource permission.                                     |

### VisualisationRegistryDatasourceDisplayServiceTests

`Jube.Test.Service.Repository.VisualisationRegistryDatasource.VisualisationRegistryDatasourceDisplayServiceTests` — 17
tests. Category: Service. Collection: Database.

| Test                                                                | Kind   | Verifies                                                                    |
|---------------------------------------------------------------------|--------|-----------------------------------------------------------------------------|
| `TheChartKindIsStoredAndComesBackOnAReadAsync`                      | Fact   | The chart kind is stored and comes back on a read.                          |
| `NoChartKindIsStoredAsNullAndReadAsZeroAsync`                       | Fact   | No chart kind is stored as null and read as zero.                           |
| `TheTemplateTextIsStoredAndReturnedAsync`                           | Fact   | The template text is stored and returned.                                   |
| `SeriesAreStoredInSelectOrderWithTheirOrdinalsAsync`                | Fact   | Series are stored in select order with their ordinals.                      |
| `AnExplicitMappingIsStoredOnTheSeriesRowsAndReadBackAsync`          | Fact   | An explicit mapping is stored on the series rows and read back.             |
| `TheMappingIsAlsoReadByGuidAndLeftOutOfListsAsync`                  | Fact   | The mapping is also read by guid and left out of lists.                     |
| `UpdateReplacesTheMappingAndTheVersionKeepsThePreviousKindAsync`    | Fact   | Update replaces the mapping and the version keeps the previous kind.        |
| `ClearingTheMappingOnUpdateReturnsTheColumnsToPositionalAsync`      | Fact   | Clearing the mapping on update returns the columns to positional.           |
| `AnUnknownChartKindIsAValidationErrorAsync`                         | Theory | An unknown chart kind is a validation error.                                |
| `AnUnknownRoleAndADuplicateRoleAreValidationErrorsAsync`            | Fact   | An unknown role and a duplicate role are validation errors.                 |
| `AColumnInTwoRolesIsAValidationErrorAsync`                          | Fact   | A column in two roles is a validation error.                                |
| `AMappingNamingAColumnTheSqlDoesNotReturnIsRefusedWithTheNameAsync` | Fact   | A mapping naming a column the sql does not return is refused with the name. |
| `AMappingNamingAMissingColumnIsRefusedEvenWithNoChartKindAsync`     | Fact   | A mapping naming a missing column is refused even with no chart kind.       |
| `AChartThatTheColumnsCannotSatisfyIsRefusedAtSaveWithAReasonAsync`  | Fact   | A chart that the columns cannot satisfy is refused at save with a reason.   |
| `ARefusedMappingStoresNoDatasourceAsync`                            | Fact   | A refused mapping stores no datasource.                                     |
| `ATemplateOverTheMaximumLengthIsAValidationErrorAsync`              | Fact   | A template over the maximum length is a validation error.                   |
| `ANullMappingFromAClientIsTreatedAsNoMappingAsync`                  | Fact   | A null mapping from a client is treated as no mapping.                      |

### VisualisationRegistryDatasourceTemplateServiceTests

`Jube.Test.Service.Repository.VisualisationRegistryDatasource.VisualisationRegistryDatasourceTemplateServiceTests` — 11
tests. Category: Service. Collection: Database.

| Test                                                                         | Kind   | Verifies                                                                           |
|------------------------------------------------------------------------------|--------|------------------------------------------------------------------------------------|
| `APreviewRendersTheTemplateAsEscapedHtmlWithNoChartStylesheetAsync`          | Fact   | A preview renders the template as escaped html with no chart stylesheet.           |
| `ATemplateAndTheGridComeBackTogetherWhenTheGridIsOnAsync`                    | Fact   | A template and the grid come back together when the grid is on.                    |
| `ABrokenTemplateIsAnErrorCardWithLineAndColumnButTheGridStillComesAsync`     | Fact   | A broken template is an error card with line and column but the grid still comes.  |
| `ATemplateKindWithNoTextIsAnErrorCardAsync`                                  | Fact   | A template kind with no text is an error card.                                     |
| `HostileRowDataIsEscapedInThePreviewAsync`                                   | Fact   | Hostile row data is escaped in the preview.                                        |
| `IntrospectionReportsATemplateErrorOnlyWhenTheTemplateKindIsAskedAboutAsync` | Fact   | Introspection reports a template error only when the template kind is asked about. |
| `TheTemplateKindAlwaysFitsByPositionAsync`                                   | Fact   | The template kind always fits by position.                                         |
| `ChartKindsListsTheTemplateKindWithNoRolesAsync`                             | Fact   | Chart kinds lists the template kind with no roles.                                 |
| `AValidTemplateIsSavedAndReadBackAsync`                                      | Fact   | A valid template is saved and read back.                                           |
| `AnInvalidTemplateIsRefusedOnSaveAsync`                                      | Theory | An invalid template is refused on save.                                            |
| `ATemplateWithAMappingRoleIsRefusedAsync`                                    | Fact   | A template with a mapping role is refused.                                         |

### VisualisationRegistryDatasourceLocalisationServiceTests

`Jube.Test.Service.Repository.VisualisationRegistryDatasource.VisualisationRegistryDatasourceLocalisationServiceTests` —
5 tests. Category: Service. Collection: Database.

| Test                                                                             | Kind   | Verifies                                                                                 |
|----------------------------------------------------------------------------------|--------|------------------------------------------------------------------------------------------|
| `ChartKindsAreListedInThePageLanguageAsync`                                      | Theory | Chart kinds are listed in the page language.                                             |
| `AFailedPreviewExplainsItselfInThePageLanguageAsync`                             | Fact   | A failed preview explains itself in the page language.                                   |
| `AChartThatCannotBeDrawnExplainsItselfInThePageLanguageWithTranslatedTermsAsync` | Fact   | A chart that cannot be drawn explains itself in the page language with translated terms. |
| `ADuplicateColumnIsReportedByIntrospectionInThePageLanguageAsync`                | Fact   | A duplicate column is reported by introspection in the page language.                    |
| `AMappingThatNamesAMissingColumnIsRefusedInThePageLanguageAsync`                 | Fact   | A mapping that names a missing column is refused in the page language.                   |

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

`Jube.Test.Service.Repository.VisualisationRegistryDatasourceSeries.VisualisationRegistryDatasourceSeriesServiceTests` —
12 tests. Category: Service. Collection: Database.

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

`Jube.Test.Service.Repository.VisualisationRegistryParameter.VisualisationRegistryParameterServiceTests` — 47 tests.
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
| `UpdateCannotMoveAParameterToAnotherRegistryOfTheSameTenantAsync`                         | Fact   | An update cannot move a parameter to another registry of the same tenant.                         |
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

### FormValidatorEngineTests

`Jube.Test.Service.Repository.CaseWorkflowFormValidator.FormValidatorEngineTests` — 18 tests. Category: Unit.

| Test                                                    | Kind | Verifies                                                            |
|---------------------------------------------------------|------|---------------------------------------------------------------------|
| `RequiresAValueForARequiredRule`                        | Fact | Requires a value for a required rule                                |
| `RequiresACheckboxToBeTickedWhenItIsRequired`           | Fact | Requires a checkbox to be ticked when it is required                |
| `RequiresAValueOnlyWhenTheOtherFieldHasTheNamedValue`   | Fact | Requires a value only when the other field has the named value      |
| `ChecksANumberAgainstTheRangeAndLeavesAnEmptyBoundOpen` | Fact | Checks a number against the range and leaves an empty bound open    |
| `ChecksADateAgainstTheRange`                            | Fact | Checks a date against the range                                     |
| `ChecksTheLengthInCharacters`                           | Fact | Checks the length in characters                                     |
| `ChecksAPatternAndTreatsABrokenOneAsAFailure`           | Fact | Checks a pattern and treats a broken one as a failure               |
| `CannotBeSlowedByACatastrophicPattern`                  | Fact | Cannot be slowed by a catastrophic pattern                          |
| `ComparesTwoFieldsByTheNamedRelation`                   | Fact | Compares two fields by the named relation                           |
| `ComparesDatesAsDatesAndNumbersAsNumbers`               | Fact | Compares dates as dates and numbers as numbers                      |
| `ChecksMembershipOfAListAndADictionary`                 | Fact | Checks membership of a list and a dictionary                        |
| `PassesASqlRuleOnlyWhenTheScalarIsTrue`                 | Fact | Passes a sql rule only when the scalar is true                      |
| `FailsClosedWhenTheSqlRaisesOrTimesOut`                 | Fact | Fails closed when the sql raises or times out                       |
| `BindsTheValueTheOtherFieldsAndTheCaseToTheSql`         | Fact | Binds the value the other fields and the case to the sql            |
| `SkipsAFieldTheTemplateDoesNotShowOrThatIsNotInTheForm` | Fact | Skips a field the template does not show or that is not in the form |
| `ReportsOnlyTheFirstFailureOfAField`                    | Fact | Reports only the first failure of a field                           |
| `CarriesTheAuthoredMessage`                             | Fact | Carries the authored message                                        |
| `TestsOneRuleAndTimesIt`                                | Fact | Tests one rule and times it                                         |

### CaseWorkflowChecklistServiceTests

`Jube.Test.Service.Repository.CaseWorkflowChecklist.CaseWorkflowChecklistServiceTests` — 13 tests. Category:
Service. Collection: Database.

| Test                                                             | Kind | Verifies                                                            |
|------------------------------------------------------------------|------|---------------------------------------------------------------------|
| `CreateWithNullUserNameThrowsNotAuthenticatedAsync`              | Fact | Create with null user name throws not authenticated.                |
| `ListWithoutPermissionThrowsForbiddenAsync`                      | Fact | List without permission throws forbidden.                           |
| `InsertThenListAndGetRoundTripAChecklistUnderItsStatusAsync`     | Fact | Insert then list and get round trip a checklist under its status.   |
| `InsertResolvesTheStatusGuidFromTheStatusIdAsync`                | Fact | Insert resolves the status guid from the status id.                 |
| `ABlankPriorityIsStoredAsZeroAsync`                              | Fact | A blank priority is stored as zero.                                 |
| `ListIsOrderedByPriorityAscendingThenNameThenIdAsync`            | Fact | List is ordered by priority ascending then name then id.            |
| `InsertRefusesMissingNameNegativePriorityAndUnknownStatusAsync`  | Fact | Insert refuses missing name negative priority and unknown status.   |
| `ValidateReportsFailuresWithoutStoringAnythingAsync`             | Fact | Validate reports failures without storing anything.                 |
| `UpdateKeepsAVersionAuditRowAndRefusesAStaleVersionAsync`        | Fact | Update keeps a version audit row and refuses a stale version.       |
| `UpdateCannotMoveAChecklistToAnotherStatusAsync`                 | Fact | Update cannot move a checklist to another status.                   |
| `DeleteSoftDeletesAndAnUnknownIdIsNotFoundAsync`                 | Fact | Delete soft deletes and an unknown id is not found.                 |
| `AnotherTenantCannotSeeChangeOrDeleteAChecklistAsync`            | Fact | Another tenant cannot see change or delete a checklist.             |
| `ActiveByStatusGuidExcludesInactiveChecklistsAndKeepsOrderAsync` | Fact | Active by status guid excludes inactive checklists and keeps order. |

### CaseWorkflowChecklistItemServiceTests

`Jube.Test.Service.Repository.CaseWorkflowChecklistItem.CaseWorkflowChecklistItemServiceTests` — 27 tests. Category:
Service. Collection: Database.

| Test                                                                               | Kind | Verifies                                                                                       |
|------------------------------------------------------------------------------------|------|------------------------------------------------------------------------------------------------|
| `CreateWithNullUserNameThrowsNotAuthenticatedAsync`                                | Fact | Create with null user name throws not authenticated.                                           |
| `ListWithoutPermissionThrowsForbiddenAsync`                                        | Fact | List without permission throws forbidden.                                                      |
| `AnItemWithNoSqlAndNoSlaIsAcceptedAndStoredAsNullsAsync`                           | Fact | An item with no sql and no sla is accepted and stored as nulls.                                |
| `AnItemCarriesShowSqlValidateSqlAndSlaWhenTheyAreSuppliedAsync`                    | Fact | An item carries show sql validate sql and sla when they are supplied.                          |
| `ItemsAreOrderedByPriorityThenNameAsync`                                           | Fact | Items are ordered by priority then name.                                                       |
| `InsertChecksNamePriorityAndSlaAsync`                                              | Fact | Insert checks name priority and sla.                                                           |
| `InsertOfAnItemWithSqlNeedsTheSqlAuthoringPermissionAsync`                         | Fact | Insert of an item with sql needs the sql authoring permission.                                 |
| `InsertRefusesUnknownParametersBlockedTablesAndWritesAsync`                        | Fact | Insert refuses unknown parameters blocked tables and writes.                                   |
| `UpdateKeepsAVersionAuditRowAndRefusesAStaleVersionAsync`                          | Fact | Update keeps aversion audit row and refuses astale version.                                    |
| `UpdateCanClearTheSqlAndSlaBackToNullAsync`                                        | Fact | Update can clear the sql and sla back to null.                                                 |
| `ChangingTheSqlOfAnItemNeedsThePermissionButRenamingItDoesNotAsync`                | Fact | Changing the sql of an item needs the permission but renaming it does not.                     |
| `DeleteSoftDeletesAndAnUnknownIdIsNotFoundAsync`                                   | Fact | Delete soft deletes and an unknown id is not found.                                            |
| `DeletingAnItemWithSqlNeedsTheSqlAuthoringPermissionAsync`                         | Fact | Deleting an item with sql needs the sql authoring permission.                                  |
| `ATenantCannotSeeOrChangeAnotherTenantsItemsAsync`                                 | Fact | Atenant cannot see or change another tenants items.                                            |
| `TestRunsEachConditionAgainstTheRealDatabaseAsync`                                 | Fact | Test runs each condition against the real database.                                            |
| `TestOfABlankConditionPassesWithoutNeedingTheSqlPermissionAsync`                   | Fact | Test of ablank condition passes without needing the sql permission.                            |
| `TestOfAConditionWithSqlNeedsTheSqlAuthoringPermissionAsync`                       | Fact | Test of acondition with sql needs the sql authoring permission.                                |
| `TestReportsAFailureOfTheSqlItselfWithoutRaisingAsync`                             | Fact | Test reports afailure of the sql itself without raising.                                       |
| `TestRefusesAnUnknownConditionAsync`                                               | Fact | Test refuses an unknown condition.                                                             |
| `ValidateReportsFailuresWithoutStoringAnythingAsync`                               | Fact | Validate reports failures without storing anything.                                            |
| `ASwitchedOnConditionWithoutSqlIsInvalidAndAnEmptyOneSwitchedOffIsNotAsync`        | Fact | A switched on condition without sql is invalid and an empty one switched off is not.           |
| `SwitchingAConditionOffRetainsItsSqlAndMessageAndSwitchingItOnAgainKeepsThemAsync` | Fact | Switching a condition off retains its sql and message and switching it on again keeps them.    |
| `ARetainedSqlOfASwitchedOffConditionIsStillCheckedForSafetyAsync`                  | Fact | A retained sql of a switched off condition is still checked for safety.                        |
| `SwitchingAConditionOnNeedsTheSqlPermissionButSwitchingItOffAndRenamingDoNotAsync` | Fact | Switching a condition on needs the sql permission but switching it off and renaming do not.    |
| `InsertWithAConditionSwitchedOnNeedsTheSqlPermissionAsync`                         | Fact | Insert with a condition switched on needs the sql permission.                                  |
| `TheVersionRowOfAnUpdateCarriesTheSwitchesAsTheyWereAsync`                         | Fact | The version row of an update carries the switches as they were.                                |
| `TestStillRunsTheSqlOfASwitchedOffConditionSoItCanBeTriedBeforeSwitchingItOnAsync` | Fact | Test still runs the sql of a switched off condition so it can be tried before switching it on. |

### SeededCaseChecklistTests

`Jube.Test.Service.Repository.CaseWorkflowChecklistItem.SeededCaseChecklistTests` — 5 tests. Category: Service.
Collection: Database.

| Test                                                                       | Kind | Verifies                                                                             |
|----------------------------------------------------------------------------|------|--------------------------------------------------------------------------------------|
| `TheDemonstrationWorkflowCarriesThreeChecklistsAndSixItemsInPriorityOrder` | Fact | The demonstration workflow carries three checklists and six items in priority order. |
| `ShowValidationAndSlaAreEachOptionalAndStoredAsNullWhenAbsent`             | Fact | Show validation and sla are each optional and stored as null when absent.            |
| `TheBackfillSwitchesAConditionOnExactlyWhereTheSeededItemHasItsSql`        | Fact | The backfill switches a condition on exactly where the seeded item has its sql.      |
| `NoItemOrItsVersionRowIsLeftWithoutAnExplicitSwitchAfterTheBackfill`       | Fact | No item or its version row is left without an explicit switch after the backfill.    |
| `EverySeededSqlRunsAsASingleBooleanAgainstARealCase`                       | Fact | Every seeded sql runs as a single boolean against a real case.                       |

### CaseWorkflowFormValidatorServiceTests

`Jube.Test.Service.Repository.CaseWorkflowFormValidator.CaseWorkflowFormValidatorServiceTests` — 15 tests. Category:
Service. Collection: Database.

| Test                                                            | Kind | Verifies                                                            |
|-----------------------------------------------------------------|------|---------------------------------------------------------------------|
| `CreateWithNullUserNameThrowsNotAuthenticatedAsync`             | Fact | Create with null user name throws not authenticated.                |
| `ListWithoutPermissionThrowsForbiddenAsync`                     | Fact | List without permission throws forbidden.                           |
| `InsertThenListAndGetRoundTripAValidatorUnderItsWorkflowAsync`  | Fact | Insert then list and get round trip a validator under its workflow. |
| `InsertRefusesAFieldTheFormTemplateDoesNotDeclareAsync`         | Fact | Insert refuses a field the form template does not declare.          |
| `InsertRefusesARuleThatDoesNotApplyToTheTypeOfTheFieldAsync`    | Fact | Insert refuses a rule that does not apply to the type of the field. |
| `InsertChecksTheArgumentsOfEachKindAsync`                       | Fact | Insert checks the arguments of each kind.                           |
| `InsertOfASqlRuleNeedsTheSqlAuthoringPermissionAsync`           | Fact | Insert of a sql rule needs the sql authoring permission.            |
| `InsertOfASqlRuleRefusesUnknownParametersAndBlockedTablesAsync` | Fact | Insert of a sql rule refuses unknown parameters and blocked tables. |
| `UpdateKeepsAVersionAuditRowAndRefusesAStaleVersionAsync`       | Fact | Update keeps a version audit row and refuses a stale version.       |
| `DeleteSoftDeletesAndAnUnknownIdIsNotFoundAsync`                | Fact | Delete soft deletes and an unknown id is not found.                 |
| `DeletingASqlRuleNeedsTheSqlAuthoringPermissionAsync`           | Fact | Deleting a sql rule needs the sql authoring permission.             |
| `ATenantCannotSeeOrChangeAnotherTenantsValidatorsAsync`         | Fact | A tenant cannot see or change another tenants validators.           |
| `TestRunsASavedSqlRuleAgainstTheRealDatabaseAsync`              | Fact | Test runs a saved sql rule against the real database.               |
| `TestReportsAFailureOfTheSqlItselfWithoutRaisingAsync`          | Fact | Test reports a failure of the sql itself without raising.           |
| `ValidateReportsFailuresWithoutStoringAnythingAsync`            | Fact | Validate reports failures without storing anything.                 |

### CaseWorkflowReleaseHookServiceTests

`Jube.Test.Service.Repository.CaseWorkflowReleaseHook.CaseWorkflowReleaseHookServiceTests` — 18 tests. Category:
Service. Collection: Database.

| Test                                                                                | Kind   | Verifies                                                                                      |
|-------------------------------------------------------------------------------------|--------|-----------------------------------------------------------------------------------------------|
| `CreateWithNullUserNameThrowsNotAuthenticatedAsync`                                 | Fact   | Create with null user name throws not authenticated.                                          |
| `ListWithoutPermissionThrowsForbiddenNamingPermission69Async`                       | Fact   | List without permission throws forbidden naming permission69.                                 |
| `InsertThenListAndGetRoundTripAHookUnderItsWorkflowAsync`                           | Fact   | Insert then list and get round trip a hook under its workflow.                                |
| `UpdateKeepsTheEarlierStateAsAVersionAndChecksTheVersionAsync`                      | Fact   | Update keeps the earlier state as a version and checks the version.                           |
| `ALockedHookCannotBeUpdatedOrDeletedAsync`                                          | Fact   | A locked hook cannot be updated or deleted.                                                   |
| `DeleteIsSoftAndTheHookNoLongerListsAsync`                                          | Fact   | Delete is soft and the hook no longer lists.                                                  |
| `AnotherTenantCannotSeeUpdateOrDeleteTheHookAsync`                                  | Fact   | Another tenant cannot see update or delete the hook.                                          |
| `TheNameMustBeUniqueWithinTheWorkflowIgnoringCaseAsync`                             | Fact   | The name must be unique within the workflow ignoring case.                                    |
| `AReleaseUrlWhoseHostIsNotLiteralOrWithAnUnknownTokenIsRefusedAsync`                | Theory | A release url whose host is not literal or with an unknown token is refused.                  |
| `TheRefuseUrlFollowsTheSameRulesAndMayBeLeftOutAsync`                               | Fact   | The refuse url follows the same rules and may be left out.                                    |
| `TheHeaderAndTheCredentialVariableComeTogetherAndNeverNameAnyOtherVariableAsync`    | Theory | The header and the credential variable come together and never name any other variable.       |
| `AValidHeaderAndCredentialVariableAreStoredAsNamesOnlyAsync`                        | Fact   | A valid header and credential variable are stored as names only.                              |
| `TheSourceFieldAndValueComeTogetherAsync`                                           | Theory | The source field and value come together.                                                     |
| `ABodyIsOnlyForPostAndTakesOnlyKnownTokensAsync`                                    | Fact   | A body is only for post and takes only known tokens.                                          |
| `ValidateReportsTheSameFailuresWithoutSavingAnythingAsync`                          | Fact   | Validate reports the same failures without saving anything.                                   |
| `TheCaseWorkflowMustExistAsync`                                                     | Fact   | The case workflow must exist.                                                                 |
| `AReleaseOrRefuseUrlOnAnInternalHostIsRefusedAtSaveTimeAsync`                       | Theory | A release or refuse url on an internal host is refused at save time.                          |
| `AnAllowListedInternalHostOrRangeIsAcceptedAtSaveTimeAndNothingElseInternalIsAsync` | Fact   | An allow listed internal host or range is accepted at save time and nothing else internal is. |

### TemplateTextTests

`Jube.Test.Service.CaseDispatch.TemplateTextTests` — 7 tests. Category: Unit.

| Test                                                      | Kind | Verifies                                                          |
|-----------------------------------------------------------|------|-------------------------------------------------------------------|
| `RendersTokensAndFiltersFromTheRowsWithTheChosenEncoding` | Fact | Renders tokens and filters from the rows with the chosen encoding |
| `RefusesANameThatIsNotARootOfTheSqlDialect`               | Fact | Refuses a name that is not a root of the sql dialect              |
| `RefusesAnUnclosedTokenAndTextThatIsTooLong`              | Fact | Refuses an unclosed token and text that is too long               |
| `LeavesPlainTextAndAnEmptyTemplateAlone`                  | Fact | Leaves plain text and an empty template alone                     |
| `TreatsAMissingRowAsEmptyRatherThanAnError`               | Fact | Treats a missing row as empty rather than an error                |
| `AcceptsTheFormSqlDialectAndRefusesTheLegacyRootsInIt`    | Fact | Accepts the form sql dialect and refuses the legacy roots in it   |
| `DrawsAFormFromTheRowsInTheSqlDialect`                    | Fact | Draws a form from the rows in the sql dialect                     |

### DispatchGuardTests

`Jube.Test.Service.CaseDispatch.DispatchGuardTests` — 10 tests. Category: Unit.

| Test                                               | Kind | Verifies                                                   |
|----------------------------------------------------|------|------------------------------------------------------------|
| `AcceptsOneOrMoreWellFormedAddresses`              | Fact | Accepts one or more well formed addresses                  |
| `RefusesAnythingThatIsNotJustAddresses`            | Fact | Refuses anything that is not just addresses                |
| `StripsLineBreaksFromASubjectSoItCannotAddHeaders` | Fact | Strips line breaks from a subject so it cannot add headers |
| `AcceptsATelephoneNumberAndRefusesOtherText`       | Fact | Accepts a telephone number and refuses other text          |
| `FillsOnlyThePathAndQueryOfAFixedHost`             | Fact | Fills only the path and query of a fixed host              |
| `RefusesATokenInTheHostOrSchemeOfTheEndpoint`      | Fact | Refuses a token in the host or scheme of the endpoint      |
| `RefusesAValueThatTriesToLeaveTheHost`             | Fact | Refuses a value that tries to leave the host               |
| `RefusesEndpointsThatAreEmptyRelativeOrNotHttp`    | Fact | Refuses endpoints that are empty relative or not http      |
| `AcceptsAFixedEndpointWithNoTokens`                | Fact | Accepts a fixed endpoint with no tokens                    |
| `BuildsAJsonBodyFromTheRowsAndTheCase`             | Fact | Builds a json body from the rows and the case              |

### CaseWorkflowSqlAuthoringTests

`Jube.Test.Service.Repository.CaseWorkflowFormValidator.CaseWorkflowSqlAuthoringTests` — 5 tests. Category:
Service. Collection: Database.

| Test                                                                   | Kind | Verifies                                                                       |
|------------------------------------------------------------------------|------|--------------------------------------------------------------------------------|
| `AFormWithoutSqlNeedsNoSqlPermissionAsync`                             | Fact | A form without sql needs no sql permission.                                    |
| `InsertingFormSqlNeedsTheSqlAuthoringPermissionAsync`                  | Fact | Inserting form sql needs the sql authoring permission.                         |
| `FormSqlThatReadsABlockedTableOrAnUnknownParameterIsRefusedAsync`      | Fact | Form sql that reads a blocked table or an unknown parameter is refused.        |
| `UpdatingAFormMayLeaveItsSqlAloneButNeedsThePermissionToChangeItAsync` | Fact | Updating a form may leave its sql alone but needs the permission to change it. |
| `AMacroNeedsTheSqlAuthoringPermissionToHaveSqlAsync`                   | Fact | A macro needs the sql authoring permission to have sql.                        |

## Graph registry

### GraphRegistryServiceTests

`Jube.Test.Service.Repository.GraphRegistry.GraphRegistryServiceTests` — 4 tests.
Category: Service. Collection: Database.

| Test                                                                   | Kind | Verifies                                                                   |
|------------------------------------------------------------------------|------|----------------------------------------------------------------------------|
| `CreatesReadsUpdatesAndSoftDeletesARegistryWritingAVersionRowAsync`    | Fact | Creates reads updates and soft deletes aregistry writing aversion row.     |
| `RefusesInvalidDefaultsAndDuplicateNamesAsync`                         | Fact | Refuses invalid defaults and duplicate names.                              |
| `IsTenantScopedAndPermissionGatedAsync`                                | Fact | Is tenant scoped and permission gated.                                     |
| `TheDirectoryListsOnlyGrantedActiveRegistriesShownInTheDirectoryAsync` | Fact | The directory lists only granted active registries shown in the directory. |

### EntityAnalysisModelSvirfdServiceTests

`Jube.Test.Service.Repository.EntityAnalysisModelSvirfd.EntityAnalysisModelSvirfdServiceTests` — 14 tests. Category:
Service. Collection: Database.

| Test                                                                                      | Kind | Verifies                                                                                          |
|-------------------------------------------------------------------------------------------|------|---------------------------------------------------------------------------------------------------|
| `CreatesUpdatesAndDeletesAnEntityWithAVersionRowAsync`                                    | Fact | Creates Updates And Deletes An Entity With AVersion Row.                                          |
| `ValidatesTheNameAndTheIpFieldAsync`                                                      | Fact | Validates The Name And The Ip Field.                                                              |
| `ListsOnlyTheModelsIpTypedRequestFieldsAsync`                                             | Fact | Lists Only The Models Ip Typed Request Fields.                                                    |
| `DeleteWritesTwoVersionRowsBumpsTheVersionAndRefusesASecondDeleteAsync`                   | Fact | Delete Writes Two Version Rows Bumps The Version And Refuses ASecond Delete.                      |
| `TheApprovedOnlyRepositoryReadHidesAnUnapprovedEntityAndShowsItOnceApprovedAsync`         | Fact | The Approved Only Repository Read Hides An Unapproved Entity And Shows It Once Approved.          |
| `TheApprovedOnlyRepositoryReadServesTheLastApprovedVersionWhileAnEditAwaitsApprovalAsync` | Fact | The Approved Only Repository Read Serves The Last Approved Version While An Edit Awaits Approval. |
| `ResubmitOfNonRejectedVersionThrowsApprovalRefusedAndWritesNoRowAsync`                    | Fact | Resubmit Of Non Rejected Version Throws Approval Refused And Writes No Row.                       |
| `ResubmitOfRejectedVersionBumpsVersionArchivesOldAndKeepsContentAsync`                    | Fact | Resubmit Of Rejected Version Bumps Version Archives Old And Keeps Content.                        |
| `ResubmitOfRejectedDeleteKeepsDeletedTrueAndReopensApprovalAsync`                         | Fact | Resubmit Of Rejected Delete Keeps Deleted True And Reopens Approval.                              |
| `RetractOfNonRejectedVersionThrowsApprovalRefusedAsync`                                   | Fact | Retract Of Non Rejected Version Throws Approval Refused.                                          |
| `RetractWithNoApprovedBaselineThrowsApprovalRefusedAsync`                                 | Fact | Retract With No Approved Baseline Throws Approval Refused.                                        |
| `RetractOfRejectedEditRestoresLastApprovedContentAndStampsTheRejectionRetractedAsync`     | Fact | Retract Of Rejected Edit Restores Last Approved Content And Stamps The Rejection Retracted.       |
| `RetractOfRejectedDeleteUndeletesAsync`                                                   | Fact | Retract Of Rejected Delete Undeletes.                                                             |
| `ResubmitAndRetractAreForbiddenWithoutTheAuthorPermissionAsync`                           | Fact | Resubmit And Retract Are Forbidden Without The Author Permission.                                 |

### EntityAnalysisModelGraphRecallServiceTests

`Jube.Test.Service.Repository.EntityAnalysisModelGraphRecall.EntityAnalysisModelGraphRecallServiceTests` — 14 tests.
Category: Service. Collection: Database.

| Test                                                                                      | Kind | Verifies                                                                                          |
|-------------------------------------------------------------------------------------------|------|---------------------------------------------------------------------------------------------------|
| `CreatesUpdatesAndDeletesAnEntityWithAVersionRowAsync`                                    | Fact | Creates updates and deletes an entity with aversion row.                                          |
| `ValidatesTheNameTheFieldAndTheCounterpartAsync`                                          | Fact | Validates the name the field and the counterpart.                                                 |
| `ListsTheModelsRequestFieldsWithTheirReportTableFlagAsync`                                | Fact | Lists the models request fields with their report table flag.                                     |
| `DeleteWritesTwoVersionRowsBumpsTheVersionAndRefusesASecondDeleteAsync`                   | Fact | Delete writes two version rows bumps the version and refuses a second delete.                     |
| `TheApprovedOnlyRepositoryReadHidesAnUnapprovedEntityAndShowsItOnceApprovedAsync`         | Fact | The approved only repository read hides an unapproved entity and shows it once approved.          |
| `TheApprovedOnlyRepositoryReadServesTheLastApprovedVersionWhileAnEditAwaitsApprovalAsync` | Fact | The approved only repository read serves the last approved version while an edit awaits approval. |
| `ResubmitOfNonRejectedVersionThrowsApprovalRefusedAndWritesNoRowAsync`                    | Fact | Resubmit of non rejected version throws approval refused and writes no row.                       |
| `ResubmitOfRejectedVersionBumpsVersionArchivesOldAndKeepsContentAsync`                    | Fact | Resubmit of rejected version bumps version archives old and keeps content.                        |
| `ResubmitOfRejectedDeleteKeepsDeletedTrueAndReopensApprovalAsync`                         | Fact | Resubmit of rejected delete keeps deleted true and reopens approval.                              |
| `RetractOfNonRejectedVersionThrowsApprovalRefusedAsync`                                   | Fact | Retract of non rejected version throws approval refused.                                          |
| `RetractWithNoApprovedBaselineThrowsApprovalRefusedAsync`                                 | Fact | Retract with no approved baseline throws approval refused.                                        |
| `RetractOfRejectedEditRestoresLastApprovedContentAndStampsTheRejectionRetractedAsync`     | Fact | Retract of rejected edit restores last approved content and stamps the rejection retracted.       |
| `RetractOfRejectedDeleteUndeletesAsync`                                                   | Fact | Retract of rejected delete undeletes.                                                             |
| `ResubmitAndRetractAreForbiddenWithoutTheAuthorPermissionAsync`                           | Fact | Resubmit and retract are forbidden without the author permission.                                 |

### GraphRegistryEdgeServiceTests

`Jube.Test.Service.Repository.GraphRegistryEdge.GraphRegistryEdgeServiceTests` — 3 tests.
Category: Service. Collection: Database.

| Test                                                           | Kind | Verifies                                                          |
|----------------------------------------------------------------|------|-------------------------------------------------------------------|
| `CreatesUpdatesAndDeletesAnEdgeDefinitionWithAVersionRowAsync` | Fact | Creates updates and deletes an edge definition with aversion row. |
| `ValidatesKeysKindAndWeightsAsync`                             | Fact | Validates keys kind and weights.                                  |
| `ListsTheReportKeysOfTheTenantsModelsWithFlagsAsync`           | Fact | Lists the report keys of the tenants models with flags.           |

### CaseTagServiceTests

`Jube.Test.Service.Repository.CaseTag.CaseTagServiceTests` — 1 test. Category: Service. Collection: Database.

| Test                                                                  | Kind | Verifies                                                                                                               |
|-----------------------------------------------------------------------|------|------------------------------------------------------------------------------------------------------------------------|
| `MergingTagsIsRefusedOnACaseLockedToAnotherUserAndWritesNothingAsync` | Fact | Merging tags is refused with a conflict on a case locked to another user, and works on an unlocked or own-locked case. |

### SanctionExemptionServiceTests

`Jube.Test.Service.Repository.SanctionExemption.SanctionExemptionServiceTests` — 10 tests. Category: Service.

| Test                                                                      | Kind | Verifies                                                                         |
|---------------------------------------------------------------------------|------|----------------------------------------------------------------------------------|
| `ARequestIsStoredPendingWithTheNormalisedNameAndTheRequesterAsync`        | Fact | A request is stored pending with the normalised name and the requester.          |
| `ARequestWithoutTheSanctionsPermissionIsForbiddenAsync`                   | Fact | A request without the sanctions permission is forbidden.                         |
| `ARequestThatIsIncompleteInThePastOrForAnUnknownListIsRefusedAsync`       | Fact | A request that is incomplete in the past or for an unknown list is refused.      |
| `AnExemptionAlreadyPendingOrApprovedForTheSameNameAndEntryIsRefusedAsync` | Fact | An exemption already pending or approved for the same name and entry is refused. |
| `AnotherApproverApprovesAndTheRequesterCannotApproveTheirOwnAsync`        | Fact | Another approver approves and the requester cannot approve their own.            |
| `RejectingRecordsTheDecisionAndFreesTheNameAndEntryForANewRequestAsync`   | Fact | Rejecting records the decision and frees the name and entry for a new request.   |
| `OnlyAnApproverMayDecideAndADecidedExemptionCannotBeDecidedAgainAsync`    | Fact | Only an approver may decide and a decided exemption cannot be decided again.     |
| `AnotherTenantCannotSeeOrDecideAnExemptionAsync`                          | Fact | Another tenant cannot see or decide an exemption.                                |
| `TheRequesterCanRetractAndSoCanAnApproverButNotAnUninvolvedAnalystAsync`  | Fact | The requester can retract and so can an approver but not an uninvolved analyst.  |
| `AnApprovedExemptionCanBeRetractedAndThenLeavesTheApprovedListAsync`      | Fact | An approved exemption can be retracted and then leaves the approved list.        |

### CaseLinkServiceTests

`Jube.Test.Service.Repository.CaseLink.CaseLinkServiceTests` — 6 tests. Category: Service.

| Test                                                                   | Kind | Verifies                                                                     |
|------------------------------------------------------------------------|------|------------------------------------------------------------------------------|
| `ALinkIsSeenFromBothCasesWithTheOtherCasesKeyAndStatusAsync`           | Fact | A link is seen from both cases with the other cases key and status.          |
| `LinkingTheSamePairAgainInEitherDirectionIsRefusedAsync`               | Fact | Linking the same pair again in either direction is refused.                  |
| `ASelfLinkAMissingCaseAndAnOverlongReasonAreRefusedAsync`              | Fact | A self link a missing case and an overlong reason are refused.               |
| `LinkingNeedsThePermissionAndBothCasesMustBeVisibleToTheCallerAsync`   | Fact | Linking needs the permission and both cases must be visible to the caller.   |
| `ALinkedCaseTheCallerCannotSeeIsNeverRevealedAsync`                    | Fact | A linked case the caller cannot see is never revealed.                       |
| `UnlinkingRemovesTheLinkFromBothSidesAndUnlinkingAgainIsNotFoundAsync` | Fact | Unlinking removes the link from both sides and unlinking again is not found. |

### CaseAgentRunServiceTests

`Jube.Test.Service.Repository.CaseAgentRun.CaseAgentRunServiceTests` — 8 tests. Category: Service.

| Test                                                                 | Kind    | Verifies                                                                                                   |
|----------------------------------------------------------------------|---------|------------------------------------------------------------------------------------------------------------|
| `TheRecommendationsOfAVisibleCaseAreListedWithTheirEvidenceAsync`    | Service | A visible case lists its recommendations with evidence, state and model name.                              |
| `AcceptingRecordsTheAnalystAndCannotBeDoneTwiceAsync`                | Service | Accept stamps the analyst and date; a second decision conflicts.                                           |
| `OverridingNeedsAReasonAndStoresItAsync`                             | Service | Override requires a reason of at most 500 characters and stores it trimmed.                                |
| `ADecisionNeverChangesTheCaseAsync`                                  | Service | Accepting leaves the case status and closed state untouched.                                               |
| `PermissionTenantAndUnknownGuidAreRefusedAsync`                      | Service | No permission is forbidden, another tenant and an unknown guid are not found.                              |
| `AcceptingAndOverridingEachWriteATimelineEventWithTheOutcomeAsync`   | Service | Accept writes event 30 and override event 31 carrying the recommended outcome and the reason.              |
| `AnOverrideDrawsTheCaseAgainForQualityReviewButAnAcceptDoesNotAsync` | Service | An override re-draws the case for QA at the boosted rate and writes the sampled event; an accept does not. |
| `AWorkflowWithQaSamplingTurnedOffIsNeverBoostedAsync`                | Service | A workflow QA rate of zero stays zero after an override.                                                   |

### CaseQaReviewServiceTests

`Jube.Test.Service.Repository.CaseQaReview.CaseQaReviewServiceTests` — 5 tests. Category: Service.

| Test                                                            | Kind    | Verifies                                                                                   |
|-----------------------------------------------------------------|---------|--------------------------------------------------------------------------------------------|
| `AReviewerJudgesASampledClosedCaseAndItIsListedAsync`           | Service | A reviewer judges a sampled closed case; the trimmed comment and verdict are listed.       |
| `TheAgentRecommendationCanBeJudgedOnlyWhenThereWasOneAsync`     | Service | An agent verdict is refused without a recommendation and links the run when there is one.  |
| `OnlyAPickedClosedCaseNotClosedByTheReviewerCanBeReviewedAsync` | Service | A case not picked, not closed, or closed by the reviewer is refused and nothing is stored. |
| `BadVerdictsAndLongCommentsAreRefusedAsync`                     | Service | Verdicts outside 1 to 3, a comment over 1000 characters and a missing case are refused.    |
| `PermissionAndTenantAreEnforcedAsync`                           | Service | No permission is forbidden; another tenant is not found.                                   |

### CaseAnalystAssessmentServiceTests

`Jube.Test.Service.Repository.CaseAnalystAssessment.CaseAnalystAssessmentServiceTests` — 8 tests. Category: Service.

| Test                                                                | Kind    | Verifies                                                                                                              |
|---------------------------------------------------------------------|---------|-----------------------------------------------------------------------------------------------------------------------|
| `AnAnalystRecordsTheirViewBesideTheComputedLikelihoodAsync`         | Service | A view is stored trimmed with its author and the computed likelihood and sequence in force, and is listed as current. |
| `RecordingAViewChangesNothingAboutTheRankingOfTheCaseAsync`         | Service | Recording a view leaves the case row unchanged and writes no risk assessment.                                         |
| `ALaterViewBecomesCurrentAndWithdrawingItRestoresTheOneBeforeAsync` | Service | The latest view not withdrawn is current; withdrawing twice is harmless; withdrawing all leaves none.                 |
| `RecordingAndWithdrawingEachWriteOneCaseEventHoldingOnlyEnumerationIdsAsync`| Service | Each record and each real withdrawal writes one case event holding only likelihood ids; a repeat withdrawal writes none.|
| `OnlyTheAuthorCanWithdrawAViewAsync`                                | Service | Another analyst sees the view as not theirs and is refused when withdrawing it.                                       |
| `ALikelihoodOutsideOneToThreeOrAMissingReasonIsRefusedAsync`        | Theory  | A likelihood outside 1 to 3 or a blank reason is refused and nothing is stored.                                       |
| `AReasonOverTheLimitAndAMissingCaseAreRefusedAsync`                 | Service | A reason over 1000 characters is refused; a missing case or assessment is not found.                                  |
| `PermissionAndTenantAreEnforcedAsync`                               | Service | No permission is forbidden; another tenant is not found, for list, record and withdraw.                               |

### CaseAgentReplayServiceTests

`Jube.Test.Service.Repository.CaseAgentReplay.CaseAgentReplayServiceTests` — 4 tests. Category: Service.

| Test                                                          | Kind    | Verifies                                                                                                                |
|---------------------------------------------------------------|---------|-------------------------------------------------------------------------------------------------------------------------|
| `AStartedReplayIsPendingHashedByItsPromptAndListedAsync`      | Service | A started replay is pending, hashed by its prompt and listed.                                                           |
| `BadWindowsCountsCutOffsPromptsAndWorkflowsAreRefusedAsync`   | Service | Bad windows, counts, cut-offs, prompts and an unknown workflow are refused.                                             |
| `ComparingTwoReplaysCountsAgreementOverTheirCommonCasesAsync` | Service | A comparison counts common cases, same answers and each side agreement; metrics split missed risk from over-escalation. |
| `PermissionAndTenantAreEnforcedAsync`                         | Service | No permission is forbidden; another tenant gets not found and an empty list.                                            |

### EntityRiskRatingServiceTests

`Jube.Test.Service.Repository.EntityRiskRating.EntityRiskRatingServiceTests` — 11 tests. Category: Service.

| Test                                                                            | Kind    | Verifies                                                                                                              |
|---------------------------------------------------------------------------------|---------|-----------------------------------------------------------------------------------------------------------------------|
| `SettingARatingByHandAppendsAVersionAndKeepsTheHistoryAsync`                    | Service | Each manual rating is a new version, trimmed reason kept, history newest first.                                       |
| `ABadRatingIsRefusedAndMassAssignmentIsIgnoredAsync`                            | Service | Bad tier, missing or long key, missing reason and NaN score are refused; forged version, source and user are ignored. |
| `AnImportWritesChangesSkipsUnchangedAndReportsBadRowsAsync`                     | Service | An import writes new and changed ratings as Imported, skips unchanged, and reports bad rows by position.              |
| `AnImportOfMoreThanTheLimitIsRefusedAsync`                                      | Service | More than 500 ratings in one import is refused.                                                                       |
| `OnlyCurrentRatingsPastTheirReviewDateAreDueAsync`                              | Service | Only the current version of a key counts and only when its review date has passed.                                    |
| `ReadNeedsCasePermissionWriteNeedsWorkflowPermissionAndTenantsAreSeparateAsync` | Service | Read, list, set and import are forbidden without permission; another tenant sees nothing and cannot overwrite.        |
| `TheReviewJobNotifiesWorkflowHoldersOncePerDayWhenRatingsAreDueAsync`           | Service | Workflow permission holders get one notification a day with the due count.                                            |
| `AProposedChangeChangesNothingUntilASecondPersonApprovesItAsync`                | Service | A manual change is only a pending request; the rating changes when a second person approves.                          |
| `NobodyCanApproveOrRejectTheirOwnProposalAndOnlyApproversCanDecideAsync`        | Service | The proposer cannot decide their own proposal, only approvers can decide, and the proposer can retract.               |
| `RejectingNeedsAReasonAndOnlyTheProposerCanRetractAsync`                        | Service | Rejecting needs a reason, only the proposer can retract, and a decided request cannot be decided again.               |
| `ASecondPendingChangeForTheSameKeyAndAStaleApprovalAreRefusedAsync`             | Service | A second pending change for a key is refused and approval is refused when the rating changed since the proposal.      |

### CaseReportServiceTests

`Jube.Test.Service.Repository.CaseReport.CaseReportServiceTests` — 16 tests. Category: Service.

| Test                                                                                 | Kind    | Verifies                                                                                                        |
|--------------------------------------------------------------------------------------|---------|-----------------------------------------------------------------------------------------------------------------|
| `AReportIsStartedSavedValidatedAndItsXmlIsStoredWithItsHashAsync`                    | Service | A report is started, saved and validated; the XML is stored on the version with its SHA-256.                    |
| `AValidationProblemNamesTheFieldAndLeavesTheReportAsADraftAsync`                     | Service | A validation problem names the data path and leaves the report a draft with no XML.                             |
| `EditingAValidatedReportMakesANewVersionAndTheOldOneIsKeptAsync`                     | Service | Editing a validated report makes version 2 superseding version 1; both are kept.                                |
| `ASecondPersonApprovesNeverTheAuthorAndOnlyAPendingReportAsync`                      | Service | Only a pending report can be decided, only by a holder of the approve permission, and never by its author.      |
| `ARejectionNeedsAReasonAndReturnsTheReportToDraftAsync`                              | Service | A rejection needs a note and returns the report to draft.                                                       |
| `DownloadNeedsAnApprovedReportReturnsTheStoredBytesAndChecksTheirHashAsync`          | Service | Download needs an approved report, returns the stored bytes and refuses if their hash no longer matches.        |
| `SubmittingRecordsTheReferenceAndPutsTheCaseUnderLegalHoldAsync`                     | Service | Recording the filing stores the FIU reference and places the case under legal hold; it cannot be done twice.    |
| `FiuFeedbackAcknowledgesOrRejectsAndARejectedReportIsCorrectedAsANewVersionAsync`    | Service | FIU rejection is recorded against a field and the report is corrected as a new version carrying the old fields. |
| `AnAcknowledgementEndsTheReportAndBadFeedbackIsRefusedAsync`                         | Service | Feedback needs a submitted report and a valid kind; an acknowledgement ends the report.                         |
| `WithdrawingIsForTheAuthorOrAnApproverAndOnlyBeforeSubmissionAsync`                  | Service | The author or an approver may withdraw, and only before submission.                                             |
| `ReportsAreVisibleOnlyToHoldersOfThePermissionsAndOnlyOnVisibleCasesAndTenantsAsync` | Service | Without a report permission every call is forbidden; another tenant gets not found.                             |
| `OnlyAnApproverSeesTheAwaitingListAndOnlyForCasesTheyCanOpenAsync`                   | Service | Only an approver can list reports awaiting approval.                                                            |
| `ApprovalAndDecisionNotifyTheRightPeopleOncePerStepAsync`                            | Service | Requesting approval notifies approvers other than the requester; a decision notifies the preparer.              |
| `AProfileIsImportedAsAnAppendOnlyVersionAndABadOneIsRefusedWithItsIssuesAsync`       | Service | A profile import appends a version; a bad template or schema is refused with its issues.                        |
| `ProfilesAreImportedOnlyByApproversAndStayInTheirTenantAsync`                        | Service | Only approvers import or read a profile; another tenant sees none.                                              |
| `BadStartsAndBadFieldsAreRefusedAsync`                                               | Service | Bad case, profile, report code and malformed or oversized fields are refused.                                   |

### CaseReportCloseGateTests

`Jube.Test.Service.Repository.CaseReport.CaseReportCloseGateTests` — 2 tests. Category: Service.

| Test                                                               | Kind    | Verifies                                                                                     |
|--------------------------------------------------------------------|---------|----------------------------------------------------------------------------------------------|
| `AnAwaitingReportBlocksClosingAndOnlyReportHoldersAreToldWhyAsync` | Service | A report awaiting approval blocks closing; only holders of a report permission are told why. |
| `AnApproverMayCloseAndASubmittedReportNoLongerBlocksAnyoneAsync`   | Service | An approver may close anyway and a submitted report blocks no one.                           |

### ErasureServiceTests

`Jube.Test.Service.Repository.Erasure.ErasureServiceTests` — 16 tests. Category: Service.

| Test                                                                 | Kind    | Verifies                                                                                                                                                    |
|----------------------------------------------------------------------|---------|-------------------------------------------------------------------------------------------------------------------------------------------------------------|
| `PreviewCountsTheSubjectsTransactionsAndSeesNoObstacleAsync`         | Service | Preview counts only the subject's archived transactions and reports no obstacle.                                                                            |
| `ApprovedErasureRemovesEverythingAboutTheSubjectAndKeepsOthersAsync` | Service | An approved erasure removes the archive rows, keys, tags, graph node and edge, clears the closed case payload, keeps other subjects and records the counts. |
| `ANumericKeyIsMatchedThroughItsIntegerValueAsync`                    | Service | A numeric key is matched through its integer value.                                                                                                         |
| `ARequestIsRefusedWhileACaseIsUnderLegalHoldAsync`                   | Service | A request is refused, with the legal hold reason, while a case is under legal hold, and nothing is removed.                                                 |
| `ARequestIsRefusedWhileACaseIsOpenOrSuspendedAsync`                  | Service | Theory over four case states: open, suspended and similar cases refuse the request with the open case reason.                                               |
| `ARequestIsRefusedInsideTheRetentionMinimumAsync`                    | Service | A request is refused inside the retention minimum and proceeds outside it.                                                                                  |
| `ASubjectWithNothingArchivedIsRefusedAsync`                          | Service | A subject with nothing archived is refused with the nothing found reason.                                                                                   |
| `ALegalHoldPlacedAfterApprovalStopsTheEngineAndIsRecordedAsync`      | Service | A hold placed after approval makes the engine refuse at run time, keep the data and record the reason.                                                      |
| `TheRequesterCannotApproveOrRejectTheirOwnRequestAsync`              | Service | Four eyes: the requester can neither approve nor reject their own request.                                                                                  |
| `OnlyAnApproverCanDecideAndRejectingNeedsAReasonAsync`               | Service | Only an approver can decide, rejecting needs a reason and a decided request cannot be decided again.                                                        |
| `OnlyTheRequesterCanRetractAndASecondOpenRequestIsRefusedAsync`      | Service | Only the requester can retract, and a second open request for the same subject is refused.                                                                  |
| `ExecutedRequestsAreNeverRunTwiceAsync`                              | Service | An executed request is not run again.                                                                                                                       |
| `ValidationRefusesBlankAndOversizedInputAsync`                       | Service | Blank and oversized key names, values and reasons are refused.                                                                                              |
| `EveryVerbRefusesAUserWithoutThePermissionAsync`                     | Service | List, preview, export and request refuse a user without the permission.                                                                                     |
| `AnotherTenantCannotSeePreviewOrExportTheSubjectAsync`               | Service | Another tenant sees nothing of the subject in preview or export.                                                                                            |
| `ExportReturnsOnlyTheSubjectsArchivedTransactionsAsync`              | Service | Export returns only the subject's transactions and no case data.                                                                                            |
