---
layout: default
title: Security Access Isolation
nav_order: 20
parent: Testing
---

# Security Access Isolation

Tests that prove tenant, role and user isolation across the service and query surface, so one tenant can never read or
mutate another tenant's data.

## Advanced analytics

### PenTestAdvancedAnalyticsChart

`Jube.Test.Security.AdvancedAnalytics.PenTestAdvancedAnalyticsChart` — 6 tests. Category: PenTest. Collection: Database.

| Test                                                                                      | Kind | Verifies                                                                                           |
|-------------------------------------------------------------------------------------------|------|----------------------------------------------------------------------------------------------------|
| `EveryChartRoute_RefusesEveryoneButAHolderOfPermissionFortyEightAsync`                    | Fact | Every chart route refuses everyone but a holder of permission forty eight.                         |
| `OwnCharts_AreSafeWellFormedExternalStyleSvgWithTheSameHeadersAsTheExhaustiveChartsAsync` | Fact | Own charts are safe well formed external style svg with the same headers as the exhaustive charts. |
| `CrossTenant_ForeignAndMissingAndDeletedVersionsAreIdenticalEmptyStatesAsync`             | Fact | Cross tenant foreign and missing and deleted versions are identical empty states.                  |
| `HostileModelNamesAreEscapedInsideTheDiagramAsync`                                        | Fact | Hostile model names are escaped inside the diagram.                                                |
| `VerbTamperingAndRouteAbuse_NeverReachTheRendererOrServerErrorAsync`                      | Fact | Verb tampering and route abuse never reach the renderer or server error.                           |
| `ConcurrentRenders_AreDeterministicAndBoundedAsync`                                       | Fact | Concurrent renders are deterministic and bounded.                                                  |

### PenTestAskJoobyAnalyticsInstance

`Jube.Test.Security.AdvancedAnalytics.PenTestAskJoobyAnalyticsInstance` — 8 tests. Category: PenTest. Collection:
Database.

| Test                                                                                        | Kind | Verifies                                                                                        |
|---------------------------------------------------------------------------------------------|------|-------------------------------------------------------------------------------------------------|
| `EveryRoute_RefusesUnauthenticatedAndUnpermittedCallersAsync`                               | Fact | Every route refuses unauthenticated and unpermitted callers.                                    |
| `Delete_PermissionFortyEightDeletesUnallocated_AnAllocatedInstanceAlsoNeedsFortySevenAsync` | Fact | Delete permission forty eight deletes unallocated an allocated instance also needs forty seven. |
| `CrossTenant_ForeignIdsBehaveExactlyLikeMissingIdsAndNothingChangesAsync`                   | Fact | Cross tenant foreign ids behave exactly like missing ids and nothing changes.                   |
| `NoMutatingRouteExistsBesideDelete_AndWrongVerbsNeverReachAHandlerAsync`                    | Fact | No mutating route exists beside delete and wrong verbs never reach a handler.                   |
| `Delete_IgnoresBodiesQueryStringsAndHeadersAndDeletesExactlyTheRouteIdAsync`                | Fact | Delete ignores bodies query strings and headers and deletes exactly the route id.               |
| `RouteAndQueryParameters_AreRefusedWithoutServerErrorsOrSideEffectsAsync`                   | Fact | Route and query parameters are refused without server errors or side effects.                   |
| `HostileStoredNames_AreReturnedAsJsonDataNeverAsMarkupAsync`                                | Fact | Hostile stored names are returned as json data never as markup.                                 |
| `Responses_AreWellFormedJsonWithoutTheStackOrServerHeadersAsync`                            | Fact | Responses are well formed json without the stack or server headers.                             |

### PenTestAskJoobyAnalyticsInstanceActive

`Jube.Test.Security.AdvancedAnalytics.PenTestAskJoobyAnalyticsInstanceActive` — 8 tests. Category: PenTest. Collection:
Database.

| Test                                                                            | Kind | Verifies                                                                            |
|---------------------------------------------------------------------------------|------|-------------------------------------------------------------------------------------|
| `Put_RefusesUnauthenticatedAndUnpermittedCallersAndOnlyFortySevenSwitchesAsync` | Fact | Put refuses unauthenticated and unpermitted callers and only forty seven switches.  |
| `Put_AnUnallocatedInstanceCannotBeSwitchedOnAndSaysWhyWithoutInternalsAsync`    | Fact | Put an unallocated instance cannot be switched on and says why without internals.   |
| `Put_CrossTenantForeignIdsBehaveExactlyLikeMissingIdsAndNothingChangesAsync`    | Fact | Put cross tenant foreign ids behave exactly like missing ids and nothing changes.   |
| `Put_MassAssignmentChangesOnlyTheSwitchAndNeverAnythingTheBodyAlsoNamesAsync`   | Fact | Put mass assignment changes only the switch and never anything the body also names. |
| `Put_WrongVerbsNeverSwitchAndOverrideHeadersAreIgnoredAsync`                    | Fact | Put wrong verbs never switch and override headers are ignored.                      |
| `Put_MalformedBodiesAreRefusedWithoutServerErrorsOrSideEffectsAsync`            | Fact | Put malformed bodies are refused without server errors or side effects.             |
| `Put_RouteAbuseIsRefusedWithoutServerErrorsAsync`                               | Fact | Put route abuse is refused without server errors.                                   |
| `Put_ConcurrentSwitchesLeaveAConsistentRowAndNoServerErrorsAsync`               | Fact | Put concurrent switches leave a consistent row and no server errors.                |

### PenTestAskJoobyAnalyticsModel

`Jube.Test.Security.AdvancedAnalytics.PenTestAskJoobyAnalyticsModel` — 7 tests. Category: PenTest. Collection: Database.

| Test                                                                     | Kind | Verifies                                                                     |
|--------------------------------------------------------------------------|------|------------------------------------------------------------------------------|
| `EveryReadRoute_RefusesEveryoneButAHolderOfPermissionFortyEightAsync`    | Fact | Every read route refuses everyone but a holder of permission forty eight.    |
| `NoMutatingRouteExists_PromotionAndRollbackAreNotReachableOverHttpAsync` | Fact | No mutating route exists promotion and rollback are not reachable over http. |
| `CrossTenant_ForeignIdsBehaveExactlyLikeMissingIdsOnEveryReadAsync`      | Fact | Cross tenant foreign ids behave exactly like missing ids on every read.      |
| `Payloads_NeverCarryTheModelJsonTheTenantColumnOrNonFiniteNumbersAsync`  | Fact | Payloads never carry the model json the tenant column or non finite numbers. |
| `Compare_RefusesNonsenseAndNeverMixesTenantsAsync`                       | Fact | Compare refuses nonsense and never mixes tenants.                            |
| `RouteAndQueryParameters_AreRefusedWithoutServerErrorsAsync`             | Fact | Route and query parameters are refused without server errors.                |
| `ConcurrentReads_AreDeterministicAndBoundedAsync`                        | Fact | Concurrent reads are deterministic and bounded.                              |

## Case

### PenTestActivationWatcher

`Jube.Test.Security.Case.PenTestActivationWatcher` — 6 tests. Category: PenTest. Collection: Database.

| Test                                                             | Kind | Verifies                                                           |
|------------------------------------------------------------------|------|--------------------------------------------------------------------|
| `Matrix_EveryPrincipal_GetsExactStatusAndOnlyOwnTenantRowsAsync` | Fact | Matrix every principal gets exact status and only own tenant rows. |
| `ForeignTenantParameters_AreIgnoredAsync`                        | Fact | Foreign tenant parameters are ignored.                             |
| `DateFilters_InjectionAndTypeConfusion_FailSafelyAsync`          | Fact | Date filters injection and type confusion fail safely.             |
| `DateRange_Semantics_AreStableAsync`                             | Fact | Date range semantics are stable.                                   |
| `OtherVerbs_AreNotAcceptedAsync`                                 | Fact | Other verbs are not accepted.                                      |
| `ParallelReplays_AreConsistentAsync`                             | Fact | Parallel replays are consistent.                                   |

### PenTestArchive

`Jube.Test.Security.Case.PenTestArchive` — 9 tests. Category: PenTest. Collection: Database.

| Test                                                             | Kind | Verifies                                                          |
|------------------------------------------------------------------|------|-------------------------------------------------------------------|
| `Matrix_EveryPrincipal_GetsExactStatusAsync`                     | Fact | Matrix every principal gets exact status.                         |
| `OtherTenantsArchive_BehavesLikeMissing_AndIsNeverModifiedAsync` | Fact | Other tenants archive behaves like missing and is never modified. |
| `MassAssignment_ExtraProperties_AreIgnoredAsync`                 | Fact | Mass assignment extra properties are ignored.                     |
| `TagValues_Injection_IsStoredAsDataOrRejectedAsync`              | Fact | Tag values injection is stored as data or rejected.               |
| `NulAndControlCharacters_AreRejectedNotErrorsAsync`              | Fact | Nul and control characters are rejected not errors.               |
| `Boundaries_AndTypeConfusion_FailSafelyAsync`                    | Fact | Boundaries and type confusion fail safely.                        |
| `HugeTagArray_IsRefusedBeforeItCanConsumeTheDatabaseAsync`       | Fact | Huge tag array is refused before it can consume the database.     |
| `ReplayAndParallelUpdates_ConvergeOnASingleConsistentSetAsync`   | Fact | Replay and parallel updates converge on a single consistent set.  |
| `OtherVerbs_AreNotAcceptedAsync`                                 | Fact | Other verbs are not accepted.                                     |

### PenTestCase

`Jube.Test.Security.Case.PenTestCase` — 15 tests. Category: PenTest. Collection: Database.

| Test                                                                           | Kind | Verifies                                                                         |
|--------------------------------------------------------------------------------|------|----------------------------------------------------------------------------------|
| `Matrix_List_GetById_Put_EveryPrincipalAsync`                                  | Fact | Matrix list get by id put every principal.                                       |
| `List_NeverExposesCasesOutsideTheCallersTenantOrRolesAsync`                    | Fact | List never exposes cases outside the callers tenant or roles.                    |
| `GetById_ForeignGatedHiddenAndMissing_AreIndistinguishableAsync`               | Fact | Get by id foreign gated hidden and missing are indistinguishable.                |
| `RouteParameter_Injection_AndTypeConfusion_NeverErrorsAsync`                   | Fact | Route parameter injection and type confusion never errors.                       |
| `Update_ForeignGatedAndHiddenCases_AreNotFoundAndNeverChangedAsync`            | Fact | Update foreign gated and hidden cases are not found and never changed.           |
| `Update_MassAssignment_ServerOwnedFieldsAreNeverHonouredAsync`                 | Fact | Update mass assignment server owned fields are never honoured.                   |
| `Update_StatusTransitions_OnlyToVisibleStatusesOfTheSameWorkflowAsync`         | Fact | Update status transitions only to visible statuses of the same workflow.         |
| `Lock_CannotBeStolenOrForgedAcrossUsersAndTenantsAsync`                        | Fact | Lock cannot be stolen or forged across users and tenants.                        |
| `Update_FieldBoundaries_AndTypeConfusion_FailSafelyAsync`                      | Fact | Update field boundaries and type confusion fail safely.                          |
| `Update_StringFields_Injection_IsRefusedOrStoredAsDataAsync`                   | Fact | Update string fields injection is refused or stored as data.                     |
| `Update_Replay_IsIdempotent_AndConcurrentUpdatesStayConsistentAsync`           | Fact | Update replay is idempotent and concurrent updates stay consistent.              |
| `ParentDeleted_CaseBecomesUnreachableAsync`                                    | Fact | Parent deleted case becomes unreachable.                                         |
| `Insert_ForeignAndInvalidStatuses_AreRefused_AndServerOwnedFieldsIgnoredAsync` | Fact | Insert foreign and invalid statuses are refused and server owned fields ignored. |
| `CreateFromCaseKeyValue_Matrix_Injection_AndForeignReferencesAsync`            | Fact | Create from case key value matrix injection and foreign references.              |
| `Verbs_AndOverrides_AreNotAcceptedAsync`                                       | Fact | Verbs and overrides are not accepted.                                            |

### PenTestCaseFile

`Jube.Test.Security.Case.PenTestCaseFile` — 15 tests. Category: PenTest. Collection: Database.

| Test                                                                                    | Kind | Verifies                                                                                      |
|-----------------------------------------------------------------------------------------|------|-----------------------------------------------------------------------------------------------|
| `Matrix_Download_EveryPrincipalAsync`                                                   | Fact | Matrix download every principal.                                                              |
| `Matrix_Upload_EveryPrincipalAsync`                                                     | Fact | Matrix upload every principal.                                                                |
| `Matrix_Remove_EveryPrincipalAsync`                                                     | Fact | Matrix remove every principal.                                                                |
| `Matrix_ListByKey_EveryPrincipalAsync`                                                  | Fact | Matrix list by key every principal.                                                           |
| `Download_ForeignGatedHiddenAndMissingIds_AreIndistinguishableAsync`                    | Fact | Download foreign gated hidden and missing ids are indistinguishable.                          |
| `Download_IdParameter_InjectionAndTypeConfusionAsync`                                   | Fact | Download id parameter injection and type confusion.                                           |
| `Upload_ToForeignGatedHiddenOrMissingCases_IsRefusedAndStoresNothingAsync`              | Fact | Upload to foreign gated hidden or missing cases is refused and stores nothing.                |
| `Upload_KeyMismatch_CannotPlantFilesOnAnotherCasesKeyAsync`                             | Fact | Upload key mismatch cannot plant files on another cases key.                                  |
| `Upload_FileNames_AreSanitisedAndNeverCarryMarkupOrPathsAsync`                          | Fact | Upload file names are sanitised and never carry markup or paths.                              |
| `Download_SpoofedContentTypes_CanNeverRenderAsActiveContentOfTheApplicationOriginAsync` | Fact | Download spoofed content types can never render as active content of the application origin.  |
| `Upload_SizeAndShape_BoundariesAsync`                                                   | Fact | Upload size and shape boundaries.                                                             |
| `Remove_Replay_ForeignIds_AndTypeConfusionAsync`                                        | Fact | Remove replay foreign ids and type confusion.                                                 |
| `ListByKey_Injection_MissingParameters_AndCrossKeyProbingAsync`                         | Fact | List by key injection missing parameters and cross key probing.                               |
| `OtherVerbs_AreNotAcceptedAsync`                                                        | Fact | Other verbs are not accepted.                                                                 |
| `Upload_ExecutablesScriptsHtmlAndSvg_AreRefusedByNameAndByContentAndStoreNothingAsync`  | Fact | Upload executables scripts html and svg are refused by name and by content and store nothing. |

### PenTestCaseNote

`Jube.Test.Security.Case.PenTestCaseNote` — 12 tests. Category: PenTest. Collection: Database.

| Test                                                                               | Kind | Verifies                                                                             |
|------------------------------------------------------------------------------------|------|--------------------------------------------------------------------------------------|
| `Matrix_Insert_And_ListByKey_EveryPrincipalAsync`                                  | Fact | Matrix insert and list by key every principal.                                       |
| `Insert_ForeignGatedHiddenMissingCases_AreRefusedIdentically_AndStoreNothingAsync` | Fact | Insert foreign gated hidden missing cases are refused identically and store nothing. |
| `Insert_ActionsOfOtherTenantsRolesOrWorkflows_AreRefusedAsync`                     | Fact | Insert actions of other tenants roles or workflows are refused.                      |
| `Insert_KeySmuggling_NoteAlwaysCarriesTheCasesOwnKeyAsync`                         | Fact | Insert key smuggling note always carries the cases own key.                          |
| `Insert_StoredXss_NoteIsNeverReturnedAsActiveMarkupAsync`                          | Fact | Insert stored xss note is never returned as active markup.                           |
| `Insert_LegitimateNotes_RoundTripFaithfullyAsync`                                  | Fact | Insert legitimate notes round trip faithfully.                                       |
| `Insert_MassAssignment_ServerOwnedFieldsAreNotHonouredAsync`                       | Fact | Insert mass assignment server owned fields are not honoured.                         |
| `Insert_LengthLimit_AndOversizeBodiesAsync`                                        | Fact | Insert length limit and oversize bodies.                                             |
| `Insert_Boundaries_TypeConfusion_AndInjection_FailSafelyAsync`                     | Fact | Insert boundaries type confusion and injection fail safely.                          |
| `ListByKey_GatedHiddenAndForeignKeys_ReturnNothing_AndInputsAreSafeAsync`          | Fact | List by key gated hidden and foreign keys return nothing and inputs are safe.        |
| `Notes_AreAppendOnly_AndOtherVerbsAreRejectedAsync`                                | Fact | Notes are append only and other verbs are rejected.                                  |
| `Insert_WithoutAnAntiforgeryTokenIsRefusedAndStoresNothingAsync`                   | Fact | Insert without an antiforgery token is refused and stores nothing.                   |

### PenTestCaseNoteNarrative

`Jube.Test.Security.Case.PenTestCaseNoteNarrative` — 12 tests. Category: PenTest. Collection: Database.

| Test                                                                                           | Kind | Verifies                                                                                              |
|------------------------------------------------------------------------------------------------|------|-------------------------------------------------------------------------------------------------------|
| `Finding_Matrix_EveryPrincipalAsync`                                                           | Fact | Finding matrix every principal.                                                                       |
| `Finding_ForeignGatedHiddenSystemAndAskJoobyNotes_BehaveLikeMissingNotesAndChangeNothingAsync` | Fact | Finding foreign gated hidden system and ask jooby notes behave like missing notes and change nothing. |
| `Finding_HostileBodies_AreRefusedCleanlyAsync`                                                 | Fact | Finding hostile bodies are refused cleanly.                                                           |
| `Finding_TamperingWithOtherFields_ChangesNothingElseAsync`                                     | Fact | Finding tampering with other fields changes nothing else.                                             |
| `Insert_ForgedSourceKindStateAndVersion_AreIgnoredAsync`                                       | Fact | Insert forged source kind state and version are ignored.                                              |
| `Insert_OverHttp_NeverYieldsAnAskJoobyNoteWhateverTheRequestClaimsAsync`                       | Fact | Insert over http never yields an ask jooby note whatever the request claims.                          |
| `List_ReturnsAskJoobyNotesAsInertDataAndNeverAnotherTenantsAsync`                              | Fact | List returns ask jooby notes as inert data and never another tenants.                                 |
| `Upload_ToANote_Matrix_And_OwnershipAsync`                                                     | Fact | Upload to a note matrix and ownership.                                                                |
| `Upload_HostileNoteIds_AreRefusedCleanlyAsync`                                                 | Fact | Upload hostile note ids are refused cleanly.                                                          |
| `Upload_WithoutANote_CreatesOneNoteThatNamesTheDocumentAndHostileNamesStayDataAsync`           | Fact | Upload without a note creates one note that names the document and hostile names stay data.           |
| `Download_OfAFileOnAnotherTenantsNote_BehavesLikeAMissingFileAsync`                            | Fact | Download of a file on another tenants note behaves like a missing file.                               |
| `NarrativeVersionChain_UnderParallelWriters_NeverForksAsync`                                   | Fact | Narrative version chain under parallel writers never forks.                                           |

### PenTestCaseSuppression

`Jube.Test.Security.Case.PenTestCaseSuppression` — 11 tests. Category: PenTest. Collection: Database.

| Test                                                                                | Kind | Verifies                                                                                |
|-------------------------------------------------------------------------------------|------|-----------------------------------------------------------------------------------------|
| `Request_Matrix_EveryPrincipalAsync`                                                | Fact | Request matrix every principal.                                                         |
| `Request_ForeignGatedHiddenAndMissingCases_BehaveIdenticallyAndStoreNothingAsync`   | Fact | Request foreign gated hidden and missing cases behave identically and store nothing.    |
| `Request_ForgedStateVersionMakerSourceAndApprovalsAreIgnoredAsync`                  | Fact | Request forged state version maker source and approvals are ignored.                    |
| `Request_ATargetOfAnotherTenantsModelOrANoteWithScript_NeverLandsAsync`             | Fact | Request a target of another tenants model or a note with script never lands.            |
| `Request_HostileKeysValuesReasonsAndEvidence_AreRefusedAndStoreNothingAsync`        | Fact | Request hostile keys values reasons and evidence are refused and store nothing.         |
| `Request_HostileBodies_AreRefusedCleanlyAsync`                                      | Fact | Request hostile bodies are refused cleanly.                                             |
| `Decide_UsersWithoutTheApprovalPermissionTheMakerAndOtherTenants_CannotDecideAsync` | Fact | Decide users without the approval permission the maker and other tenants cannot decide. |
| `Read_IsRoleTenantAndPermissionScopedAsync`                                         | Fact | Read is role tenant and permission scoped.                                              |
| `Candidates_AreCaseVisibilityScopedAndHostileInputIsRefusedAsync`                   | Fact | Candidates are case visibility scoped and hostile input is refused.                     |
| `Retract_ParallelAndReplayedDecisionsRecordExactlyOneOutcomeAsync`                  | Fact | Retract parallel and replayed decisions record exactly one outcome.                     |
| `Request_WithoutAnAntiforgeryTokenIsRefusedAsync`                                   | Fact | Request without an antiforgery token is refused.                                        |

### PenTestCaseResearch

`Jube.Test.Security.Case.PenTestCaseResearch` — 8 tests. Category: PenTest. Collection: Database.

| Test                                                                             | Kind | Verifies                                                                             |
|----------------------------------------------------------------------------------|------|--------------------------------------------------------------------------------------|
| `Create_Matrix_EveryPrincipalAsync`                                              | Fact | Create matrix every principal.                                                       |
| `Create_ForeignGatedHiddenAndMissingCases_BehaveIdenticallyAndStoreNothingAsync` | Fact | Create foreign gated hidden and missing cases behave identically and store nothing.  |
| `Create_ForgedSourceKeyAuthorAndIdAreIgnoredAsync`                               | Fact | Create forged source key author and id are ignored.                                  |
| `Create_HostileAddressesTitlesCountsAndTopics_AreRefusedAndStoreNothingAsync`    | Fact | Create hostile addresses titles counts and topics are refused and store nothing.     |
| `Create_HostileBodies_AreRefusedCleanlyAsync`                                    | Fact | Create hostile bodies are refused cleanly.                                           |
| `List_IsTenantAndRoleScopedAndStoredMarkupNeverComesBackActiveAsync`             | Fact | List is tenant and role scoped and stored markup never comes back active.            |
| `Files_OnResearch_OwnerOnly_ForeignAskJoobyBothAndMissingParentsAreRefusedAsync` | Fact | Files on research owner only foreign ask jooby both and missing parents are refused. |
| `Files_DownloadAcrossParentKinds_IsTenantScopedAndListedWithTheResearchAsync`    | Fact | Files download across parent kinds is tenant scoped and listed with the research.    |

### PenTestCaseWorkflow

`Jube.Test.Security.Case.PenTestCaseWorkflow` — 11 tests. Category: PenTest. Collection: Database.

| Test                                                              | Kind | Verifies                                                           |
|-------------------------------------------------------------------|------|--------------------------------------------------------------------|
| `Matrix_ReadRoutes_EveryPrincipalAsync`                           | Fact | Matrix read routes every principal.                                |
| `Matrix_WriteRoutes_EveryPrincipalAsync`                          | Fact | Matrix write routes every principal.                               |
| `ForeignAndGated_ReadsBehaveLikeMissingAsync`                     | Fact | Foreign and gated reads behave like missing.                       |
| `Writes_ToForeignRowsAndParents_AreRefused_AndChangeNothingAsync` | Fact | Writes to foreign rows and parents are refused and change nothing. |
| `VisualisationReference_MustBelongToTheCallersTenantAsync`        | Fact | Visualisation reference must belong to the callers tenant.         |
| `MassAssignment_ServerOwnedFieldsAreNeverHonouredAsync`           | Fact | Mass assignment server owned fields are never honoured.            |
| `Fields_Injection_Limits_AndTypeConfusion_FailSafelyAsync`        | Fact | Fields injection limits and type confusion fail safely.            |
| `RouteParameters_Injection_AndTypeConfusion_NeverErrorAsync`      | Fact | Route parameters injection and type confusion never error.         |
| `Lifecycle_DuplicateNames_SoftDelete_Locked_AndConcurrencyAsync`  | Fact | Lifecycle duplicate names soft delete locked and concurrency.      |
| `ConcurrentCreates_OfTheSameName_ProduceOneRowAsync`              | Fact | Concurrent creates of the same name produce one row.               |
| `Verbs_AndOverrides_AreNotAcceptedAsync`                          | Fact | Verbs and overrides are not accepted.                              |

### PenTestCaseWorkflowAction

`Jube.Test.Security.Case.PenTestCaseWorkflowAction` — 12 tests. Category: PenTest. Collection: Database.
| `Verbs_AndOverrides_AreNotAcceptedAsync`                            | Fact | Verbs and overrides are not accepted. |
| `ConcurrentCreates_OfTheSameName_ProduceOneRowAsync`                | Fact | Concurrent creates of the same name
produce one row. |
| `SoftDelete_Replay_LockedRows_AndConcurrentDuplicatesAsync`         | Fact | Soft delete replay locked rows and
concurrent duplicates. |
| `RouteParameters_Injection_AndTypeConfusion_NeverErrorAsync`        | Fact | Route parameters injection and type
confusion never error. |
| `Boundaries_TypeConfusion_AndMalformedBodies_FailSafelyAsync`       | Fact | Boundaries type confusion and malformed
bodies fail safely. |
| `TextFields_Limits_AndOversizeBodies_AreEnforcedWithoutErrorsAsync` | Fact | Text fields limits and oversize bodies
are enforced without errors. |
| `TextFields_Injection_IsStoredAsDataOrRefusedAsync`                 | Fact | Text fields injection is stored as data
or refused. |
| `MassAssignment_ServerOwnedFieldsAreNeverHonouredAsync`             | Fact | Mass assignment server owned fields are
never honoured. |
| `Writes_ToForeignRowsAndParents_AreRefused_AndChangeNothingAsync`   | Fact | Writes to foreign rows and parents are
refused and change nothing. |
| `ForeignAndGatedIds_BehaveLikeMissing_OnEveryReadRouteAsync`        | Fact | Foreign and gated ids behave like missing
on every read route. |
| `Matrix_WriteRoutes_EveryPrincipalAsync`                            | Fact | Matrix write routes every principal. |
| `Matrix_ReadRoutes_EveryPrincipalAsync`                             | Fact | Matrix read routes every principal. |

| Test | Kind | Verifies |
|------|------|----------|

### PenTestCaseWorkflowActionRole

`Jube.Test.Security.Case.PenTestCaseWorkflowActionRole` — 9 tests. Category: PenTest. Collection: Database.
| `Verbs_AndOverrides_AreNotAcceptedAsync`                           | Fact | Verbs and overrides are not accepted. |
| `RouteParameters_Injection_AndTypeConfusion_NeverErrorAsync`       | Fact | Route parameters injection and type
confusion never error. |
| `Boundaries_TypeConfusion_AndInjection_FailSafelyAsync`            | Fact | Boundaries type confusion and injection
fail safely. |
| `MassAssignment_ServerOwnedFieldsAreNeverHonouredAsync`            | Fact | Mass assignment server owned fields are
never honoured. |
| `DuplicateAndConcurrentGrants_DoNotMultiplyAsync`                  | Fact | Duplicate and concurrent grants do not
multiply. |
| `Delete_ForeignIds_AreRefused_ReplayIsRefused_AndRowsSurviveAsync` | Fact | Delete foreign ids are refused replay is
refused and rows survive. |
| `List_ForForeignAndMissingParents_IsIndistinguishableAsync`        | Fact | List for foreign and missing parents is
indistinguishable. |
| `Create_WithForeignReferences_IsRefused_AndChangesNothingAsync`    | Fact | Create with foreign references is refused
and changes nothing. |
| `Matrix_EveryPrincipal_OnEveryRouteAsync`                          | Fact | Matrix every principal on every route. |

| Test | Kind | Verifies |
|------|------|----------|

### PenTestCaseWorkflowDisplay

`Jube.Test.Security.Case.PenTestCaseWorkflowDisplay` — 12 tests. Category: PenTest. Collection: Database.
| `Verbs_AndOverrides_AreNotAcceptedAsync`                            | Fact | Verbs and overrides are not accepted. |
| `ConcurrentCreates_OfTheSameName_ProduceOneRowAsync`                | Fact | Concurrent creates of the same name
produce one row. |
| `SoftDelete_Replay_LockedRows_AndConcurrentDuplicatesAsync`         | Fact | Soft delete replay locked rows and
concurrent duplicates. |
| `RouteParameters_Injection_AndTypeConfusion_NeverErrorAsync`        | Fact | Route parameters injection and type
confusion never error. |
| `Boundaries_TypeConfusion_AndMalformedBodies_FailSafelyAsync`       | Fact | Boundaries type confusion and malformed
bodies fail safely. |
| `TextFields_Limits_AndOversizeBodies_AreEnforcedWithoutErrorsAsync` | Fact | Text fields limits and oversize bodies
are enforced without errors. |
| `TextFields_Injection_IsStoredAsDataOrRefusedAsync`                 | Fact | Text fields injection is stored as data
or refused. |
| `MassAssignment_ServerOwnedFieldsAreNeverHonouredAsync`             | Fact | Mass assignment server owned fields are
never honoured. |
| `Writes_ToForeignRowsAndParents_AreRefused_AndChangeNothingAsync`   | Fact | Writes to foreign rows and parents are
refused and change nothing. |
| `ForeignAndGatedIds_BehaveLikeMissing_OnEveryReadRouteAsync`        | Fact | Foreign and gated ids behave like missing
on every read route. |
| `Matrix_WriteRoutes_EveryPrincipalAsync`                            | Fact | Matrix write routes every principal. |
| `Matrix_ReadRoutes_EveryPrincipalAsync`                             | Fact | Matrix read routes every principal. |

| Test | Kind | Verifies |
|------|------|----------|

### PenTestCaseWorkflowDisplayRole

`Jube.Test.Security.Case.PenTestCaseWorkflowDisplayRole` — 9 tests. Category: PenTest. Collection: Database.
| `Verbs_AndOverrides_AreNotAcceptedAsync`                           | Fact | Verbs and overrides are not accepted. |
| `RouteParameters_Injection_AndTypeConfusion_NeverErrorAsync`       | Fact | Route parameters injection and type
confusion never error. |
| `Boundaries_TypeConfusion_AndInjection_FailSafelyAsync`            | Fact | Boundaries type confusion and injection
fail safely. |
| `MassAssignment_ServerOwnedFieldsAreNeverHonouredAsync`            | Fact | Mass assignment server owned fields are
never honoured. |
| `DuplicateAndConcurrentGrants_DoNotMultiplyAsync`                  | Fact | Duplicate and concurrent grants do not
multiply. |
| `Delete_ForeignIds_AreRefused_ReplayIsRefused_AndRowsSurviveAsync` | Fact | Delete foreign ids are refused replay is
refused and rows survive. |
| `List_ForForeignAndMissingParents_IsIndistinguishableAsync`        | Fact | List for foreign and missing parents is
indistinguishable. |
| `Create_WithForeignReferences_IsRefused_AndChangesNothingAsync`    | Fact | Create with foreign references is refused
and changes nothing. |
| `Matrix_EveryPrincipal_OnEveryRouteAsync`                          | Fact | Matrix every principal on every route. |

| Test | Kind | Verifies |
|------|------|----------|

### PenTestCaseWorkflowFilter

`Jube.Test.Security.Case.PenTestCaseWorkflowFilter` — 12 tests. Category: PenTest. Collection: Database.
| `Verbs_AndOverrides_AreNotAcceptedAsync`                            | Fact | Verbs and overrides are not accepted. |
| `ConcurrentCreates_OfTheSameName_ProduceOneRowAsync`                | Fact | Concurrent creates of the same name
produce one row. |
| `SoftDelete_Replay_LockedRows_AndConcurrentDuplicatesAsync`         | Fact | Soft delete replay locked rows and
concurrent duplicates. |
| `RouteParameters_Injection_AndTypeConfusion_NeverErrorAsync`        | Fact | Route parameters injection and type
confusion never error. |
| `Boundaries_TypeConfusion_AndMalformedBodies_FailSafelyAsync`       | Fact | Boundaries type confusion and malformed
bodies fail safely. |
| `TextFields_Limits_AndOversizeBodies_AreEnforcedWithoutErrorsAsync` | Fact | Text fields limits and oversize bodies
are enforced without errors. |
| `TextFields_Injection_IsStoredAsDataOrRefusedAsync`                 | Fact | Text fields injection is stored as data
or refused. |
| `MassAssignment_ServerOwnedFieldsAreNeverHonouredAsync`             | Fact | Mass assignment server owned fields are
never honoured. |
| `Writes_ToForeignRowsAndParents_AreRefused_AndChangeNothingAsync`   | Fact | Writes to foreign rows and parents are
refused and change nothing. |
| `ForeignAndGatedIds_BehaveLikeMissing_OnEveryReadRouteAsync`        | Fact | Foreign and gated ids behave like missing
on every read route. |
| `Matrix_WriteRoutes_EveryPrincipalAsync`                            | Fact | Matrix write routes every principal. |
| `Matrix_ReadRoutes_EveryPrincipalAsync`                             | Fact | Matrix read routes every principal. |

| Test | Kind | Verifies |
|------|------|----------|

### PenTestCaseWorkflowFilterRole

`Jube.Test.Security.Case.PenTestCaseWorkflowFilterRole` — 6 tests. Category: PenTest. Collection: Database.
| `Delete_IsPermissionedAndObjectLevelAuthorisedAsync`                     | Fact | Delete is permissioned and object
level authorised. |
| `Post_TypeConfusionAndStructure_FailSafelyAsync`                         | Fact | Post type confusion and structure
fail safely. |
| `Post_DuplicateGrants_DoNotCreateDuplicateRowsAsync`                     | Fact | Post duplicate grants do not create
duplicate rows. |
| `Post_HappyPathAndMassAssignment_PersistOnlyServerControlledFieldsAsync` | Fact | Post happy path and mass assignment
persist only server controlled fields. |
| `Post_IsPermissionedAndRefusesForeignParentsAndRolesAsync`               | Fact | Post is permissioned and refuses
foreign parents and roles. |
| `Get_ByParentGuid_IsPermissionedAndTenantIsolatedAsync`                  | Fact | Get by parent guid is permissioned
and tenant isolated. |

| Test | Kind | Verifies |
|------|------|----------|

### PenTestCaseWorkflowForm

`Jube.Test.Security.Case.PenTestCaseWorkflowForm` — 12 tests. Category: PenTest. Collection: Database.
| `Delete_IsPermissionedAndObjectLevelAuthorisedAsync`            | Fact | Delete is permissioned and object level
authorised. |
| `Put_TypeConfusionAndInjection_FailSafelyAsync`                 | Fact | Put type confusion and injection fail
safely. |
| `Put_MassAssignment_LockingAndSoftDelete_BehaveSafelyAsync`     | Fact | Put mass assignment locking and soft delete
behave safely. |
| `Put_IsPermissionedAndObjectLevelAuthorisedAsync`               | Fact | Put is permissioned and object level
authorised. |
| `Post_LengthLimitsAndDuplicateNames_AreEnforcedAsync`           | Fact | Post length limits and duplicate names are
enforced. |
| `Post_TypeConfusionAndBoundaries_FailSafelyAsync`               | Fact | Post type confusion and boundaries fail
safely. |
| `Post_StoredHostilePayloads_AreDataAndRoundTripFaithfullyAsync` | Fact | Post stored hostile payloads are data and
round trip faithfully. |
| `Post_MassAssignment_ForbiddenFieldsAreNotHonouredAsync`        | Fact | Post mass assignment forbidden fields are not
honoured. |
| `Post_IsPermissionedAndRefusesForeignParentsAsync`              | Fact | Post is permissioned and refuses foreign
parents. |
| `Get_ByWorkflowRoutes_ArePermissionedAndTenantIsolatedAsync`    | Fact | Get by workflow routes are permissioned and
tenant isolated. |
| `Get_ById_IsPermissionedAndObjectLevelAuthorisedAsync`          | Fact | Get by id is permissioned and object level
authorised. |
| `Get_List_IsPermissionedAndTenantIsolatedAsync`                 | Fact | Get list is permissioned and tenant
isolated. |

| Test | Kind | Verifies |
|------|------|----------|

### PenTestCaseWorkflowFormEntry

`Jube.Test.Security.Case.PenTestCaseWorkflowFormEntry` — 5 tests. Category: PenTest. Collection: Database.

| Test                                                                | Kind | Verifies                                                                |
|---------------------------------------------------------------------|------|-------------------------------------------------------------------------|
| `Post_IsPermissionedAndRefusesForeignCasesAndFormsAsync`            | Fact | Post is permissioned and refuses foreign cases and forms.               |
| `Post_KeepsTheEntryConsistentWithItsCaseAsync`                      | Fact | Post keeps the entry consistent with its case.                          |
| `Post_HostilePayloadKeysAndValues_AreStoredAsDataAndRoundTripAsync` | Fact | Post hostile payload keys and values are stored as data and round trip. |
| `Post_StructuralAbuse_FailsSafelyAsync`                             | Fact | Post structural abuse fails safely.                                     |
| `Get_ByCaseKeyValue_IsPermissionedAndTenantIsolatedAsync`           | Fact | Get by case key value is permissioned and tenant isolated.              |

### PenTestCaseWorkflowFormEntryValue

`Jube.Test.Security.Case.PenTestCaseWorkflowFormEntryValue` — 1 test. Category: PenTest. Collection: Database.

| Test                                                        | Kind | Verifies                                                     |
|-------------------------------------------------------------|------|--------------------------------------------------------------|
| `Get_ByEntryId_IsPermissionedAndObjectLevelAuthorisedAsync` | Fact | Get by entry id is permissioned and object level authorised. |

### PenTestCaseWorkflowFormRole

`Jube.Test.Security.Case.PenTestCaseWorkflowFormRole` — 6 tests. Category: PenTest. Collection: Database.
| `Delete_IsPermissionedAndObjectLevelAuthorisedAsync`                     | Fact | Delete is permissioned and object
level authorised. |
| `Post_TypeConfusionAndStructure_FailSafelyAsync`                         | Fact | Post type confusion and structure
fail safely. |
| `Post_DuplicateGrants_DoNotCreateDuplicateRowsAsync`                     | Fact | Post duplicate grants do not create
duplicate rows. |
| `Post_HappyPathAndMassAssignment_PersistOnlyServerControlledFieldsAsync` | Fact | Post happy path and mass assignment
persist only server controlled fields. |
| `Post_IsPermissionedAndRefusesForeignParentsAndRolesAsync`               | Fact | Post is permissioned and refuses
foreign parents and roles. |
| `Get_ByParentGuid_IsPermissionedAndTenantIsolatedAsync`                  | Fact | Get by parent guid is permissioned
and tenant isolated. |

| Test | Kind | Verifies |
|------|------|----------|

### PenTestCaseWorkflowMacro

`Jube.Test.Security.Case.PenTestCaseWorkflowMacro` — 1 test. Category: PenTest. Collection: Database.

| Test                                                            | Kind | Verifies                                                         |
|-----------------------------------------------------------------|------|------------------------------------------------------------------|
| `Post_ImageLocation_RejectsPathsAndMarkupAsync`                 | Fact | Post image location rejects paths and markup.                    |
| `Delete_IsPermissionedAndObjectLevelAuthorisedAsync`            | Fact | Delete is permissioned and object level authorised.              |
| `Put_TypeConfusionAndInjection_FailSafelyAsync`                 | Fact | Put type confusion and injection fail safely.                    |
| `Put_MassAssignment_LockingAndSoftDelete_BehaveSafelyAsync`     | Fact | Put mass assignment locking and soft delete behave safely.       |
| `Put_IsPermissionedAndObjectLevelAuthorisedAsync`               | Fact | Put is permissioned and object level authorised.                 |
| `Post_LengthLimitsAndDuplicateNames_AreEnforcedAsync`           | Fact | Post length limits and duplicate names are enforced.             |
| `Post_TypeConfusionAndBoundaries_FailSafelyAsync`               | Fact | Post type confusion and boundaries fail safely.                  |
| `Post_StoredHostilePayloads_AreDataAndRoundTripFaithfullyAsync` | Fact | Post stored hostile payloads are data and round trip faithfully. |
| `Post_MassAssignment_ForbiddenFieldsAreNotHonouredAsync`        | Fact | Post mass assignment forbidden fields are not honoured.          |
| `Post_IsPermissionedAndRefusesForeignParentsAsync`              | Fact | Post is permissioned and refuses foreign parents.                |
| `Get_ByWorkflowRoutes_ArePermissionedAndTenantIsolatedAsync`    | Fact | Get by workflow routes are permissioned and tenant isolated.     |
| `Get_ById_IsPermissionedAndObjectLevelAuthorisedAsync`          | Fact | Get by id is permissioned and object level authorised.           |
| `Get_List_IsPermissionedAndTenantIsolatedAsync`                 | Fact | Get list is permissioned and tenant isolated.                    |

### PenTestCaseWorkspace

`Jube.Test.Security.Case.PenTestCaseWorkspace` — 7 tests. Category: PenTest. Collection: Database.

| Test                                                                                   | Kind | Verifies                                                                                                                                                                                                |
|----------------------------------------------------------------------------------------|------|---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| `Matrix_ReadEndpoints_EveryPrincipalAsync`                                             | Fact | Workflows, counts, run, risk matrix, analytics, supervision and stuck locks refuse anonymous, no-permission and no-tenant callers, give tenant B nothing of tenant A and give the owner their own data. |
| `ARoleGatedOrMissingWorkflow_IsIndistinguishableOnEveryReadAndWriteAsync`              | Fact | A role-gated or another tenant's workflow answers exactly like a missing one on every read and on skim, and a refused skim locks nothing.                                                               |
| `Run_HostileParametersAreRefusedOrHarmlessNeverAServerErrorAsync`                      | Fact | Hostile take, skip, view, sort, scope, period and idle parameters are refused or harmless and never a server error.                                                                                     |
| `Diary_Matrix_AndTheLockClosedAndForeignCasesAreNeverChangedAsync`                     | Fact | Diary set and clear refuse held, closed, foreign, role-gated, hidden-status and missing cases without changing them, refuse hostile bodies, ignore unlisted fields and work for the owner.              |
| `Supervision_BreakLockAndReassign_Matrix_AndNothingOutsideTheCallersReachChangesAsync` | Fact | Break lock and reassign need permission 18, never reach another tenant, role-gated, hidden-status or closed cases, refuse ineligible targets, and work for the owner.                                   |
| `SavedSearches_ArePrivateToTheirOwnerAndNeverRunAcrossTenantsAsync`                    | Fact | Saved searches ignore owner and version in the body, cannot be read, changed, deleted or run by another tenant or a no-permission user, and refuse hostile or foreign-workflow bodies.                  |
| `Skim_NeverLocksACaseOfAnotherTenantAndRefusesWithoutThePermissionAsync`               | Fact | Skim refuses anonymous, no-permission and cross-tenant callers, changes nothing of another tenant and refuses a type-confused body.                                                                     |

### PenTestCaseChecklist

`Jube.Test.Security.Case.PenTestCaseChecklist` — 4 tests. Category: PenTest. Collection: Database.

| Test                                                                         | Kind | Verifies                                                                        |
|------------------------------------------------------------------------------|------|---------------------------------------------------------------------------------|
| `Matrix_EveryEndpointIsAuthenticatedPermissionedAndTenantIsolatedAsync`      | Fact | Matrix every endpoint is authenticated permissioned and tenant isolated.        |
| `ForeignGatedHiddenAndMissingCases_AreIndistinguishableOnEveryEndpointAsync` | Fact | Foreign gated hidden and missing cases are indistinguishable on every endpoint. |
| `Mutations_WithoutAnAntiforgeryTokenOrOrigin_AreRefusedAsync`                | Fact | Mutations without an antiforgery token or origin are refused.                   |
| `HostileRouteSegmentsAndOverrideReasons_NeverReachAServerErrorAsync`         | Fact | Hostile route segments and override reasons never reach a server error.         |

### PenTestCaseHold

`Jube.Test.Security.Case.PenTestCaseHold` — 9 tests. Category: PenTest. Collection: Database.

| Test                                                                             | Kind   | Verifies                                                                               |
|----------------------------------------------------------------------------------|--------|----------------------------------------------------------------------------------------|
| `List_IsPermissionedTenantIsolatedAndHostileRangeNamesAreOnlyDataAsync`          | Fact   | List_is permissioned tenant isolated and hostile range names are only data.            |
| `Decision_IsPermissionedAndAForeignTenantChangesNothingAsync`                    | Theory | Decision_is permissioned and a foreign tenant changes nothing.                         |
| `Decision_Validation_RefusesBadBodiesWithoutCallingAnythingAsync`                | Fact   | Decision_validation_refuses bad bodies without calling anything.                       |
| `Decision_CrossSiteOrOriginlessCookiePostsAreRefusedAndNothingIsCalledAsync`     | Fact   | Decision_cross site or originless cookie posts are refused and nothing is called.      |
| `TheCredentialValueNeverAppearsInAnyResponseTheArchiveOrTheAuditAsync`           | Theory | The credential value never appears in any response the archive or the audit.           |
| `ASecondReleaseOfThePaymentIsRefusedAndTheHookIsCalledOnceAsync`                 | Fact   | A second release of the payment is refused and the hook is called once.                |
| `ARelease_ToAnInternalHostIsRefusedAtSendTimeAuditedAndTheHoldStaysHeldAsync`    | Theory | A release_to an internal host is refused at send time audited and the hold stays held. |
| `ARelease_ToAnAllowListedLoopbackHostIsSentAsync`                                | Fact   | A release_to an allow listed loopback host is sent.                                    |
| `Decision_CookiePostsWithoutAnAntiforgeryTokenAreRefusedAndNothingIsCalledAsync` | Fact   | Decision cookie posts without an antiforgery token are refused and nothing is called.  |

### PenTestCaseWorkflowReleaseHook

`Jube.Test.Security.Case.PenTestCaseWorkflowReleaseHook` — 15 tests. Category: PenTest. Collection: Database.

| Test                                                                                    | Kind | Verifies                                                                                         |
|-----------------------------------------------------------------------------------------|------|--------------------------------------------------------------------------------------------------|
| `Post_CredentialVariable_NeverNamesAnythingButAHookVariableAsync`                       | Fact | Post_credential variable_never names anything but a hook variable.                               |
| `Post_ReleaseUrl_RefusesAnyHostThatIsNotWrittenOutLiterallyAsync`                       | Fact | Post_release url_refuses any host that is not written out literally.                             |
| `Post_ReleaseAndRefuseUrl_RefuseAnInternalHostAtSaveTimeAndAcceptAnAllowListedOneAsync` | Fact | Post_release and refuse url_refuse an internal host at save time and accept an allow listed one. |
| `Delete_IsPermissionedAndObjectLevelAuthorisedAsync`                                    | Fact | Delete_is permissioned and object level authorised.                                              |
| `Put_TypeConfusionAndInjection_FailSafelyAsync`                                         | Fact | Put_type confusion and injection_fail safely.                                                    |
| `Put_MassAssignment_LockingAndSoftDelete_BehaveSafelyAsync`                             | Fact | Put_mass assignment_locking and soft delete_behave safely.                                       |
| `Put_IsPermissionedAndObjectLevelAuthorisedAsync`                                       | Fact | Put_is permissioned and object level authorised.                                                 |
| `Post_LengthLimitsAndDuplicateNames_AreEnforcedAsync`                                   | Fact | Post_length limits and duplicate names_are enforced.                                             |
| `Post_TypeConfusionAndBoundaries_FailSafelyAsync`                                       | Fact | Post_type confusion and boundaries_fail safely.                                                  |
| `Post_StoredHostilePayloads_AreDataAndRoundTripFaithfullyAsync`                         | Fact | Post_stored hostile payloads_are data and round trip faithfully.                                 |
| `Post_MassAssignment_ForbiddenFieldsAreNotHonouredAsync`                                | Fact | Post_mass assignment_forbidden fields are not honoured.                                          |
| `Post_IsPermissionedAndRefusesForeignParentsAsync`                                      | Fact | Post_is permissioned and refuses foreign parents.                                                |
| `Get_ByWorkflowRoutes_ArePermissionedAndTenantIsolatedAsync`                            | Fact | Get_by workflow routes_are permissioned and tenant isolated.                                     |
| `Get_ById_IsPermissionedAndObjectLevelAuthorisedAsync`                                  | Fact | Get_by id_is permissioned and object level authorised.                                           |
| `Get_List_IsPermissionedAndTenantIsolatedAsync`                                         | Fact | Get_list_is permissioned and tenant isolated.                                                    |

### PenTestCaseWorkflowChecklist

`Jube.Test.Security.Case.PenTestCaseWorkflowChecklist` — 8 tests (inherited from `PenTestCaseWorkflowGuidChildBase`).
Category: PenTest. Collection: PenTest. Route `/api/CaseWorkflowChecklist`.
| `EveryMutation_WithoutAntiforgeryOrOriginEvidence_IsRefusedAndChangesNothingAsync` | Fact | Every mutation without
antiforgery or origin evidence is refused and changes nothing. |
| `HostileText_TypeConfusionAndOversizeBodies_FailSafelyAsync` | Fact | Hostile text type confusion and oversize bodies
fail safely. |
| `SoftDelete_RemovesTheRowFromEveryRouteAndReplayIsNotFoundAsync` | Fact | Soft delete removes the row from every route
and replay is not found. |
| `Put_CannotMoveARowToAnotherParentAsync` | Fact | Put cannot move a row to another parent. |
| `MassAssignment_OnCreateAndUpdate_IsIgnoredAndStaleVersionsConflictAsync` | Fact | Mass assignment on create and
update is ignored and stale versions conflict. |
| `Post_RefusesForeignAndInvalidParentGuidsAndCreatesNothingAsync` | Fact | Post refuses foreign and invalid parent
guids and creates nothing. |
| `ForeignTenantRows_AreInvisibleAndImmutable_ByIdAsync` | Fact | Foreign tenant rows are invisible and immutable by
id. |
| `EveryVerb_RefusesAnonymousGarbageAndUnpermissionedCallersAsync` | Fact | Every verb refuses anonymous garbage and
unpermissioned callers. |

| Test | Kind | Verifies |
|------|------|----------|

### PenTestCaseWorkflowChecklistItem

`Jube.Test.Security.Case.PenTestCaseWorkflowChecklistItem` — 8 tests (inherited from
`PenTestCaseWorkflowGuidChildBase`). Category: PenTest. Collection: PenTest. Route `/api/CaseWorkflowChecklistItem`.
| `EveryMutation_WithoutAntiforgeryOrOriginEvidence_IsRefusedAndChangesNothingAsync` | Fact | Every mutation without
antiforgery or origin evidence is refused and changes nothing. |
| `HostileText_TypeConfusionAndOversizeBodies_FailSafelyAsync` | Fact | Hostile text type confusion and oversize bodies
fail safely. |
| `SoftDelete_RemovesTheRowFromEveryRouteAndReplayIsNotFoundAsync` | Fact | Soft delete removes the row from every route
and replay is not found. |
| `Put_CannotMoveARowToAnotherParentAsync` | Fact | Put cannot move a row to another parent. |
| `MassAssignment_OnCreateAndUpdate_IsIgnoredAndStaleVersionsConflictAsync` | Fact | Mass assignment on create and
update is ignored and stale versions conflict. |
| `Post_RefusesForeignAndInvalidParentGuidsAndCreatesNothingAsync` | Fact | Post refuses foreign and invalid parent
guids and creates nothing. |
| `ForeignTenantRows_AreInvisibleAndImmutable_ByIdAsync` | Fact | Foreign tenant rows are invisible and immutable by
id. |
| `EveryVerb_RefusesAnonymousGarbageAndUnpermissionedCallersAsync` | Fact | Every verb refuses anonymous garbage and
unpermissioned callers. |

| Test | Kind | Verifies |
|------|------|----------|

### PenTestCaseWorkflowFormValidator

`Jube.Test.Security.Case.PenTestCaseWorkflowFormValidator` — 8 tests (inherited from
`PenTestCaseWorkflowGuidChildBase`). Category: PenTest. Collection: PenTest. Route `/api/CaseWorkflowFormValidator`.
| `EveryMutation_WithoutAntiforgeryOrOriginEvidence_IsRefusedAndChangesNothingAsync` | Fact | Every mutation without
antiforgery or origin evidence is refused and changes nothing. |
| `HostileText_TypeConfusionAndOversizeBodies_FailSafelyAsync` | Fact | Hostile text type confusion and oversize bodies
fail safely. |
| `SoftDelete_RemovesTheRowFromEveryRouteAndReplayIsNotFoundAsync` | Fact | Soft delete removes the row from every route
and replay is not found. |
| `Put_CannotMoveARowToAnotherParentAsync` | Fact | Put cannot move a row to another parent. |
| `MassAssignment_OnCreateAndUpdate_IsIgnoredAndStaleVersionsConflictAsync` | Fact | Mass assignment on create and
update is ignored and stale versions conflict. |
| `Post_RefusesForeignAndInvalidParentGuidsAndCreatesNothingAsync` | Fact | Post refuses foreign and invalid parent
guids and creates nothing. |
| `ForeignTenantRows_AreInvisibleAndImmutable_ByIdAsync` | Fact | Foreign tenant rows are invisible and immutable by
id. |
| `EveryVerb_RefusesAnonymousGarbageAndUnpermissionedCallersAsync` | Fact | Every verb refuses anonymous garbage and
unpermissioned callers. |

| Test | Kind | Verifies |
|------|------|----------|

### PenTestCaseWorkflowMacroRole

`Jube.Test.Security.Case.PenTestCaseWorkflowMacroRole` — 6 tests. Category: PenTest. Collection: Database.
| `Delete_IsPermissionedAndObjectLevelAuthorisedAsync`                     | Fact | Delete is permissioned and object
level authorised. |
| `Post_TypeConfusionAndStructure_FailSafelyAsync`                         | Fact | Post type confusion and structure
fail safely. |
| `Post_DuplicateGrants_DoNotCreateDuplicateRowsAsync`                     | Fact | Post duplicate grants do not create
duplicate rows. |
| `Post_HappyPathAndMassAssignment_PersistOnlyServerControlledFieldsAsync` | Fact | Post happy path and mass assignment
persist only server controlled fields. |
| `Post_IsPermissionedAndRefusesForeignParentsAndRolesAsync`               | Fact | Post is permissioned and refuses
foreign parents and roles. |
| `Get_ByParentGuid_IsPermissionedAndTenantIsolatedAsync`                  | Fact | Get by parent guid is permissioned
and tenant isolated. |

| Test | Kind | Verifies |
|------|------|----------|

### PenTestCaseWorkflowPriority

`Jube.Test.Security.Case.PenTestCaseWorkflowPriority` — 1 test. Category: PenTest. Collection: Database.

| Test                                                              | Kind | Verifies                                                          |
|-------------------------------------------------------------------|------|-------------------------------------------------------------------|
| `Get_Priorities_ArePermissionedAndLeakNothingAboutWorkflowsAsync` | Fact | Get priorities are permissioned and leak nothing about workflows. |

### PenTestCaseWorkflowRole

`Jube.Test.Security.Case.PenTestCaseWorkflowRole` — 6 tests. Category: PenTest. Collection: Database.
| `Delete_IsPermissionedAndObjectLevelAuthorisedAsync`                     | Fact | Delete is permissioned and object
level authorised. |
| `Post_TypeConfusionAndStructure_FailSafelyAsync`                         | Fact | Post type confusion and structure
fail safely. |
| `Post_DuplicateGrants_DoNotCreateDuplicateRowsAsync`                     | Fact | Post duplicate grants do not create
duplicate rows. |
| `Post_HappyPathAndMassAssignment_PersistOnlyServerControlledFieldsAsync` | Fact | Post happy path and mass assignment
persist only server controlled fields. |
| `Post_IsPermissionedAndRefusesForeignParentsAndRolesAsync`               | Fact | Post is permissioned and refuses
foreign parents and roles. |
| `Get_ByParentGuid_IsPermissionedAndTenantIsolatedAsync`                  | Fact | Get by parent guid is permissioned
and tenant isolated. |

| Test | Kind | Verifies |
|------|------|----------|

### PenTestCaseWorkflowStatus

`Jube.Test.Security.Case.PenTestCaseWorkflowStatus` — 1 test. Category: PenTest. Collection: Database.

| Test                                                            | Kind | Verifies                                                         |
|-----------------------------------------------------------------|------|------------------------------------------------------------------|
| `Post_Priority_IsBoundedAndDuplicatesAreAllowedAsync`           | Fact | Post priority is bounded and duplicates are allowed.             |
| `Delete_IsPermissionedAndObjectLevelAuthorisedAsync`            | Fact | Delete is permissioned and object level authorised.              |
| `Put_TypeConfusionAndInjection_FailSafelyAsync`                 | Fact | Put type confusion and injection fail safely.                    |
| `Put_MassAssignment_LockingAndSoftDelete_BehaveSafelyAsync`     | Fact | Put mass assignment locking and soft delete behave safely.       |
| `Put_IsPermissionedAndObjectLevelAuthorisedAsync`               | Fact | Put is permissioned and object level authorised.                 |
| `Post_LengthLimitsAndDuplicateNames_AreEnforcedAsync`           | Fact | Post length limits and duplicate names are enforced.             |
| `Post_TypeConfusionAndBoundaries_FailSafelyAsync`               | Fact | Post type confusion and boundaries fail safely.                  |
| `Post_StoredHostilePayloads_AreDataAndRoundTripFaithfullyAsync` | Fact | Post stored hostile payloads are data and round trip faithfully. |
| `Post_MassAssignment_ForbiddenFieldsAreNotHonouredAsync`        | Fact | Post mass assignment forbidden fields are not honoured.          |
| `Post_IsPermissionedAndRefusesForeignParentsAsync`              | Fact | Post is permissioned and refuses foreign parents.                |
| `Get_ByWorkflowRoutes_ArePermissionedAndTenantIsolatedAsync`    | Fact | Get by workflow routes are permissioned and tenant isolated.     |
| `Get_ById_IsPermissionedAndObjectLevelAuthorisedAsync`          | Fact | Get by id is permissioned and object level authorised.           |
| `Get_List_IsPermissionedAndTenantIsolatedAsync`                 | Fact | Get list is permissioned and tenant isolated.                    |

### PenTestCaseWorkflowStatusRole

`Jube.Test.Security.Case.PenTestCaseWorkflowStatusRole` — 6 tests. Category: PenTest. Collection: Database.
| `Delete_IsPermissionedAndObjectLevelAuthorisedAsync`                     | Fact | Delete is permissioned and object
level authorised. |
| `Post_TypeConfusionAndStructure_FailSafelyAsync`                         | Fact | Post type confusion and structure
fail safely. |
| `Post_DuplicateGrants_DoNotCreateDuplicateRowsAsync`                     | Fact | Post duplicate grants do not create
duplicate rows. |
| `Post_HappyPathAndMassAssignment_PersistOnlyServerControlledFieldsAsync` | Fact | Post happy path and mass assignment
persist only server controlled fields. |
| `Post_IsPermissionedAndRefusesForeignParentsAndRolesAsync`               | Fact | Post is permissioned and refuses
foreign parents and roles. |
| `Get_ByParentGuid_IsPermissionedAndTenantIsolatedAsync`                  | Fact | Get by parent guid is permissioned
and tenant isolated. |

| Test | Kind | Verifies |
|------|------|----------|

### PenTestCaseWorkflowXPath

`Jube.Test.Security.Case.PenTestCaseWorkflowXPath` — 1 test. Category: PenTest. Collection: Database.

| Test                                                            | Kind | Verifies                                                         |
|-----------------------------------------------------------------|------|------------------------------------------------------------------|
| `Post_ColourFields_RejectMarkupAndStyleBreakoutsAsync`          | Fact | Post colour fields reject markup and style breakouts.            |
| `Delete_IsPermissionedAndObjectLevelAuthorisedAsync`            | Fact | Delete is permissioned and object level authorised.              |
| `Put_TypeConfusionAndInjection_FailSafelyAsync`                 | Fact | Put type confusion and injection fail safely.                    |
| `Put_MassAssignment_LockingAndSoftDelete_BehaveSafelyAsync`     | Fact | Put mass assignment locking and soft delete behave safely.       |
| `Put_IsPermissionedAndObjectLevelAuthorisedAsync`               | Fact | Put is permissioned and object level authorised.                 |
| `Post_LengthLimitsAndDuplicateNames_AreEnforcedAsync`           | Fact | Post length limits and duplicate names are enforced.             |
| `Post_TypeConfusionAndBoundaries_FailSafelyAsync`               | Fact | Post type confusion and boundaries fail safely.                  |
| `Post_StoredHostilePayloads_AreDataAndRoundTripFaithfullyAsync` | Fact | Post stored hostile payloads are data and round trip faithfully. |
| `Post_MassAssignment_ForbiddenFieldsAreNotHonouredAsync`        | Fact | Post mass assignment forbidden fields are not honoured.          |
| `Post_IsPermissionedAndRefusesForeignParentsAsync`              | Fact | Post is permissioned and refuses foreign parents.                |
| `Get_ByWorkflowRoutes_ArePermissionedAndTenantIsolatedAsync`    | Fact | Get by workflow routes are permissioned and tenant isolated.     |
| `Get_ById_IsPermissionedAndObjectLevelAuthorisedAsync`          | Fact | Get by id is permissioned and object level authorised.           |
| `Get_List_IsPermissionedAndTenantIsolatedAsync`                 | Fact | Get list is permissioned and tenant isolated.                    |

### PenTestCaseWorkflowXPathRole

`Jube.Test.Security.Case.PenTestCaseWorkflowXPathRole` — 6 tests. Category: PenTest. Collection: Database.
| `Delete_IsPermissionedAndObjectLevelAuthorisedAsync`                     | Fact | Delete is permissioned and object
level authorised. |
| `Post_TypeConfusionAndStructure_FailSafelyAsync`                         | Fact | Post type confusion and structure
fail safely. |
| `Post_DuplicateGrants_DoNotCreateDuplicateRowsAsync`                     | Fact | Post duplicate grants do not create
duplicate rows. |
| `Post_HappyPathAndMassAssignment_PersistOnlyServerControlledFieldsAsync` | Fact | Post happy path and mass assignment
persist only server controlled fields. |
| `Post_IsPermissionedAndRefusesForeignParentsAndRolesAsync`               | Fact | Post is permissioned and refuses
foreign parents and roles. |
| `Get_ByParentGuid_IsPermissionedAndTenantIsolatedAsync`                  | Fact | Get by parent guid is permissioned
and tenant isolated. |

| Test | Kind | Verifies |
|------|------|----------|

## Dynamic sql

### PenTestCaseBySessionCaseSearchCompile

`Jube.Test.Security.DynamicSql.PenTestCaseBySessionCaseSearchCompile` — 2 tests. Category: PenTest.

| Test                                           | Kind | Verifies                                    |
|------------------------------------------------|------|---------------------------------------------|
| `AuthenticationAuthorisationAndOwnershipAsync` | Fact | Authentication authorisation and ownership. |
| `AStoredRowWithHostileSqlNeverRunsAsync`       | Fact | A stored row with hostile sql never runs.   |

### PenTestCaseJournal

`Jube.Test.Security.DynamicSql.PenTestCaseJournal` — 4 tests. Category: PenTest.

| Test                                                                          | Kind | Verifies                                                                            |
|-------------------------------------------------------------------------------|------|-------------------------------------------------------------------------------------|
| `AuthenticationAndAuthorisationMatrixAsync`                                   | Fact | Authentication and authorisation matrix.                                            |
| `TenantScope_AnotherTenantsArchiveIsNeverReturnedAsync`                       | Fact | Tenant scope another tenants archive is never returned.                             |
| `InjectionInDrillNameDrillValueAndEveryOtherParameter_IsInertAndBoundedAsync` | Fact | Injection in drill name drill value and every other parameter is inert and bounded. |
| `LimitIsBounded_NegativeIsAClientError_HugeIsClampedAsync`                    | Fact | Limit is bounded negative is a client error huge is clamped.                        |

### PenTestEntityAnalysisModelSample

`Jube.Test.Security.DynamicSql.PenTestEntityAnalysisModelSample` — 5 tests. Category: PenTest.

| Test                                                            | Kind | Verifies                                                           |
|-----------------------------------------------------------------|------|--------------------------------------------------------------------|
| `AuthenticationAndAuthorisationMatrixAsync`                     | Fact | Authentication and authorisation matrix.                           |
| `AnotherTenantsModel_IsNotFound_ExactlyLikeAnUnknownModelAsync` | Fact | Another tenants model is not found exactly like an unknown model.  |
| `CsvCells_NeverCarryAFormulaThatASpreadsheetWouldEvaluateAsync` | Fact | Csv cells never carry a formula that a spreadsheet would evaluate. |
| `BoundaryValuesAndMalformedBodies_NeverProduceA500Async`        | Fact | Boundary values and malformed bodies never produce a 500.          |
| `Replay_AndConcurrentSamples_AreSafeAndBoundedAsync`            | Fact | Replay and concurrent samples are safe and bounded.                |

### PenTestParser

`Jube.Test.Security.DynamicSql.PenTestParser` — 8 tests. Category: PenTest.

| Test                                                          | Kind | Verifies                                                          |
|---------------------------------------------------------------|------|-------------------------------------------------------------------|
| `AuthenticationAndAuthorisationMatrixAsync`                   | Fact | Authentication and authorisation matrix.                          |
| `AnotherTenantsModel_LooksExactlyLikeAnUnknownModelAsync`     | Fact | Another tenants model looks exactly like an unknown model.        |
| `NothingOutsideTheAllowList_CanBeCompiled_InAnySpellingAsync` | Fact | Nothing outside the allow list can be compiled in any spelling.   |
| `CurlyAndFullWidthQuotes_CannotHideCodeFromTheTokenScanAsync` | Fact | Curly and full width quotes cannot hide code from the token scan. |
| `EveryRuleParseType_AndOddTypeValues_AreHandledAsync`         | Fact | Every rule parse type and odd type values are handled.            |
| `OversizeAndDeeplyNestedText_IsBoundedAndNeverA500Async`      | Fact | Oversize and deeply nested text is bounded and never a 500.       |
| `MalformedBodies_AreClientErrorsNeverA500Async`               | Fact | Malformed bodies are client errors never a 500.                   |
| `ParsingIsSideEffectFree_AndConcurrentParsesAreSafeAsync`     | Fact | Parsing is side effect free and concurrent parses are safe.       |

### PenTestPostgresGuard

`Jube.Test.Security.DynamicSql.PenTestPostgresGuard` — 3 tests. Category: PenTest. Collection: Database.

| Test                                                              | Kind | Verifies                                                               |
|-------------------------------------------------------------------|------|------------------------------------------------------------------------|
| `ReadOnlyAndTimeoutSurviveAReconnectAfterTheBackendIsKilledAsync` | Fact | Read only and timeout survive a reconnect after the backend is killed. |
| `AGuardedExecutionStillWorksAfterAReconnectAsync`                 | Fact | A guarded execution still works after a reconnect.                     |
| `ArchiveSampleLimitIsABoundParameterAndNotAParameterNumberAsync`  | Fact | Archive sample limit is a bound parameter and not a parameter number.  |

### PenTestSessionCaseSearchCompiledSql

`Jube.Test.Security.DynamicSql.PenTestSessionCaseSearchCompiledSql` — 8 tests. Category: PenTest.

| Test                                                                       | Kind | Verifies                                                                      |
|----------------------------------------------------------------------------|------|-------------------------------------------------------------------------------|
| `AuthenticationAndAuthorisationMatrixAsync`                                | Fact | Authentication and authorisation matrix.                                      |
| `CompileAndExecute_HappyPath_AndOwnerScopingAsync`                         | Fact | Compile and execute happy path and owner scoping.                             |
| `SelectColumnExpression_IsNeverConcatenatedIntoSqlAsync`                   | Fact | Select column expression is never concatenated into sql.                      |
| `GroupConditionOperatorAndIdentifiers_AreClosedAllowListsAsync`            | Fact | Group condition operator and identifiers are closed allow lists.              |
| `CrossTenantWorkflowAndFilterGuids_AreRefusedWithoutLeakingExistenceAsync` | Fact | Cross tenant workflow and filter guids are refused without leaking existence. |
| `FilterValuesAreBoundParameters_AndBadTypesAreClientErrorsAsync`           | Fact | Filter values are bound parameters and bad types are client errors.           |
| `MalformedBodies_AreClientErrorsNeverA500Async`                            | Fact | Malformed bodies are client errors never a 500.                               |
| `ReplayAndDoubleSubmit_AreSafeAsync`                                       | Fact | Replay and double submit are safe.                                            |

### PenTestSqlGate

`Jube.Test.Security.DynamicSql.PenTestSqlGate` — 17 tests. Category: PenTest.

| Test                                                                  | Kind   | Verifies                                                                         |
|-----------------------------------------------------------------------|--------|----------------------------------------------------------------------------------|
| `EveryWriteSeedAndEverySpellingOfItIsRejected`                        | Fact   | Every write seed and every spelling of it is rejected.                           |
| `EveryDangerousFunctionInEveryPositionIsRejected`                     | Fact   | Every dangerous function in every position is rejected.                          |
| `CataloguesSystemViewsAndCredentialTablesAreRejectedInEverySpelling`  | Fact   | Catalogues system views and credential tables are rejected in every spelling.    |
| `LegitimateSelectDatasourcesKeepWorking`                              | Fact   | Legitimate select datasources keep working.                                      |
| `MalformedOrEmptyTextIsRejectedNotCrashed`                            | Theory | Malformed or empty text is rejected not crashed.                                 |
| `NullIsRejected`                                                      | Fact   | Null is rejected.                                                                |
| `DeeplyNestedParenthesesAreRejectedOrAcceptedNeverCrash`              | Theory | Deeply nested parentheses are rejected or accepted never crash.                  |
| `DeeplyNestedSubqueriesAreRejectedOrAcceptedNeverCrash`               | Theory | Deeply nested subqueries are rejected or accepted never crash.                   |
| `ADeepWriteHiddenInsideNestedSubqueriesIsStillFound`                  | Fact   | A deep write hidden inside nested subqueries is still found.                     |
| `OversizeStatementsAreRejected`                                       | Fact   | Oversize statements are rejected.                                                |
| `ServerBuiltSqlThatJoinsTheIdentityTablesForTenantScopingStillPasses` | Fact   | Server built sql that joins the identity tables for tenant scoping still passes. |
| `ParameterTokensNeverChangeTheVerdict`                                | Fact   | Parameter tokens never change the verdict.                                       |
| `AmplificationAndInformationFunctionsAreRefused`                      | Theory | Amplification and information functions are refused.                             |
| `LegitimateFunctionsStillPass`                                        | Theory | Legitimate functions still pass.                                                 |
| `MaximumSizeStatementsAreValidatedInBoundedTime`                      | Fact   | Maximum size statements are validated in bounded time.                           |
| `ValidationCostGrowsLinearlyWithStatementSize`                        | Fact   | Validation cost grows linearly with statement size.                              |
| `StatementsOverTheSizeCapAreRefusedWithoutBeingParsed`                | Fact   | Statements over the size cap are refused without being parsed.                   |

### PenTestVisualisationRegistryDatasource

`Jube.Test.Security.DynamicSql.PenTestVisualisationRegistryDatasource` — 15 tests. Category: PenTest.

| Test                                                                           | Kind   | Verifies                                                                           |
|--------------------------------------------------------------------------------|--------|------------------------------------------------------------------------------------|
| `AuthenticationAndAuthorisationMatrixAsync`                                    | Theory | Authentication and authorisation matrix.                                           |
| `SavePath_RejectsEveryWriteAsync`                                              | Fact   | Save path rejects every write.                                                     |
| `UpdatePath_RejectsEveryWriteAsync`                                            | Fact   | Update path rejects every write.                                                   |
| `LegitimateSelectDatasourcesCanStillBeSavedAndUpdatedAsync`                    | Fact   | Legitimate select datasources can still be saved and updated.                      |
| `Idor_ForeignIdsBehaveLikeNotFoundAndNothingChangesAsync`                      | Fact   | Idor foreign ids behave like not found and nothing changes.                        |
| `MassAssignment_ServerOwnedFieldsAreNotHonouredAsync`                          | Fact   | Mass assignment server owned fields are not honoured.                              |
| `InjectionInEveryStringFieldAndRouteParameter_IsStoredAsDataOrRejectedAsync`   | Fact   | Injection in every string field and route parameter is stored as data or rejected. |
| `TypeConfusionAndMalformedBodies_NeverProduceA500Async`                        | Fact   | Type confusion and malformed bodies never produce a 500.                           |
| `BusinessFlow_DeleteThenUpdate_DoesNotResurrect_AndDoubleSubmitIsSafeAsync`    | Fact   | Business flow delete then update does not resurrect and double submit is safe.     |
| `Introspect_RefusesEveryAttackAndChangesNothingAsync`                          | Fact   | Introspect refuses every attack and changes nothing.                               |
| `Introspect_AcceptsALegitimateSelectAndForeignRegistriesLookLikeNotFoundAsync` | Fact   | Introspect accepts a legitimate select and foreign registries look like not found. |
| `Introspect_MalformedBodiesNeverProduceA500Async`                              | Fact   | Introspect malformed bodies never produce a 500.                                   |
| `Preview_ReportsEveryAttackAsAnErrorAndChangesNothingAsync`                    | Fact   | Preview reports every attack as an error and changes nothing.                      |
| `Preview_DrawsALegitimateSelectAndForeignRegistriesLookLikeNotFoundAsync`      | Fact   | Preview draws a legitimate select and foreign registries look like not found.      |
| `Preview_MalformedBodiesAndHostileValuesNeverProduceA500Async`                 | Fact   | Preview malformed bodies and hostile values never produce a 500.                   |

### PenTestVisualisationRegistryDatasourceCommandExecution

`Jube.Test.Security.DynamicSql.PenTestVisualisationRegistryDatasourceCommandExecution` — 9 tests. Category: PenTest.

| Test                                                                                    | Kind | Verifies                                                                                      |
|-----------------------------------------------------------------------------------------|------|-----------------------------------------------------------------------------------------------|
| `Card_AuthenticationAndTenantIsolationMatrixAsync`                                      | Fact | Card authentication and tenant isolation matrix.                                              |
| `Card_MalformedBodiesAndHostileValuesNeverProduceA500Async`                             | Fact | Card malformed bodies and hostile values never produce a500.                                  |
| `ExecutePath_NeverRunsAWriteAsync`                                                      | Fact | Execute path never runs a write.                                                              |
| `LegitimateDatasourcesStillReturnTheirRowsAsync`                                        | Fact | Legitimate datasources still return their rows.                                               |
| `Parameters_AreBoundAsTypedValues_AnInjectedValueNeverChangesTheStatementAsync`         | Fact | Parameters are bound as typed values an injected value never changes the statement.           |
| `BadParameterBodies_AreAClientErrorNeverA500Async`                                      | Fact | Bad parameter bodies are a client error never a 500.                                          |
| `StatementTimeout_And_RowLimit_BoundTheCostAsync`                                       | Fact | Statement timeout and row limit bound the cost.                                               |
| `FailingLegitimateSql_DoesNotLeakSchemaOrConnectionDetailsAsync`                        | Fact | Failing legitimate sql does not leak schema or connection details.                            |
| `ExecutionsAreLoggedWithTheirParameters_AndOnlyForTheCallersOwnVisibleDatasourcesAsync` | Fact | Executions are logged with their parameters and only for the callers own visible datasources. |

## Entity approval

### PenTestEntityApproval

`Jube.Test.Security.EntityApproval.PenTestEntityApproval` — 10 tests. Category: PenTest. Collection: Database.

| Test                                                                     | Kind | Verifies                                                                         |
|--------------------------------------------------------------------------|------|----------------------------------------------------------------------------------|
| `ACheckerInTheSameTenantCanApproveAndTheRecordedCheckerIsTheCallerAsync` | Fact | A checker in the same tenant can approve and the recorded checker is the caller. |
| `AMakerWithApprovalRightsCannotApproveOrRejectTheirOwnChangeAsync`       | Fact | A maker with approval rights cannot approve or reject their own change.          |
| `ACheckerInAnotherTenantCannotDecideOnThisTenantsChangeAsync`            | Fact | A checker in another tenant cannot decide on this tenants change.                |
| `ApprovalStateOfAnotherTenantIsNotReadableAsync`                         | Fact | Approval state of another tenant is not readable.                                |
| `ClientsWithoutTheApprovalRightsCannotDecideOrListAsync`                 | Fact | Clients without the approval rights cannot decide or list.                       |
| `ANoteOverTheLimitIsRefusedAndNothingIsRecordedAsync`                    | Fact | A note over the limit is refused and nothing is recorded.                        |
| `ForgedFieldsOnADecisionAreIgnoredAsync`                                 | Fact | Forged fields on a decision are ignored.                                         |
| `ADecisionOnAStaleOrUnknownVersionIsRefusedAsync`                        | Fact | A decision on a stale or unknown version is refused.                             |
| `BulkValueApprovalStaysWithinTheCallersTenantAsync`                      | Fact | Bulk value approval stays within the callers tenant.                             |
| `ValueStatusOfAnotherTenantsListIsNotReadableAsync`                      | Fact | Value status of another tenants list is not readable.                            |

### PenTestForceOverrideApproval

`Jube.Test.Security.EntityApproval.PenTestForceOverrideApproval` — 7 tests. Category: PenTest. Collection: Database.

| Test                                                                                   | Kind | Verifies                                                                                                                                                    |
|----------------------------------------------------------------------------------------|------|-------------------------------------------------------------------------------------------------------------------------------------------------------------|
| `ACheckerInTheSameTenantCanApproveAForceOverrideAndTheRecordedCheckerIsTheCallerAsync` | Fact | A checker in the tenant can approve a Force override and the recorded checker is the caller.                                                                |
| `AMakerWithApprovalRightsCannotApproveOrRejectTheirOwnForceOverrideAsync`              | Fact | A maker with approval rights cannot approve or reject their own Force override.                                                                             |
| `ACheckerInAnotherTenantCannotDecideOnOrReadAForceOverrideOfThisTenantAsync`           | Fact | Another tenant cannot decide on, read the status or history of, or list a Force override.                                                                   |
| `ClientsWithoutTheApprovalRightsCannotDecideOrListForceOverridesAsync`                 | Fact | Wrong permission is 403 and anonymous is 401 for approve, reject and the pending list.                                                                      |
| `ForgedFieldsOnAForceOverrideDecisionAreIgnoredAsync`                                  | Fact | Forged user, tenant, date and state on a decision are ignored.                                                                                              |
| `ASuppressOverrideIsNotSubjectToApprovalAndCannotBeDecidedOnAsync`                     | Fact | A Suppress override is not an approval subject and no approval row is written.                                                                              |
| `ResubmitAndRetractOfAForceOverrideAreGatedAndTenantScopedAsync`                       | Fact | Resubmit and retract: anonymous 401, no permission 403, cross-tenant refused, Force permission needed to resubmit, not-rejected refused, version unchanged. |

### PenTestSensitiveRouteCoverage

`Jube.Test.Security.EntityApproval.PenTestSensitiveRouteCoverage` — 3 tests. Category: PenTest.

| Test                                         | Kind | Verifies                                            |
|----------------------------------------------|------|-----------------------------------------------------|
| `EverySensitiveRouteHasASpecificPenTest`     | Fact | Every sensitive route has a specific pen test.      |
| `TheSensitiveRegistryHasNoStaleEntries`      | Fact | The sensitive registry has no stale entries.        |
| `TheSensitiveRoutesAreFoundOnTheRunningHost` | Fact | The sensitive routes are found on the running host. |

### PenTestVersionHistoryTenancy

`Jube.Test.Security.EntityApproval.PenTestVersionHistoryTenancy` — 3 tests. Category: PenTest. Collection: Database.

| Test                                                  | Kind | Verifies                                                 |
|-------------------------------------------------------|------|----------------------------------------------------------|
| `TheOwningTenantReadsItsOwnVersionHistoryAsync`       | Fact | The owning tenant reads its own version history.         |
| `ANotherTenantReadsNoVersionHistoryOfThisTenantAsync` | Fact | A nother tenant reads no version history of this tenant. |
| `TheTenantRegistryHistoryIsLandlordOnlyAsync`         | Fact | The tenant registry history is landlord only.            |

## Exhaustive

### PenTestExhaustiveInstanceCsv

`Jube.Test.Security.Exhaustive.PenTestExhaustiveInstanceCsv` — 16 tests. Category: PenTest. Collection: Database.

| Test                                                                                       | Kind   | Verifies                                                                                         |
|--------------------------------------------------------------------------------------------|--------|--------------------------------------------------------------------------------------------------|
| `AuthenticationAndAuthorisationMatrix_RefusesEveryone_ButThePermittedUserAsync`            | Fact   | Authentication and authorisation matrix refuses everyone but the permitted user.                 |
| `UploadThenConfirm_WorksEndToEndThroughTheRealPipelineAsync`                               | Fact   | Upload then confirm works end to end through the real pipeline.                                  |
| `CrossTenant_ReadConfirmDelete_BehaveLikeNotFound_AndChangeNothingAsync`                   | Fact   | Cross tenant read confirm delete behave like not found and change nothing.                       |
| `InstanceCannotReferenceADatasetOfAnotherTenant_OrAnUnknownDataSource_MassAssignmentAsync` | Fact   | Instance cannot reference a dataset of another tenant or an unknown data source mass assignment. |
| `Confirm_ServerOwnedFieldsInTheBody_AreNotHonouredAsync`                                   | Fact   | Confirm server owned fields in the body are not honoured.                                        |
| `HostileFiles_AreRefusedWithoutServerErrors_AndStoreNothingAsync`                          | Theory | Hostile files are refused without server errors and store nothing.                               |
| `FormulaCells_AreStoredAsDataAndEchoedOnlyAsJsonAsync`                                     | Fact   | Formula cells are stored as data and echoed only as json.                                        |
| `ContentTypeTampering_PartAndEnvelope_AreRefusedAsync`                                     | Fact   | Content type tampering part and envelope are refused.                                            |
| `FileNames_TraversalAndDoubleExtensions_AreNeutralisedOrRefusedAsync`                      | Fact   | File names traversal and double extensions are neutralised or refused.                           |
| `SuppliedNames_InjectionIsStoredAsData_MarkupIsRefusedAsync`                               | Fact   | Supplied names injection is stored as data markup is refused.                                    |
| `OversizedBodies_AreRefusedQuicklyAsync`                                                   | Fact   | Oversized bodies are refused quickly.                                                            |
| `ChunkedUploadWithoutContentLength_IsStillBounded_AndNeverServerErrorsAsync`               | Fact   | Chunked upload without content length is still bounded and never server errors.                  |
| `VerbTampering_OnlyTheDeclaredMethodsWorkAsync`                                            | Fact   | Verb tampering only the declared methods work.                                                   |
| `RouteAndBodyAbuse_NeverServerErrorsAsync`                                                 | Fact   | Route and body abuse never server errors.                                                        |
| `ConcurrentUploads_NeverServerError_AndEachStoresCompletelyOrNotAtAllAsync`                | Fact   | Concurrent uploads never server error and each stores completely or not at all.                  |
| `ErrorResponses_DoNotLeakPathsStackTracesOrSecretsAsync`                                   | Fact   | Error responses do not leak paths stack traces or secrets.                                       |

### PenTestExhaustiveInstanceJson

`Jube.Test.Security.Exhaustive.PenTestExhaustiveInstanceJson` — 20 tests. Category: PenTest. Collection: Database.

| Test                                                                                   | Kind   | Verifies                                                                                  |
|----------------------------------------------------------------------------------------|--------|-------------------------------------------------------------------------------------------|
| `AuthenticationAndAuthorisationMatrix_RefusesEveryone_ButThePermittedUserAsync`        | Fact   | Authentication and authorisation matrix refuses everyone but the permitted user.          |
| `UploadRawJsonThenConfirm_WorksEndToEndThroughTheRealPipelineAsync`                    | Fact   | Upload raw json then confirm works end to end through the real pipeline.                  |
| `UploadColumnarAndMultipartJsonFile_AreAcceptedAsync`                                  | Fact   | Upload columnar and multipart json file are accepted.                                     |
| `KindConfusion_ExtensionAndContentMustAgree_AndIdsDoNotCrossKindsAsync`                | Fact   | Kind confusion extension and content must agree and ids do not cross kinds.               |
| `CrossTenant_ReadConfirmDelete_BehaveLikeNotFound_AndChangeNothingAsync`               | Fact   | Cross tenant read confirm delete behave like not found and change nothing.                |
| `InstanceCannotReferenceAForeignOrAmbiguousJsonDataset_MassAssignmentAsync`            | Fact   | Instance cannot reference a foreign or ambiguous json dataset mass assignment.            |
| `Confirm_ServerOwnedFieldsInTheBody_AreNotHonouredAsync`                               | Fact   | Confirm server owned fields in the body are not honoured.                                 |
| `HostileRawBodies_AreRefusedWithoutServerErrors_AndStoreNothingAsync`                  | Theory | Hostile raw bodies are refused without server errors and store nothing.                   |
| `HostileJsonFiles_ThroughMultipart_AreRefusedWithoutServerErrors_AndStoreNothingAsync` | Theory | Hostile json files through multipart are refused without server errors and store nothing. |
| `ContentTypeAndEncodingTampering_AreRefusedAsync`                                      | Fact   | Content type and encoding tampering are refused.                                          |
| `SourceLabels_TraversalAndMarkupAreRefused_InjectionIsStoredAsDataAsync`               | Fact   | Source labels traversal and markup are refused injection is stored as data.               |
| `ReservedAndPrototypeKeys_AreStoredAsDataWithSafeVariableNamesAsync`                   | Fact   | Reserved and prototype keys are stored as data with safe variable names.                  |
| `SuppliedNames_InjectionIsStoredAsData_MarkupIsRefusedAsync`                           | Fact   | Supplied names injection is stored as data markup is refused.                             |
| `OversizedBodies_AreRefusedQuicklyAsync`                                               | Fact   | Oversized bodies are refused quickly.                                                     |
| `ChunkedRawUploadWithoutContentLength_IsStillBounded_AndNeverServerErrorsAsync`        | Fact   | Chunked raw upload without content length is still bounded and never server errors.       |
| `VerbTampering_OnlyTheDeclaredMethodsWorkAsync`                                        | Fact   | Verb tampering only the declared methods work.                                            |
| `RouteAndBodyAbuse_NeverServerErrorsAsync`                                             | Fact   | Route and body abuse never server errors.                                                 |
| `ConcurrentUploads_NeverServerError_AndEachStoresCompletelyOrNotAtAllAsync`            | Fact   | Concurrent uploads never server error and each stores completely or not at all.           |
| `ErrorResponses_DoNotLeakPathsStackTracesOrSecretsAsync`                               | Fact   | Error responses do not leak paths stack traces or secrets.                                |
| `CombinedListing_ShowsBothKindsScopedToTheCallerTenantAsync`                           | Fact   | Combined listing shows both kinds scoped to the caller tenant.                            |

## Model

### PenTestEntityAnalysisInlineScript

`Jube.Test.Security.Model.PenTestEntityAnalysisInlineScript` — 4 tests. Category: PenTest. Collection: Database.

| Test                                                           | Kind | Verifies                                                         |
|----------------------------------------------------------------|------|------------------------------------------------------------------|
| `PermissionMatrix_OnlyThePermittedUserReadsTheCatalogueAsync`  | Fact | Permission matrix only the permitted user reads the catalogue.   |
| `MutatingVerbs_AreNotRoutedAndLeaveTheCatalogueUnchangedAsync` | Fact | Mutating verbs are not routed and leave the catalogue unchanged. |
| `HostileQueryStringsAndHeaders_NeverServerErrorAsync`          | Fact | Hostile query strings and headers never server error.            |
| `Catalogue_ContainsNoSecretsOrTenantDataAsync`                 | Fact | Catalogue contains no secrets or tenant data.                    |

### PenTestEntityAnalysisModel

`Jube.Test.Security.Model.PenTestEntityAnalysisModel` — 14 tests. Category: PenTest. Collection: Database.
| `OversizedBodies_AreRefusedQuicklyAsync`                                            | Fact | Oversized bodies are
refused quickly. |
| `ConcurrentUpdatesAndDoubleSubmit_NeverServerError_AndStayConsistentAsync`          | Fact | Concurrent updates and
double submit never server error and stay consistent. |
| `LockedRows_CannotBeUpdatedOrDeletedAsync`                                          | Fact | Locked rows cannot be
updated or deleted. |
| `RouteAndQueryParameterAbuse_NeverServerErrors_OrTouchesRowsAsync`                  | Fact | Route and query parameter
abuse never server errors or touches rows. |
| `MalformedBodiesAndContentTypes_AreRejectedWithoutServerErrorsAsync`                | Fact | Malformed bodies and
content types are rejected without server errors. |
| `TypeConfusionAndBoundaries_NeverProduceServerErrors_OrSideEffectsAsync`            | Fact | Type confusion and
boundaries never produce server errors or side effects. |
| `LengthBoundaries_AcceptTheMaximum_AndRefuseOneMoreAsync`                           | Fact | Length boundaries accept
the maximum and refuse one more. |
| `JsonColumns_RefuseInvalidJson_InsteadOfFailingInTheDatabaseAsync`                  | Fact | Json columns refuse
invalid json instead of failing in the database. |
| `Injection_EveryTextField_IsStoredOrRejectedAsDataAsync`                            | Fact | Injection every text
field is stored or rejected as data. |
| `MassAssignment_ServerOwnedFields_AreNotHonouredAsync`                              | Fact | Mass assignment server
owned fields are not honoured. |
| `CrossTenantParent_CreateUnderForeignParent_AndMoveToForeignParent_AreRefusedAsync` | Fact | Cross tenant parent
create under foreign parent and move to foreign parent are refused. |
| `CrossTenant_ReadUpdateDeleteReset_BehaveLikeNotFoundAsync`                         | Fact | Cross tenant read update
delete reset behave like not found. |
| `Lifecycle_CreateReadUpdateDelete_HasSoftDeleteAndVersionSemanticsAsync`            | Fact | Lifecycle create read
update delete has soft delete and version semantics. |
| `AuthenticationAndAuthorisationMatrix_RefusesEveryone_ButThePermittedUserAsync`     | Fact | Authentication and
authorisation matrix refuses everyone but the permitted user. |

| Test | Kind | Verifies |
|------|------|----------|

### PenTestEntityAnalysisModelAbstractionCalculation

`Jube.Test.Security.Model.PenTestEntityAnalysisModelAbstractionCalculation` — 14 tests. Category: PenTest. Collection:
| `OversizedBodies_AreRefusedQuicklyAsync`                                            | Fact | Oversized bodies are
refused quickly. |
| `ConcurrentUpdatesAndDoubleSubmit_NeverServerError_AndStayConsistentAsync`          | Fact | Concurrent updates and
double submit never server error and stay consistent. |
| `LockedRows_CannotBeUpdatedOrDeletedAsync`                                          | Fact | Locked rows cannot be
updated or deleted. |
| `RouteAndQueryParameterAbuse_NeverServerErrors_OrTouchesRowsAsync`                  | Fact | Route and query parameter
abuse never server errors or touches rows. |
| `MalformedBodiesAndContentTypes_AreRejectedWithoutServerErrorsAsync`                | Fact | Malformed bodies and
content types are rejected without server errors. |
| `TypeConfusionAndBoundaries_NeverProduceServerErrors_OrSideEffectsAsync`            | Fact | Type confusion and
boundaries never produce server errors or side effects. |
| `LengthBoundaries_AcceptTheMaximum_AndRefuseOneMoreAsync`                           | Fact | Length boundaries accept
the maximum and refuse one more. |
| `JsonColumns_RefuseInvalidJson_InsteadOfFailingInTheDatabaseAsync`                  | Fact | Json columns refuse
invalid json instead of failing in the database. |
| `Injection_EveryTextField_IsStoredOrRejectedAsDataAsync`                            | Fact | Injection every text
field is stored or rejected as data. |
| `MassAssignment_ServerOwnedFields_AreNotHonouredAsync`                              | Fact | Mass assignment server
owned fields are not honoured. |
| `CrossTenantParent_CreateUnderForeignParent_AndMoveToForeignParent_AreRefusedAsync` | Fact | Cross tenant parent
create under foreign parent and move to foreign parent are refused. |
| `CrossTenant_ReadUpdateDeleteReset_BehaveLikeNotFoundAsync`                         | Fact | Cross tenant read update
delete reset behave like not found. |
| `Lifecycle_CreateReadUpdateDelete_HasSoftDeleteAndVersionSemanticsAsync`            | Fact | Lifecycle create read
update delete has soft delete and version semantics. |
| `AuthenticationAndAuthorisationMatrix_RefusesEveryone_ButThePermittedUserAsync`     | Fact | Authentication and
authorisation matrix refuses everyone but the permitted user. |
Database.

| Test | Kind | Verifies |
|------|------|----------|

### PenTestEntityAnalysisModelAbstractionRule

`Jube.Test.Security.Model.PenTestEntityAnalysisModelAbstractionRule` — 14 tests. Category: PenTest. Collection:
| `OversizedBodies_AreRefusedQuicklyAsync`                                            | Fact | Oversized bodies are
refused quickly. |
| `ConcurrentUpdatesAndDoubleSubmit_NeverServerError_AndStayConsistentAsync`          | Fact | Concurrent updates and
double submit never server error and stay consistent. |
| `LockedRows_CannotBeUpdatedOrDeletedAsync`                                          | Fact | Locked rows cannot be
updated or deleted. |
| `RouteAndQueryParameterAbuse_NeverServerErrors_OrTouchesRowsAsync`                  | Fact | Route and query parameter
abuse never server errors or touches rows. |
| `MalformedBodiesAndContentTypes_AreRejectedWithoutServerErrorsAsync`                | Fact | Malformed bodies and
content types are rejected without server errors. |
| `TypeConfusionAndBoundaries_NeverProduceServerErrors_OrSideEffectsAsync`            | Fact | Type confusion and
boundaries never produce server errors or side effects. |
| `LengthBoundaries_AcceptTheMaximum_AndRefuseOneMoreAsync`                           | Fact | Length boundaries accept
the maximum and refuse one more. |
| `JsonColumns_RefuseInvalidJson_InsteadOfFailingInTheDatabaseAsync`                  | Fact | Json columns refuse
invalid json instead of failing in the database. |
| `Injection_EveryTextField_IsStoredOrRejectedAsDataAsync`                            | Fact | Injection every text
field is stored or rejected as data. |
| `MassAssignment_ServerOwnedFields_AreNotHonouredAsync`                              | Fact | Mass assignment server
owned fields are not honoured. |
| `CrossTenantParent_CreateUnderForeignParent_AndMoveToForeignParent_AreRefusedAsync` | Fact | Cross tenant parent
create under foreign parent and move to foreign parent are refused. |
| `CrossTenant_ReadUpdateDeleteReset_BehaveLikeNotFoundAsync`                         | Fact | Cross tenant read update
delete reset behave like not found. |
| `Lifecycle_CreateReadUpdateDelete_HasSoftDeleteAndVersionSemanticsAsync`            | Fact | Lifecycle create read
update delete has soft delete and version semantics. |
| `AuthenticationAndAuthorisationMatrix_RefusesEveryone_ButThePermittedUserAsync`     | Fact | Authentication and
authorisation matrix refuses everyone but the permitted user. |
Database.

| Test | Kind | Verifies |
|------|------|----------|

### PenTestEntityAnalysisModelActivationRule

`Jube.Test.Security.Model.PenTestEntityAnalysisModelActivationRule` — 14 tests. Category: PenTest. Collection: Database.
| `OversizedBodies_AreRefusedQuicklyAsync`                                            | Fact | Oversized bodies are
refused quickly. |
| `ConcurrentUpdatesAndDoubleSubmit_NeverServerError_AndStayConsistentAsync`          | Fact | Concurrent updates and
double submit never server error and stay consistent. |
| `LockedRows_CannotBeUpdatedOrDeletedAsync`                                          | Fact | Locked rows cannot be
updated or deleted. |
| `RouteAndQueryParameterAbuse_NeverServerErrors_OrTouchesRowsAsync`                  | Fact | Route and query parameter
abuse never server errors or touches rows. |
| `MalformedBodiesAndContentTypes_AreRejectedWithoutServerErrorsAsync`                | Fact | Malformed bodies and
content types are rejected without server errors. |
| `TypeConfusionAndBoundaries_NeverProduceServerErrors_OrSideEffectsAsync`            | Fact | Type confusion and
boundaries never produce server errors or side effects. |
| `LengthBoundaries_AcceptTheMaximum_AndRefuseOneMoreAsync`                           | Fact | Length boundaries accept
the maximum and refuse one more. |
| `JsonColumns_RefuseInvalidJson_InsteadOfFailingInTheDatabaseAsync`                  | Fact | Json columns refuse
invalid json instead of failing in the database. |
| `Injection_EveryTextField_IsStoredOrRejectedAsDataAsync`                            | Fact | Injection every text
field is stored or rejected as data. |
| `MassAssignment_ServerOwnedFields_AreNotHonouredAsync`                              | Fact | Mass assignment server
owned fields are not honoured. |
| `CrossTenantParent_CreateUnderForeignParent_AndMoveToForeignParent_AreRefusedAsync` | Fact | Cross tenant parent
create under foreign parent and move to foreign parent are refused. |
| `CrossTenant_ReadUpdateDeleteReset_BehaveLikeNotFoundAsync`                         | Fact | Cross tenant read update
delete reset behave like not found. |
| `Lifecycle_CreateReadUpdateDelete_HasSoftDeleteAndVersionSemanticsAsync`            | Fact | Lifecycle create read
update delete has soft delete and version semantics. |
| `AuthenticationAndAuthorisationMatrix_RefusesEveryone_ButThePermittedUserAsync`     | Fact | Authentication and
authorisation matrix refuses everyone but the permitted user. |

| Test | Kind | Verifies |
|------|------|----------|

### PenTestEntityAnalysisModelActivationRuleOverride

`Jube.Test.Security.Model.PenTestEntityAnalysisModelActivationRuleOverride` — 14 tests. Category: PenTest. Collection:
| `OversizedBodies_AreRefusedQuicklyAsync`                                            | Fact | Oversized bodies are
refused quickly. |
| `ConcurrentUpdatesAndDoubleSubmit_NeverServerError_AndStayConsistentAsync`          | Fact | Concurrent updates and
double submit never server error and stay consistent. |
| `LockedRows_CannotBeUpdatedOrDeletedAsync`                                          | Fact | Locked rows cannot be
updated or deleted. |
| `RouteAndQueryParameterAbuse_NeverServerErrors_OrTouchesRowsAsync`                  | Fact | Route and query parameter
abuse never server errors or touches rows. |
| `MalformedBodiesAndContentTypes_AreRejectedWithoutServerErrorsAsync`                | Fact | Malformed bodies and
content types are rejected without server errors. |
| `TypeConfusionAndBoundaries_NeverProduceServerErrors_OrSideEffectsAsync`            | Fact | Type confusion and
boundaries never produce server errors or side effects. |
| `LengthBoundaries_AcceptTheMaximum_AndRefuseOneMoreAsync`                           | Fact | Length boundaries accept
the maximum and refuse one more. |
| `JsonColumns_RefuseInvalidJson_InsteadOfFailingInTheDatabaseAsync`                  | Fact | Json columns refuse
invalid json instead of failing in the database. |
| `Injection_EveryTextField_IsStoredOrRejectedAsDataAsync`                            | Fact | Injection every text
field is stored or rejected as data. |
| `MassAssignment_ServerOwnedFields_AreNotHonouredAsync`                              | Fact | Mass assignment server
owned fields are not honoured. |
| `CrossTenantParent_CreateUnderForeignParent_AndMoveToForeignParent_AreRefusedAsync` | Fact | Cross tenant parent
create under foreign parent and move to foreign parent are refused. |
| `CrossTenant_ReadUpdateDeleteReset_BehaveLikeNotFoundAsync`                         | Fact | Cross tenant read update
delete reset behave like not found. |
| `Lifecycle_CreateReadUpdateDelete_HasSoftDeleteAndVersionSemanticsAsync`            | Fact | Lifecycle create read
update delete has soft delete and version semantics. |
| `AuthenticationAndAuthorisationMatrix_RefusesEveryone_ButThePermittedUserAsync`     | Fact | Authentication and
authorisation matrix refuses everyone but the permitted user. |
Database.

| Test | Kind | Verifies |
|------|------|----------|

### PenTestEntityAnalysisModelBacktest

`Jube.Test.Security.Model.PenTestEntityAnalysisModelBacktest` — 6 tests.

| Test                                                      | Kind | Verifies                                                   |
|-----------------------------------------------------------|------|------------------------------------------------------------|
| `GetRoutes_RoleMatrixAsync`                               | Fact | Get routes role matrix.                                    |
| `PostRoutes_RoleMatrixAsync`                              | Fact | Post routes role matrix.                                   |
| `AnotherTenantsModelsAndRunsBehaveLikeAbsentOnesAsync`    | Fact | Another tenants models and runs behave like absent ones.   |
| `MalformedBodies_AreRefusedWithoutAServerErrorAsync`      | Fact | Malformed bodies are refused without a server error.       |
| `HostileRuleText_IsNeverPersistedOrExecutedAsync`         | Fact | Hostile rule text is never persisted or executed.          |
| `MassAssignment_TheServerDecidesUserTenantAndStatusAsync` | Fact | Mass assignment the server decides user tenant and status. |

### PenTestEntityAnalysisModelDictionary

`Jube.Test.Security.Model.PenTestEntityAnalysisModelDictionary` — 14 tests. Category: PenTest. Collection: Database.
| `OversizedBodies_AreRefusedQuicklyAsync`                                            | Fact | Oversized bodies are
refused quickly. |
| `ConcurrentUpdatesAndDoubleSubmit_NeverServerError_AndStayConsistentAsync`          | Fact | Concurrent updates and
double submit never server error and stay consistent. |
| `LockedRows_CannotBeUpdatedOrDeletedAsync`                                          | Fact | Locked rows cannot be
updated or deleted. |
| `RouteAndQueryParameterAbuse_NeverServerErrors_OrTouchesRowsAsync`                  | Fact | Route and query parameter
abuse never server errors or touches rows. |
| `MalformedBodiesAndContentTypes_AreRejectedWithoutServerErrorsAsync`                | Fact | Malformed bodies and
content types are rejected without server errors. |
| `TypeConfusionAndBoundaries_NeverProduceServerErrors_OrSideEffectsAsync`            | Fact | Type confusion and
boundaries never produce server errors or side effects. |
| `LengthBoundaries_AcceptTheMaximum_AndRefuseOneMoreAsync`                           | Fact | Length boundaries accept
the maximum and refuse one more. |
| `JsonColumns_RefuseInvalidJson_InsteadOfFailingInTheDatabaseAsync`                  | Fact | Json columns refuse
invalid json instead of failing in the database. |
| `Injection_EveryTextField_IsStoredOrRejectedAsDataAsync`                            | Fact | Injection every text
field is stored or rejected as data. |
| `MassAssignment_ServerOwnedFields_AreNotHonouredAsync`                              | Fact | Mass assignment server
owned fields are not honoured. |
| `CrossTenantParent_CreateUnderForeignParent_AndMoveToForeignParent_AreRefusedAsync` | Fact | Cross tenant parent
create under foreign parent and move to foreign parent are refused. |
| `CrossTenant_ReadUpdateDeleteReset_BehaveLikeNotFoundAsync`                         | Fact | Cross tenant read update
delete reset behave like not found. |
| `Lifecycle_CreateReadUpdateDelete_HasSoftDeleteAndVersionSemanticsAsync`            | Fact | Lifecycle create read
update delete has soft delete and version semantics. |
| `AuthenticationAndAuthorisationMatrix_RefusesEveryone_ButThePermittedUserAsync`     | Fact | Authentication and
authorisation matrix refuses everyone but the permitted user. |

| Test | Kind | Verifies |
|------|------|----------|

### PenTestEntityAnalysisModelDictionaryKvp

`Jube.Test.Security.Model.PenTestEntityAnalysisModelDictionaryKvp` — 20 tests. Category: PenTest. Collection: Database.

| Test                                                                                  | Kind | Verifies                                                                                                               |
|---------------------------------------------------------------------------------------|------|------------------------------------------------------------------------------------------------------------------------|
| `CsvUpload_AuthorisationAndCrossTenantDictionary_AreRefusedAsync`                     | Fact | Csv upload authorisation and cross tenant dictionary are refused.                                                      |
| `CsvUpload_ImportsCellsAsData_IncludingFormulaAndMarkupKeysAsync`                     | Fact | Csv upload imports cells as data including formula and markup keys.                                                    |
| `CsvUpload_MalformedRows_AreSkippedWithoutServerErrorAsync`                           | Fact | Csv upload malformed rows are skipped without server error.                                                            |
| `CsvUpload_HostileFileNames_AreStoredSanitisedAndNeverUsedAsPathsAsync`               | Fact | Csv upload hostile file names are stored sanitised and never used as paths.                                            |
| `CsvUpload_MultipartAttacks_AreRefusedWithoutServerErrorsAsync`                       | Fact | Csv upload multipart attacks are refused without server errors.                                                        |
| `CsvUpload_TheDictionaryIsNamedByTheRouteOnly_NeverByAFormFieldOrTheQueryStringAsync` | Fact | Csv upload names the dictionary by the route only, never by a form field or the query string, and stays tenant scoped. |
| `OversizedBodies_AreRefusedQuicklyAsync`                                              | Fact | Oversized bodies are refused quickly.                                                                                  |
| `ConcurrentUpdatesAndDoubleSubmit_NeverServerError_AndStayConsistentAsync`            | Fact | Concurrent updates and double submit never server error and stay consistent.                                           |
| `LockedRows_CannotBeUpdatedOrDeletedAsync`                                            | Fact | Locked rows cannot be updated or deleted.                                                                              |
| `RouteAndQueryParameterAbuse_NeverServerErrors_OrTouchesRowsAsync`                    | Fact | Route and query parameter abuse never server errors or touches rows.                                                   |
| `MalformedBodiesAndContentTypes_AreRejectedWithoutServerErrorsAsync`                  | Fact | Malformed bodies and content types are rejected without server errors.                                                 |
| `TypeConfusionAndBoundaries_NeverProduceServerErrors_OrSideEffectsAsync`              | Fact | Type confusion and boundaries never produce server errors or side effects.                                             |
| `LengthBoundaries_AcceptTheMaximum_AndRefuseOneMoreAsync`                             | Fact | Length boundaries accept the maximum and refuse one more.                                                              |
| `JsonColumns_RefuseInvalidJson_InsteadOfFailingInTheDatabaseAsync`                    | Fact | Json columns refuse invalid json instead of failing in the database.                                                   |
| `Injection_EveryTextField_IsStoredOrRejectedAsDataAsync`                              | Fact | Injection every text field is stored or rejected as data.                                                              |
| `MassAssignment_ServerOwnedFields_AreNotHonouredAsync`                                | Fact | Mass assignment server owned fields are not honoured.                                                                  |
| `CrossTenantParent_CreateUnderForeignParent_AndMoveToForeignParent_AreRefusedAsync`   | Fact | Cross tenant parent create under foreign parent and move to foreign parent are refused.                                |
| `CrossTenant_ReadUpdateDeleteReset_BehaveLikeNotFoundAsync`                           | Fact | Cross tenant read update delete reset behave like not found.                                                           |
| `Lifecycle_CreateReadUpdateDelete_HasSoftDeleteAndVersionSemanticsAsync`              | Fact | Lifecycle create read update delete has soft delete and version semantics.                                             |
| `AuthenticationAndAuthorisationMatrix_RefusesEveryone_ButThePermittedUserAsync`       | Fact | Authentication and authorisation matrix refuses everyone but the permitted user.                                       |

### PenTestEntityAnalysisModelGatewayRule

`Jube.Test.Security.Model.PenTestEntityAnalysisModelGatewayRule` — 14 tests. Category: PenTest. Collection: Database.
| `OversizedBodies_AreRefusedQuicklyAsync`                                            | Fact | Oversized bodies are
refused quickly. |
| `ConcurrentUpdatesAndDoubleSubmit_NeverServerError_AndStayConsistentAsync`          | Fact | Concurrent updates and
double submit never server error and stay consistent. |
| `LockedRows_CannotBeUpdatedOrDeletedAsync`                                          | Fact | Locked rows cannot be
updated or deleted. |
| `RouteAndQueryParameterAbuse_NeverServerErrors_OrTouchesRowsAsync`                  | Fact | Route and query parameter
abuse never server errors or touches rows. |
| `MalformedBodiesAndContentTypes_AreRejectedWithoutServerErrorsAsync`                | Fact | Malformed bodies and
content types are rejected without server errors. |
| `TypeConfusionAndBoundaries_NeverProduceServerErrors_OrSideEffectsAsync`            | Fact | Type confusion and
boundaries never produce server errors or side effects. |
| `LengthBoundaries_AcceptTheMaximum_AndRefuseOneMoreAsync`                           | Fact | Length boundaries accept
the maximum and refuse one more. |
| `JsonColumns_RefuseInvalidJson_InsteadOfFailingInTheDatabaseAsync`                  | Fact | Json columns refuse
invalid json instead of failing in the database. |
| `Injection_EveryTextField_IsStoredOrRejectedAsDataAsync`                            | Fact | Injection every text
field is stored or rejected as data. |
| `MassAssignment_ServerOwnedFields_AreNotHonouredAsync`                              | Fact | Mass assignment server
owned fields are not honoured. |
| `CrossTenantParent_CreateUnderForeignParent_AndMoveToForeignParent_AreRefusedAsync` | Fact | Cross tenant parent
create under foreign parent and move to foreign parent are refused. |
| `CrossTenant_ReadUpdateDeleteReset_BehaveLikeNotFoundAsync`                         | Fact | Cross tenant read update
delete reset behave like not found. |
| `Lifecycle_CreateReadUpdateDelete_HasSoftDeleteAndVersionSemanticsAsync`            | Fact | Lifecycle create read
update delete has soft delete and version semantics. |
| `AuthenticationAndAuthorisationMatrix_RefusesEveryone_ButThePermittedUserAsync`     | Fact | Authentication and
authorisation matrix refuses everyone but the permitted user. |

| Test | Kind | Verifies |
|------|------|----------|

### PenTestSvirfdLookup

`Jube.Test.Security.Model.PenTestSvirfdLookup` — 4 tests (5 cases). Category: PenTest. Collection: PenTest.

| Test                                                          | Kind   | Verifies                                                                                                                                                                            |
|---------------------------------------------------------------|--------|-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| `AuthenticationAndPermissionMatrixAsync`                      | Theory | Lookup and Status: anonymous, garbage bearer, no permission, no tenant and unknown user are rejected; both tenants are served; write verbs are refused.                             |
| `TheLookupIsTheSameForEveryTenantAndCarriesNoTenantDataAsync` | Fact   | An address record does not depend on the tenant and carries no tenant data.                                                                                                         |
| `HostileAddressesAreRefusedWithoutAServerErrorOrAnEchoAsync`  | Fact   | Private, loopback, link-local, IPv6 forms, numeric forms, oversize, unicode digits, CRLF, SQL and script strings are refused, never a 5xx other than 503, never echoed or injected. |
| `NoSecretIsReturnedAndStatusCannotBeSteeredByTheRequestAsync` | Fact   | No API key or config in responses; query and body cannot change the status; writes are refused.                                                                                     |

### PenTestEntityAnalysisModelSvirfd

`Jube.Test.Security.Model.PenTestEntityAnalysisModelSvirfd` — 1 test. Category: PenTest. Collection: Database.

| Test                                                                                | Kind | Verifies                                                                                   |
|-------------------------------------------------------------------------------------|------|--------------------------------------------------------------------------------------------|
| `Field_MustBeAnIpTypedRequestXPathOfTheSameModel_AndNeverOneOfAnotherTenantAsync`   | Fact | Field Must Be An Ip Typed Request XPath Of The Same Model And Never One Of Another Tenant. |
| `OversizedBodies_AreRefusedQuicklyAsync`                                            | Fact | Oversized Bodies Are Refused Quickly.                                                      |
| `ConcurrentUpdatesAndDoubleSubmit_NeverServerError_AndStayConsistentAsync`          | Fact | Concurrent Updates And Double Submit Never Server Error And Stay Consistent.               |
| `LockedRows_CannotBeUpdatedOrDeletedAsync`                                          | Fact | Locked Rows Cannot Be Updated Or Deleted.                                                  |
| `RouteAndQueryParameterAbuse_NeverServerErrors_OrTouchesRowsAsync`                  | Fact | Route And Query Parameter Abuse Never Server Errors Or Touches Rows.                       |
| `MalformedBodiesAndContentTypes_AreRejectedWithoutServerErrorsAsync`                | Fact | Malformed Bodies And Content Types Are Rejected Without Server Errors.                     |
| `TypeConfusionAndBoundaries_NeverProduceServerErrors_OrSideEffectsAsync`            | Fact | Type Confusion And Boundaries Never Produce Server Errors Or Side Effects.                 |
| `LengthBoundaries_AcceptTheMaximum_AndRefuseOneMoreAsync`                           | Fact | Length Boundaries Accept The Maximum And Refuse One More.                                  |
| `JsonColumns_RefuseInvalidJson_InsteadOfFailingInTheDatabaseAsync`                  | Fact | Json Columns Refuse Invalid Json Instead Of Failing In The Database.                       |
| `Injection_EveryTextField_IsStoredOrRejectedAsDataAsync`                            | Fact | Injection Every Text Field Is Stored Or Rejected As Data.                                  |
| `MassAssignment_ServerOwnedFields_AreNotHonouredAsync`                              | Fact | Mass Assignment Server Owned Fields Are Not Honoured.                                      |
| `CrossTenantParent_CreateUnderForeignParent_AndMoveToForeignParent_AreRefusedAsync` | Fact | Cross Tenant Parent Create Under Foreign Parent And Move To Foreign Parent Are Refused.    |
| `CrossTenant_ReadUpdateDeleteReset_BehaveLikeNotFoundAsync`                         | Fact | Cross Tenant Read Update Delete Reset Behave Like Not Found.                               |
| `Lifecycle_CreateReadUpdateDelete_HasSoftDeleteAndVersionSemanticsAsync`            | Fact | Lifecycle Create Read Update Delete Has Soft Delete And Version Semantics.                 |
| `AuthenticationAndAuthorisationMatrix_RefusesEveryone_ButThePermittedUserAsync`     | Fact | Authentication And Authorisation Matrix Refuses Everyone But The Permitted User.           |

### PenTestEntityAnalysisModelGraphRecall

`Jube.Test.Security.Model.PenTestEntityAnalysisModelGraphRecall` — 1 test. Category: PenTest. Collection: Database.

| Test                                                                                | Kind | Verifies                                                                                |
|-------------------------------------------------------------------------------------|------|-----------------------------------------------------------------------------------------|
| `Counterpart_MustNameAnotherEntityOfTheSameModel_AndNeverOneOfAnotherTenantAsync`   | Fact | Counterpart must name another entity of the same model and never one of another tenant. |
| `OversizedBodies_AreRefusedQuicklyAsync`                                            | Fact | Oversized bodies are refused quickly.                                                   |
| `ConcurrentUpdatesAndDoubleSubmit_NeverServerError_AndStayConsistentAsync`          | Fact | Concurrent updates and double submit never server error and stay consistent.            |
| `LockedRows_CannotBeUpdatedOrDeletedAsync`                                          | Fact | Locked rows cannot be updated or deleted.                                               |
| `RouteAndQueryParameterAbuse_NeverServerErrors_OrTouchesRowsAsync`                  | Fact | Route and query parameter abuse never server errors or touches rows.                    |
| `MalformedBodiesAndContentTypes_AreRejectedWithoutServerErrorsAsync`                | Fact | Malformed bodies and content types are rejected without server errors.                  |
| `TypeConfusionAndBoundaries_NeverProduceServerErrors_OrSideEffectsAsync`            | Fact | Type confusion and boundaries never produce server errors or side effects.              |
| `LengthBoundaries_AcceptTheMaximum_AndRefuseOneMoreAsync`                           | Fact | Length boundaries accept the maximum and refuse one more.                               |
| `JsonColumns_RefuseInvalidJson_InsteadOfFailingInTheDatabaseAsync`                  | Fact | Json columns refuse invalid json instead of failing in the database.                    |
| `Injection_EveryTextField_IsStoredOrRejectedAsDataAsync`                            | Fact | Injection every text field is stored or rejected as data.                               |
| `MassAssignment_ServerOwnedFields_AreNotHonouredAsync`                              | Fact | Mass assignment server owned fields are not honoured.                                   |
| `CrossTenantParent_CreateUnderForeignParent_AndMoveToForeignParent_AreRefusedAsync` | Fact | Cross tenant parent create under foreign parent and move to foreign parent are refused. |
| `CrossTenant_ReadUpdateDeleteReset_BehaveLikeNotFoundAsync`                         | Fact | Cross tenant read update delete reset behave like not found.                            |
| `Lifecycle_CreateReadUpdateDelete_HasSoftDeleteAndVersionSemanticsAsync`            | Fact | Lifecycle create read update delete has soft delete and version semantics.              |
| `AuthenticationAndAuthorisationMatrix_RefusesEveryone_ButThePermittedUserAsync`     | Fact | Authentication and authorisation matrix refuses everyone but the permitted user.        |

### PenTestEntityAnalysisModelHttpAdaptation

`Jube.Test.Security.Model.PenTestEntityAnalysisModelHttpAdaptation` — 1 test. Category: PenTest. Collection: Database.

| Test                                                                                | Kind | Verifies                                                                                |
|-------------------------------------------------------------------------------------|------|-----------------------------------------------------------------------------------------|
| `HttpEndpoint_OnlyPlainPathsOnTheConfiguredHost_AreAcceptedAsync`                   | Fact | Http endpoint only plain paths on the configured host are accepted.                     |
| `OversizedBodies_AreRefusedQuicklyAsync`                                            | Fact | Oversized bodies are refused quickly.                                                   |
| `ConcurrentUpdatesAndDoubleSubmit_NeverServerError_AndStayConsistentAsync`          | Fact | Concurrent updates and double submit never server error and stay consistent.            |
| `LockedRows_CannotBeUpdatedOrDeletedAsync`                                          | Fact | Locked rows cannot be updated or deleted.                                               |
| `RouteAndQueryParameterAbuse_NeverServerErrors_OrTouchesRowsAsync`                  | Fact | Route and query parameter abuse never server errors or touches rows.                    |
| `MalformedBodiesAndContentTypes_AreRejectedWithoutServerErrorsAsync`                | Fact | Malformed bodies and content types are rejected without server errors.                  |
| `TypeConfusionAndBoundaries_NeverProduceServerErrors_OrSideEffectsAsync`            | Fact | Type confusion and boundaries never produce server errors or side effects.              |
| `LengthBoundaries_AcceptTheMaximum_AndRefuseOneMoreAsync`                           | Fact | Length boundaries accept the maximum and refuse one more.                               |
| `JsonColumns_RefuseInvalidJson_InsteadOfFailingInTheDatabaseAsync`                  | Fact | Json columns refuse invalid json instead of failing in the database.                    |
| `Injection_EveryTextField_IsStoredOrRejectedAsDataAsync`                            | Fact | Injection every text field is stored or rejected as data.                               |
| `MassAssignment_ServerOwnedFields_AreNotHonouredAsync`                              | Fact | Mass assignment server owned fields are not honoured.                                   |
| `CrossTenantParent_CreateUnderForeignParent_AndMoveToForeignParent_AreRefusedAsync` | Fact | Cross tenant parent create under foreign parent and move to foreign parent are refused. |
| `CrossTenant_ReadUpdateDeleteReset_BehaveLikeNotFoundAsync`                         | Fact | Cross tenant read update delete reset behave like not found.                            |
| `Lifecycle_CreateReadUpdateDelete_HasSoftDeleteAndVersionSemanticsAsync`            | Fact | Lifecycle create read update delete has soft delete and version semantics.              |
| `AuthenticationAndAuthorisationMatrix_RefusesEveryone_ButThePermittedUserAsync`     | Fact | Authentication and authorisation matrix refuses everyone but the permitted user.        |

### PenTestExhaustiveSearchInstance

`Jube.Test.Security.Model.PenTestExhaustiveSearchInstance` — 18 tests. Category: PenTest. Collection: Database.

| Test                                                                                | Kind | Verifies                                                                                |
|-------------------------------------------------------------------------------------|------|-----------------------------------------------------------------------------------------|
| `Stop_ForeignGuid_LooksLikeMissing_AndDoesNotTouchTheRowAsync`                      | Fact | Stop foreign guid looks like missing and does not touch the row.                        |
| `SetActive_ForeignId_LooksLikeMissing_AndDoesNotTouchTheRowAsync`                   | Fact | Set active foreign id looks like missing and does not touch the row.                    |
| `FilterDefinition_MustBeAPlainBooleanExpression_AndValidJsonAsync`                  | Fact | Filter definition must be a plain boolean expression and valid json.                    |
| `AnomalyDetectorFields_AreValidated_AndTheReportIsServerWrittenAsync`               | Fact | Anomaly detector fields are validated and the report is server written.                 |
| `OversizedBodies_AreRefusedQuicklyAsync`                                            | Fact | Oversized bodies are refused quickly.                                                   |
| `ConcurrentUpdatesAndDoubleSubmit_NeverServerError_AndStayConsistentAsync`          | Fact | Concurrent updates and double submit never server error and stay consistent.            |
| `LockedRows_CannotBeUpdatedOrDeletedAsync`                                          | Fact | Locked rows cannot be updated or deleted.                                               |
| `RouteAndQueryParameterAbuse_NeverServerErrors_OrTouchesRowsAsync`                  | Fact | Route and query parameter abuse never server errors or touches rows.                    |
| `MalformedBodiesAndContentTypes_AreRejectedWithoutServerErrorsAsync`                | Fact | Malformed bodies and content types are rejected without server errors.                  |
| `TypeConfusionAndBoundaries_NeverProduceServerErrors_OrSideEffectsAsync`            | Fact | Type confusion and boundaries never produce server errors or side effects.              |
| `LengthBoundaries_AcceptTheMaximum_AndRefuseOneMoreAsync`                           | Fact | Length boundaries accept the maximum and refuse one more.                               |
| `JsonColumns_RefuseInvalidJson_InsteadOfFailingInTheDatabaseAsync`                  | Fact | Json columns refuse invalid json instead of failing in the database.                    |
| `Injection_EveryTextField_IsStoredOrRejectedAsDataAsync`                            | Fact | Injection every text field is stored or rejected as data.                               |
| `MassAssignment_ServerOwnedFields_AreNotHonouredAsync`                              | Fact | Mass assignment server owned fields are not honoured.                                   |
| `CrossTenantParent_CreateUnderForeignParent_AndMoveToForeignParent_AreRefusedAsync` | Fact | Cross tenant parent create under foreign parent and move to foreign parent are refused. |
| `CrossTenant_ReadUpdateDeleteReset_BehaveLikeNotFoundAsync`                         | Fact | Cross tenant read update delete reset behave like not found.                            |
| `Lifecycle_CreateReadUpdateDelete_HasSoftDeleteAndVersionSemanticsAsync`            | Fact | Lifecycle create read update delete has soft delete and version semantics.              |
| `AuthenticationAndAuthorisationMatrix_RefusesEveryone_ButThePermittedUserAsync`     | Fact | Authentication and authorisation matrix refuses everyone but the permitted user.        |

### PenTestModelInlineFunction

`Jube.Test.Security.Model.PenTestModelInlineFunction` — 11 tests. Category: PenTest. Collection: Database.
| `Consumption_ListsAndParametersAreBoundedAsync`            | Fact | Consumption lists and parameters are bounded. |
| `Limits_MaxLengthOversizeAndHostileExpressionsAsync`       | Fact | Limits max length oversize and hostile
expressions. |
| `BusinessFlow_DeleteReplayAndSoftDeleteAreConsistentAsync` | Fact | Business flow delete replay and soft delete are
consistent. |
| `RequestStructure_MalformedBodiesAndContentTypesAsync`     | Fact | Request structure malformed bodies and content
types. |
| `TypeConfusion_EveryFieldEveryRawValueAsync`               | Fact | Type confusion every field every raw value. |
| `Injection_RouteAndQueryParametersAsync`                   | Fact | Injection route and query parameters. |
| `Injection_StringFieldsStayDataAndRoundTripAsync`          | Fact | Injection string fields stay data and round
trip. |
| `MassAssignment_ServerOwnedFieldsAreNotHonouredAsync`      | Fact | Mass assignment server owned fields are not
honoured. |
| `Bola_ForeignParentReferencesAreRefusedAsync`              | Fact | Bola foreign parent references are refused. |
| `Bola_ForeignTenantIdsBehaveLikeAbsentIdsAsync`            | Fact | Bola foreign tenant ids behave like absent ids. |
| `Matrix_EveryRouteEveryRoleAsync`                          | Fact | Matrix every route every role. |

| Test | Kind | Verifies |
|------|------|----------|

### PenTestModelInlineScript

`Jube.Test.Security.Model.PenTestModelInlineScript` — 13 tests. Category: PenTest. Collection: Database.

| Test                                                       | Kind | Verifies                                                    |
|------------------------------------------------------------|------|-------------------------------------------------------------|
| `InlineScriptReference_MustExistAndBeAnIntegerAsync`       | Fact | Inline script reference must exist and be an integer.       |
| `CreatedRowExposesNoScriptSourceAsync`                     | Fact | Created row exposes no script source.                       |
| `Consumption_ListsAndParametersAreBoundedAsync`            | Fact | Consumption lists and parameters are bounded.               |
| `Limits_MaxLengthOversizeAndHostileExpressionsAsync`       | Fact | Limits max length oversize and hostile expressions.         |
| `BusinessFlow_DeleteReplayAndSoftDeleteAreConsistentAsync` | Fact | Business flow delete replay and soft delete are consistent. |
| `RequestStructure_MalformedBodiesAndContentTypesAsync`     | Fact | Request structure malformed bodies and content types.       |
| `TypeConfusion_EveryFieldEveryRawValueAsync`               | Fact | Type confusion every field every raw value.                 |
| `Injection_RouteAndQueryParametersAsync`                   | Fact | Injection route and query parameters.                       |
| `Injection_StringFieldsStayDataAndRoundTripAsync`          | Fact | Injection string fields stay data and round trip.           |
| `MassAssignment_ServerOwnedFieldsAreNotHonouredAsync`      | Fact | Mass assignment server owned fields are not honoured.       |
| `Bola_ForeignParentReferencesAreRefusedAsync`              | Fact | Bola foreign parent references are refused.                 |
| `Bola_ForeignTenantIdsBehaveLikeAbsentIdsAsync`            | Fact | Bola foreign tenant ids behave like absent ids.             |
| `Matrix_EveryRouteEveryRoleAsync`                          | Fact | Matrix every route every role.                              |

### PenTestModelIntegrityQueries

`Jube.Test.Security.Model.PenTestModelIntegrityQueries` — 4 tests.

| Test                                                       | Kind | Verifies                                                  |
|------------------------------------------------------------|------|-----------------------------------------------------------|
| `ModelRoutes_RoleMatrixAsync`                              | Fact | Model routes role matrix.                                 |
| `AnotherTenantsModelBehavesLikeAnAbsentModelAsync`         | Fact | Another tenants model behaves like an absent model.       |
| `RouteParameterTampering_IsRefusedQuietlyAsync`            | Fact | Route parameter tampering is refused quietly.             |
| `OperatorCatalogue_IsAuthenticatedStaticMetadataOnlyAsync` | Fact | Operator catalogue is authenticated static metadata only. |

### PenTestModelList

`Jube.Test.Security.Model.PenTestModelList` — 11 tests. Category: PenTest. Collection: Database.
| `Consumption_ListsAndParametersAreBoundedAsync`            | Fact | Consumption lists and parameters are bounded. |
| `Limits_MaxLengthOversizeAndHostileExpressionsAsync`       | Fact | Limits max length oversize and hostile
expressions. |
| `BusinessFlow_DeleteReplayAndSoftDeleteAreConsistentAsync` | Fact | Business flow delete replay and soft delete are
consistent. |
| `RequestStructure_MalformedBodiesAndContentTypesAsync`     | Fact | Request structure malformed bodies and content
types. |
| `TypeConfusion_EveryFieldEveryRawValueAsync`               | Fact | Type confusion every field every raw value. |
| `Injection_RouteAndQueryParametersAsync`                   | Fact | Injection route and query parameters. |
| `Injection_StringFieldsStayDataAndRoundTripAsync`          | Fact | Injection string fields stay data and round
trip. |
| `MassAssignment_ServerOwnedFieldsAreNotHonouredAsync`      | Fact | Mass assignment server owned fields are not
honoured. |
| `Bola_ForeignParentReferencesAreRefusedAsync`              | Fact | Bola foreign parent references are refused. |
| `Bola_ForeignTenantIdsBehaveLikeAbsentIdsAsync`            | Fact | Bola foreign tenant ids behave like absent ids. |
| `Matrix_EveryRouteEveryRoleAsync`                          | Fact | Matrix every route every role. |

| Test | Kind | Verifies |
|------|------|----------|

### PenTestModelListValue

`Jube.Test.Security.Model.PenTestModelListValue` — 19 tests. Category: PenTest. Collection: Database.

| Test                                                        | Kind | Verifies                                                         |
|-------------------------------------------------------------|------|------------------------------------------------------------------|
| `ByParentRead_WithoutAnId_IsAnEmptyListNotABadRequestAsync` | Fact | By parent read without an id is an empty list not a bad request. |
| `Values_ExpiryDatesDuplicatesAndUnicodeAsync`               | Fact | Values expiry dates duplicates and unicode.                      |
| `Values_DeleteByIdIsTenantScopedAndBodyIdIsIgnoredAsync`    | Fact | Values delete by id is tenant scoped and body id is ignored.     |
| `Csv_AuthenticationAuthorisationAndTenantScopeAsync`        | Fact | Csv authentication authorisation and tenant scope.               |
| `Csv_FileNamesTraversalAndContentTypesAsync`                | Fact | Csv file names traversal and content types.                      |
| `Csv_ContentAttacksAreRowErrorsOrInertDataAsync`            | Fact | Csv content attacks are row errors or inert data.                |
| `Csv_HugeFilesManyFilesAndAtomicRollbackAsync`              | Fact | Csv huge files many files and atomic rollback.                   |
| `Csv_MalformedMultipartAndCsrfAsync`                        | Fact | Csv malformed multipart and csrf.                                |
| `Consumption_ListsAndParametersAreBoundedAsync`             | Fact | Consumption lists and parameters are bounded.                    |
| `Limits_MaxLengthOversizeAndHostileExpressionsAsync`        | Fact | Limits max length oversize and hostile expressions.              |
| `BusinessFlow_DeleteReplayAndSoftDeleteAreConsistentAsync`  | Fact | Business flow delete replay and soft delete are consistent.      |
| `RequestStructure_MalformedBodiesAndContentTypesAsync`      | Fact | Request structure malformed bodies and content types.            |
| `TypeConfusion_EveryFieldEveryRawValueAsync`                | Fact | Type confusion every field every raw value.                      |
| `Injection_RouteAndQueryParametersAsync`                    | Fact | Injection route and query parameters.                            |
| `Injection_StringFieldsStayDataAndRoundTripAsync`           | Fact | Injection string fields stay data and round trip.                |
| `MassAssignment_ServerOwnedFieldsAreNotHonouredAsync`       | Fact | Mass assignment server owned fields are not honoured.            |
| `Bola_ForeignParentReferencesAreRefusedAsync`               | Fact | Bola foreign parent references are refused.                      |
| `Bola_ForeignTenantIdsBehaveLikeAbsentIdsAsync`             | Fact | Bola foreign tenant ids behave like absent ids.                  |
| `Matrix_EveryRouteEveryRoleAsync`                           | Fact | Matrix every route every role.                                   |

### PenTestModelOverride

`Jube.Test.Security.Model.PenTestModelOverride` — 13 tests. Category: PenTest. Collection: Database.

| Test                                                                  | Kind | Verifies                                                                                    |
|-----------------------------------------------------------------------|------|---------------------------------------------------------------------------------------------|
| `ForceOverrides_NeedTheSeparateForcePermission_OnEveryWritePathAsync` | Fact | POST and PUT of a Force override by a user without permission 62 are 403 and store nothing. |
| `ExpiryDate_PastFarFutureAndCrossTenantAreHandledAsync`               | Fact | Expiry date past far future and cross tenant are handled.                                   |
| `Consumption_ListsAndParametersAreBoundedAsync`                       | Fact | Consumption lists and parameters are bounded.                                               |
| `Limits_MaxLengthOversizeAndHostileExpressionsAsync`                  | Fact | Limits max length oversize and hostile expressions.                                         |
| `BusinessFlow_DeleteReplayAndSoftDeleteAreConsistentAsync`            | Fact | Business flow delete replay and soft delete are consistent.                                 |
| `RequestStructure_MalformedBodiesAndContentTypesAsync`                | Fact | Request structure malformed bodies and content types.                                       |
| `TypeConfusion_EveryFieldEveryRawValueAsync`                          | Fact | Type confusion every field every raw value.                                                 |
| `Injection_RouteAndQueryParametersAsync`                              | Fact | Injection route and query parameters.                                                       |
| `Injection_StringFieldsStayDataAndRoundTripAsync`                     | Fact | Injection string fields stay data and round trip.                                           |
| `MassAssignment_ServerOwnedFieldsAreNotHonouredAsync`                 | Fact | Mass assignment server owned fields are not honoured.                                       |
| `Bola_ForeignParentReferencesAreRefusedAsync`                         | Fact | Bola foreign parent references are refused.                                                 |
| `Bola_ForeignTenantIdsBehaveLikeAbsentIdsAsync`                       | Fact | Bola foreign tenant ids behave like absent ids.                                             |
| `Matrix_EveryRouteEveryRoleAsync`                                     | Fact | Matrix every route every role.                                                              |

### PenTestModelReprocessingRule

`Jube.Test.Security.Model.PenTestModelReprocessingRule` — 1 test. Category: PenTest. Collection: Database.

| Test                                                            | Kind | Verifies                                                         |
|-----------------------------------------------------------------|------|------------------------------------------------------------------|
| `Window_ReprocessingValueCannotOverflowTheEngineDateMathsAsync` | Fact | Window reprocessing value cannot overflow the engine date maths. |
| `Consumption_ListsAndParametersAreBoundedAsync`                 | Fact | Consumption lists and parameters are bounded.                    |
| `Limits_MaxLengthOversizeAndHostileExpressionsAsync`            | Fact | Limits max length oversize and hostile expressions.              |
| `BusinessFlow_DeleteReplayAndSoftDeleteAreConsistentAsync`      | Fact | Business flow delete replay and soft delete are consistent.      |
| `RequestStructure_MalformedBodiesAndContentTypesAsync`          | Fact | Request structure malformed bodies and content types.            |
| `TypeConfusion_EveryFieldEveryRawValueAsync`                    | Fact | Type confusion every field every raw value.                      |
| `Injection_RouteAndQueryParametersAsync`                        | Fact | Injection route and query parameters.                            |
| `Injection_StringFieldsStayDataAndRoundTripAsync`               | Fact | Injection string fields stay data and round trip.                |
| `MassAssignment_ServerOwnedFieldsAreNotHonouredAsync`           | Fact | Mass assignment server owned fields are not honoured.            |
| `Bola_ForeignParentReferencesAreRefusedAsync`                   | Fact | Bola foreign parent references are refused.                      |
| `Bola_ForeignTenantIdsBehaveLikeAbsentIdsAsync`                 | Fact | Bola foreign tenant ids behave like absent ids.                  |
| `Matrix_EveryRouteEveryRoleAsync`                               | Fact | Matrix every route every role.                                   |

### PenTestModelReprocessingRuleInstance

`Jube.Test.Security.Model.PenTestModelReprocessingRuleInstance` — 14 tests. Category: PenTest. Collection: Database.

| Test                                                         | Kind | Verifies                                                        |
|--------------------------------------------------------------|------|-----------------------------------------------------------------|
| `Trigger_OnlyForOwnTenantsRuleAndServerAssignsRunStateAsync` | Fact | Trigger only for own tenants rule and server assigns run state. |
| `RunState_EnumsCountsAndDatesAreBoundedAsync`                | Fact | Run state enums counts and dates are bounded.                   |
| `Update_CannotHijackOrResurrectAnInstanceAsync`              | Fact | Update cannot hijack or resurrect an instance.                  |
| `Consumption_ListsAndParametersAreBoundedAsync`              | Fact | Consumption lists and parameters are bounded.                   |
| `Limits_MaxLengthOversizeAndHostileExpressionsAsync`         | Fact | Limits max length oversize and hostile expressions.             |
| `BusinessFlow_DeleteReplayAndSoftDeleteAreConsistentAsync`   | Fact | Business flow delete replay and soft delete are consistent.     |
| `RequestStructure_MalformedBodiesAndContentTypesAsync`       | Fact | Request structure malformed bodies and content types.           |
| `TypeConfusion_EveryFieldEveryRawValueAsync`                 | Fact | Type confusion every field every raw value.                     |
| `Injection_RouteAndQueryParametersAsync`                     | Fact | Injection route and query parameters.                           |
| `Injection_StringFieldsStayDataAndRoundTripAsync`            | Fact | Injection string fields stay data and round trip.               |
| `MassAssignment_ServerOwnedFieldsAreNotHonouredAsync`        | Fact | Mass assignment server owned fields are not honoured.           |
| `Bola_ForeignParentReferencesAreRefusedAsync`                | Fact | Bola foreign parent references are refused.                     |
| `Bola_ForeignTenantIdsBehaveLikeAbsentIdsAsync`              | Fact | Bola foreign tenant ids behave like absent ids.                 |
| `Matrix_EveryRouteEveryRoleAsync`                            | Fact | Matrix every route every role.                                  |

### PenTestModelRequestXPath

`Jube.Test.Security.Model.PenTestModelRequestXPath` — 14 tests. Category: PenTest. Collection: Database.

| Test                                                         | Kind | Verifies                                                      |
|--------------------------------------------------------------|------|---------------------------------------------------------------|
| `Window_SearchKeyWindowsAndLimitsAreBoundedAsync`            | Fact | Window search key windows and limits are bounded.             |
| `ExtraRoutes_ForeignParentsAndHostileArgumentsAreInertAsync` | Fact | Extra routes foreign parents and hostile arguments are inert. |
| `XPathExpressions_AreStoredAsInertDataAsync`                 | Fact | X path expressions are stored as inert data.                  |
| `Consumption_ListsAndParametersAreBoundedAsync`              | Fact | Consumption lists and parameters are bounded.                 |
| `Limits_MaxLengthOversizeAndHostileExpressionsAsync`         | Fact | Limits max length oversize and hostile expressions.           |
| `BusinessFlow_DeleteReplayAndSoftDeleteAreConsistentAsync`   | Fact | Business flow delete replay and soft delete are consistent.   |
| `RequestStructure_MalformedBodiesAndContentTypesAsync`       | Fact | Request structure malformed bodies and content types.         |
| `TypeConfusion_EveryFieldEveryRawValueAsync`                 | Fact | Type confusion every field every raw value.                   |
| `Injection_RouteAndQueryParametersAsync`                     | Fact | Injection route and query parameters.                         |
| `Injection_StringFieldsStayDataAndRoundTripAsync`            | Fact | Injection string fields stay data and round trip.             |
| `MassAssignment_ServerOwnedFieldsAreNotHonouredAsync`        | Fact | Mass assignment server owned fields are not honoured.         |
| `Bola_ForeignParentReferencesAreRefusedAsync`                | Fact | Bola foreign parent references are refused.                   |
| `Bola_ForeignTenantIdsBehaveLikeAbsentIdsAsync`              | Fact | Bola foreign tenant ids behave like absent ids.               |
| `Matrix_EveryRouteEveryRoleAsync`                            | Fact | Matrix every route every role.                                |

### PenTestModelResponseElevationRange

`Jube.Test.Security.Model.PenTestModelResponseElevationRange` — 11 tests. Category: PenTest. Collection: Database.
| `Consumption_ListsAndParametersAreBoundedAsync`            | Fact | Consumption lists and parameters are bounded. |
| `Limits_MaxLengthOversizeAndHostileExpressionsAsync`       | Fact | Limits max length oversize and hostile
expressions. |
| `BusinessFlow_DeleteReplayAndSoftDeleteAreConsistentAsync` | Fact | Business flow delete replay and soft delete are
consistent. |
| `RequestStructure_MalformedBodiesAndContentTypesAsync`     | Fact | Request structure malformed bodies and content
types. |
| `TypeConfusion_EveryFieldEveryRawValueAsync`               | Fact | Type confusion every field every raw value. |
| `Injection_RouteAndQueryParametersAsync`                   | Fact | Injection route and query parameters. |
| `Injection_StringFieldsStayDataAndRoundTripAsync`          | Fact | Injection string fields stay data and round
trip. |
| `MassAssignment_ServerOwnedFieldsAreNotHonouredAsync`      | Fact | Mass assignment server owned fields are not
honoured. |
| `Bola_ForeignParentReferencesAreRefusedAsync`              | Fact | Bola foreign parent references are refused. |
| `Bola_ForeignTenantIdsBehaveLikeAbsentIdsAsync`            | Fact | Bola foreign tenant ids behave like absent ids. |
| `Matrix_EveryRouteEveryRoleAsync`                          | Fact | Matrix every route every role. |

| Test | Kind | Verifies |
|------|------|----------|

### PenTestModelSanction

`Jube.Test.Security.Model.PenTestModelSanction` — 1 test. Category: PenTest. Collection: Database.

| Test                                                       | Kind | Verifies                                                    |
|------------------------------------------------------------|------|-------------------------------------------------------------|
| `Window_CacheValueCannotOverflowTheEngineDateMathsAsync`   | Fact | Window cache value cannot overflow the engine date maths.   |
| `Consumption_ListsAndParametersAreBoundedAsync`            | Fact | Consumption lists and parameters are bounded.               |
| `Limits_MaxLengthOversizeAndHostileExpressionsAsync`       | Fact | Limits max length oversize and hostile expressions.         |
| `BusinessFlow_DeleteReplayAndSoftDeleteAreConsistentAsync` | Fact | Business flow delete replay and soft delete are consistent. |
| `RequestStructure_MalformedBodiesAndContentTypesAsync`     | Fact | Request structure malformed bodies and content types.       |
| `TypeConfusion_EveryFieldEveryRawValueAsync`               | Fact | Type confusion every field every raw value.                 |
| `Injection_RouteAndQueryParametersAsync`                   | Fact | Injection route and query parameters.                       |
| `Injection_StringFieldsStayDataAndRoundTripAsync`          | Fact | Injection string fields stay data and round trip.           |
| `MassAssignment_ServerOwnedFieldsAreNotHonouredAsync`      | Fact | Mass assignment server owned fields are not honoured.       |
| `Bola_ForeignParentReferencesAreRefusedAsync`              | Fact | Bola foreign parent references are refused.                 |
| `Bola_ForeignTenantIdsBehaveLikeAbsentIdsAsync`            | Fact | Bola foreign tenant ids behave like absent ids.             |
| `Matrix_EveryRouteEveryRoleAsync`                          | Fact | Matrix every route every role.                              |

### PenTestModelTag

`Jube.Test.Security.Model.PenTestModelTag` — 11 tests. Category: PenTest. Collection: Database.
| `Consumption_ListsAndParametersAreBoundedAsync`            | Fact | Consumption lists and parameters are bounded. |
| `Limits_MaxLengthOversizeAndHostileExpressionsAsync`       | Fact | Limits max length oversize and hostile
expressions. |
| `BusinessFlow_DeleteReplayAndSoftDeleteAreConsistentAsync` | Fact | Business flow delete replay and soft delete are
consistent. |
| `RequestStructure_MalformedBodiesAndContentTypesAsync`     | Fact | Request structure malformed bodies and content
types. |
| `TypeConfusion_EveryFieldEveryRawValueAsync`               | Fact | Type confusion every field every raw value. |
| `Injection_RouteAndQueryParametersAsync`                   | Fact | Injection route and query parameters. |
| `Injection_StringFieldsStayDataAndRoundTripAsync`          | Fact | Injection string fields stay data and round
trip. |
| `MassAssignment_ServerOwnedFieldsAreNotHonouredAsync`      | Fact | Mass assignment server owned fields are not
honoured. |
| `Bola_ForeignParentReferencesAreRefusedAsync`              | Fact | Bola foreign parent references are refused. |
| `Bola_ForeignTenantIdsBehaveLikeAbsentIdsAsync`            | Fact | Bola foreign tenant ids behave like absent ids. |
| `Matrix_EveryRouteEveryRoleAsync`                          | Fact | Matrix every route every role. |

| Test | Kind | Verifies |
|------|------|----------|

### PenTestModelTtlCounter

`Jube.Test.Security.Model.PenTestModelTtlCounter` — 13 tests. Category: PenTest. Collection: Database.

| Test                                                          | Kind | Verifies                                                        |
|---------------------------------------------------------------|------|-----------------------------------------------------------------|
| `Window_TtlCounterValueCannotOverflowTheEngineDateMathsAsync` | Fact | Window ttl counter value cannot overflow the engine date maths. |
| `ByGuid_ForeignAndMalformedGuidsBehaveLikeAbsentAsync`        | Fact | By guid foreign and malformed guids behave like absent.         |
| `Consumption_ListsAndParametersAreBoundedAsync`               | Fact | Consumption lists and parameters are bounded.                   |
| `Limits_MaxLengthOversizeAndHostileExpressionsAsync`          | Fact | Limits max length oversize and hostile expressions.             |
| `BusinessFlow_DeleteReplayAndSoftDeleteAreConsistentAsync`    | Fact | Business flow delete replay and soft delete are consistent.     |
| `RequestStructure_MalformedBodiesAndContentTypesAsync`        | Fact | Request structure malformed bodies and content types.           |
| `TypeConfusion_EveryFieldEveryRawValueAsync`                  | Fact | Type confusion every field every raw value.                     |
| `Injection_RouteAndQueryParametersAsync`                      | Fact | Injection route and query parameters.                           |
| `Injection_StringFieldsStayDataAndRoundTripAsync`             | Fact | Injection string fields stay data and round trip.               |
| `MassAssignment_ServerOwnedFieldsAreNotHonouredAsync`         | Fact | Mass assignment server owned fields are not honoured.           |
| `Bola_ForeignParentReferencesAreRefusedAsync`                 | Fact | Bola foreign parent references are refused.                     |
| `Bola_ForeignTenantIdsBehaveLikeAbsentIdsAsync`               | Fact | Bola foreign tenant ids behave like absent ids.                 |
| `Matrix_EveryRouteEveryRoleAsync`                             | Fact | Matrix every route every role.                                  |

## Monitoring

### PenTestOpenTelemetryExclude

`Jube.Test.Security.Monitoring.PenTestOpenTelemetryExclude` — 9 tests. Category: PenTest. Collection: Database.

| Test                                                                   | Kind   | Verifies                                                                 |
|------------------------------------------------------------------------|--------|--------------------------------------------------------------------------|
| `Matrix_EveryRoleAndCredentialFormAsync`                               | Theory | Matrix every role and credential form.                                   |
| `Authorised_ListAndGet_ShapeAndIdSemanticsAsync`                       | Fact   | Authorised list and get shape and id semantics.                          |
| `Create_MassAssignmentAndForbiddenFieldsAreNotHonouredAsync`           | Fact   | Create mass assignment and forbidden fields are not honoured.            |
| `Update_MassAssignmentAndForbiddenFieldsAreNotHonouredAsync`           | Fact   | Update mass assignment and forbidden fields are not honoured.            |
| `Lifecycle_LockedDeletedReplayAndConcurrencyAsync`                     | Fact   | Lifecycle locked deleted replay and concurrency.                         |
| `Name_InjectionCharactersRoundTripFaithfullyOrAreRejectedAsync`        | Fact   | Name injection characters round trip faithfully or are rejected.         |
| `Bodies_TypeConfusionBoundariesAndMalformedInputNeverServerErrorAsync` | Fact   | Bodies type confusion boundaries and malformed input never server error. |
| `Name_LengthBoundariesAndUpdateValidationAsync`                        | Fact   | Name length boundaries and update validation.                            |
| `Verbs_UnsupportedMethodsAndRouteTricksAsync`                          | Fact   | Verbs unsupported methods and route tricks.                              |

### PenTestOpenTelemetryLogCounter

`Jube.Test.Security.Monitoring.PenTestOpenTelemetryLogCounter` — 8 tests. Category: PenTest. Collection: Database.

| Test                                                           | Kind   | Verifies                                                        |
|----------------------------------------------------------------|--------|-----------------------------------------------------------------|
| `Matrix_EveryRoleAndCredentialFormAsync`                       | Theory | Matrix every role and credential form.                          |
| `Authorised_ReadShapeAndUnknownIdsAsync`                       | Fact   | Authorised read shape and unknown ids.                          |
| `MassAssignment_CreateAndUpdateAsync`                          | Fact   | Mass assignment create and update.                              |
| `Name_MustBeStrictInstrumentNameIncludingTrailingNewlineAsync` | Fact   | Name must be strict instrument name including trailing newline. |
| `Regex_ValidationBoundsAndInjectionAsync`                      | Fact   | Regex validation bounds and injection.                          |
| `Lifecycle_LockedDeletedReplayInjectionAndConcurrencyAsync`    | Fact   | Lifecycle locked deleted replay injection and concurrency.      |
| `Bodies_TypeConfusionAndMalformedInputNeverServerErrorAsync`   | Fact   | Bodies type confusion and malformed input never server error.   |
| `Regex_StoredMarkupAndSqlAreReturnedAsInertDataAsync`          | Fact   | Regex stored markup and sql are returned as inert data.         |

### PenTestPlatformMonitoring

`Jube.Test.Security.Monitoring.PenTestPlatformMonitoring` — 4 tests. Category: PenTest. Collection: Database.

| Test                                                                    | Kind   | Verifies                                                                   |
|-------------------------------------------------------------------------|--------|----------------------------------------------------------------------------|
| `AuthorizationMatrixForAreaAsync`                                       | Theory | Authorization matrix for area.                                             |
| `ParameterAbuseForAreaAsync`                                            | Theory | Parameter abuse for area.                                                  |
| `ConsumptionAndVerbsForAreaAsync`                                       | Theory | Consumption and verbs for area.                                            |
| `PatroniClusterEvent_StoredCredentialsAreRedactedAndMarkupIsInertAsync` | Fact   | Patroni cluster event stored credentials are redacted and markup is inert. |

### PenTestPostgresMonitoring

`Jube.Test.Security.Monitoring.PenTestPostgresMonitoring` — 6 tests. Category: PenTest. Collection: Database.

| Test                                                                 | Kind   | Verifies                                                                |
|----------------------------------------------------------------------|--------|-------------------------------------------------------------------------|
| `AuthorizationMatrixForAreaAsync`                                    | Theory | Authorization matrix for area.                                          |
| `ParameterAbuseForAreaAsync`                                         | Theory | Parameter abuse for area.                                               |
| `ConsumptionAndVerbsForAreaAsync`                                    | Theory | Consumption and verbs for area.                                         |
| `PostgresActivity_LiveQueryCredentialsAreRedactedAsync`              | Fact   | Postgres activity live query credentials are redacted.                  |
| `PostgresLogEntry_StoredCredentialsAreRedactedAndMarkupIsInertAsync` | Fact   | Postgres log entry stored credentials are redacted and markup is inert. |
| `SortFieldAndSortDirectionCannotAlterTheRowSetAsync`                 | Theory | Sort field and sort direction cannot alter the row set.                 |

### PenTestQueryParsing

`Jube.Test.Security.Monitoring.PenTestQueryParsing` — 4 tests. Category: PenTest. Collection: Database.

| Test                                                                | Kind   | Verifies                                                               |
|---------------------------------------------------------------------|--------|------------------------------------------------------------------------|
| `ParseNullableInt_TreatsHostileTextAsNoFilter`                      | Theory | Parse nullable int treats hostile text as no filter.                   |
| `ParseNullableGuid_OnlyAcceptsGuids`                                | Theory | Parse nullable guid only accepts guids.                                |
| `Parsers_NeverThrowAndAreCultureIndependentAsync`                   | Fact   | Parsers never throw and are culture independent.                       |
| `TypeFilterEndpoints_GarbageBehavesLikeNoFilterAndNeverErrorsAsync` | Theory | Type filter endpoints garbage behaves like no filter and never errors. |

### PenTestRespMonitoring

`Jube.Test.Security.Monitoring.PenTestRespMonitoring` — 5 tests. Category: PenTest. Collection: Database.

| Test                                                          | Kind   | Verifies                                                       |
|---------------------------------------------------------------|--------|----------------------------------------------------------------|
| `AuthorizationMatrixForAreaAsync`                             | Theory | Authorization matrix for area.                                 |
| `ParameterAbuseForAreaAsync`                                  | Theory | Parameter abuse for area.                                      |
| `ConsumptionAndVerbsForAreaAsync`                             | Theory | Consumption and verbs for area.                                |
| `RespSlowOperation_AuthenticationArgumentsAreRedactedAsync`   | Fact   | Redis slow operation authentication arguments are redacted.    |
| `RespConnectionAndSentinelEvents_CredentialsAreRedactedAsync` | Fact   | Redis connection and sentinel events credentials are redacted. |

### PenTestSensitiveTextRedactor

`Jube.Test.Security.Monitoring.PenTestSensitiveTextRedactor` — 6 tests. Category: PenTest.

| Test                                         | Kind   | Verifies                                          |
|----------------------------------------------|--------|---------------------------------------------------|
| `Redact_MasksCredentialShapes`               | Theory | Redact masks credential shapes.                   |
| `RedactRespCommand_MasksCredentialArguments` | Theory | Redact redis command masks credential arguments.  |
| `RedactRespKeyName_AuthKeyIsTheCredential`   | Theory | Redact redis key name auth key is the credential. |
| `Redact_LeavesOrdinaryTextUntouched`         | Theory | Redact leaves ordinary text untouched.            |
| `Redact_NullIsNull`                          | Fact   | Redact null is null.                              |
| `Redact_HostileInputIsFastAndNeverThrows`    | Fact   | Redact hostile input is fast and never throws.    |

## Observability

### PenTestObservabilityContent

`Jube.Test.Security.Observability.PenTestObservabilityContent` — 4 tests.

| Test                                               | Kind   | Verifies                                            |
|----------------------------------------------------|--------|-----------------------------------------------------|
| `StoredHostileText_IsReturnedAsInertJsonDataAsync` | Theory | Stored hostile text is returned as inert json data. |
| `SecretsInStoredText_AreNotReturnedAsync`          | Theory | Secrets in stored text are not returned.            |
| `TenantScopedRows_AreLandlordOnlyAsync`            | Theory | Tenant scoped rows are landlord only.               |
| `ModelInvokeWarning_IsLandlordOnlyAsync`           | Fact   | Model invoke warning is landlord only.              |

### PenTestObservabilityLists

`Jube.Test.Security.Observability.PenTestObservabilityLists` — 8 tests.

| Test                                                       | Kind   | Verifies                                                   |
|------------------------------------------------------------|--------|------------------------------------------------------------|
| `PermissionMatrix_MatchesSpecAsync`                        | Theory | Permission matrix matches spec.                            |
| `NonGetVerbs_AreNotServedAsync`                            | Theory | Non get verbs are not served.                              |
| `SortFieldAndDirection_AreWhitelistedNotConcatenatedAsync` | Theory | Sort field and direction are whitelisted not concatenated. |
| `Search_IsInertData_NeverSqlOrMarkupAsync`                 | Theory | Search is inert data never sql or markup.                  |
| `IdFilters_TypeConfusionAndInjectionAsync`                 | Theory | Id filters type confusion and injection.                   |
| `Paging_LimitAbuse_IsClampedAndBoundedAsync`               | Theory | Paging limit abuse is clamped and bounded.                 |
| `DateRangeAndSample_ParameterAbuseAsync`                   | Theory | Date range and sample parameter abuse.                     |
| `QueryStructure_PollutionOversizeAndRouteVariantsAsync`    | Theory | Query structure pollution oversize and route variants.     |

## Query case

### PenTestCaseByCaseKeyValue

`Jube.Test.Security.QueryCase.PenTestCaseByCaseKeyValue` — 8 tests. Category: PenTest. Collection: Database.
| `ReplayAndConcurrentRequestsAreConsistentAsync`                      | Fact | Replay and concurrent requests are
consistent. |
| `OtherVerbsNeverSucceedAndDoNotChangeTheRowSetAsync`                 | Fact | Other verbs never succeed and do not
change the row set. |
| `ResponseExposesOnlyTheDocumentedPropertiesAsync`                    | Fact | Response exposes only the documented
properties. |
| `ParameterTypeConfusionAndBoundariesAsync`                           | Fact | Parameter type confusion and
boundaries. |
| `InjectionInKeyAndValueNeverMatchesLeaksOrDelaysAsync`               | Fact | Injection in key and value never matches
leaks or delays. |
| `CaseVisibilityRequiresWorkflowRoleAndStatusRoleAndLiveParentsAsync` | Fact | Case visibility requires workflow role
and status role and live parents. |
| `ExactMatchOnlyAndTenantIsolationForIdenticalKeyValueAsync`          | Fact | Exact match only and tenant isolation
for identical key value. |
| `AuthenticationAndAuthorisationMatrixAsync`                          | Fact | Authentication and authorisation
matrix. |

| Test | Kind | Verifies |
|------|------|----------|

### PenTestCaseById

`Jube.Test.Security.QueryCase.PenTestCaseById` — 7 tests. Category: PenTest. Collection: Database.

| Test                                                                          | Kind | Verifies                                                                         |
|-------------------------------------------------------------------------------|------|----------------------------------------------------------------------------------|
| `AuthenticationAndAuthorisationMatrixWritesEventsOnlyForAuthorisedViewsAsync` | Fact | Authentication and authorisation matrix writes events only for authorised views. |
| `UnknownForeignAndInvisibleCasesAreIndistinguishableNotFoundAsync`            | Fact | Unknown foreign and invisible cases are indistinguishable not found.             |
| `ViewingRecordsOneEventPerViewAttributedToTheAuthenticatedUserOnlyAsync`      | Fact | Viewing records one event per view attributed to the authenticated user only.    |
| `RouteParameterAbuseIsRefusedWithoutSideEffectsAsync`                         | Fact | Route parameter abuse is refused without side effects.                           |
| `OtherVerbsNeverSucceedAndWriteNoEventsAsync`                                 | Fact | Other verbs never succeed and write no events.                                   |
| `StoredPayloadIsReturnedAsInertDataAndCorruptJsonDoesNotCrashAsync`           | Fact | Stored payload is returned as inert data and corrupt json does not crash.        |
| `ResponseShapeAndHeadersAsync`                                                | Fact | Response shape and headers.                                                      |

### PenTestCaseEventByCaseKeyValue

`Jube.Test.Security.QueryCase.PenTestCaseEventByCaseKeyValue` — 8 tests. Category: PenTest. Collection: Database.
| `ReplayAndConcurrentRequestsAreConsistentAsync`                      | Fact | Replay and concurrent requests are
consistent. |
| `OtherVerbsNeverSucceedAndDoNotChangeTheRowSetAsync`                 | Fact | Other verbs never succeed and do not
change the row set. |
| `ResponseExposesOnlyTheDocumentedPropertiesAsync`                    | Fact | Response exposes only the documented
properties. |
| `ParameterTypeConfusionAndBoundariesAsync`                           | Fact | Parameter type confusion and
boundaries. |
| `InjectionInKeyAndValueNeverMatchesLeaksOrDelaysAsync`               | Fact | Injection in key and value never matches
leaks or delays. |
| `CaseVisibilityRequiresWorkflowRoleAndStatusRoleAndLiveParentsAsync` | Fact | Case visibility requires workflow role
and status role and live parents. |
| `ExactMatchOnlyAndTenantIsolationForIdenticalKeyValueAsync`          | Fact | Exact match only and tenant isolation
for identical key value. |
| `AuthenticationAndAuthorisationMatrixAsync`                          | Fact | Authentication and authorisation
matrix. |

| Test | Kind | Verifies |
|------|------|----------|

### PenTestCaseNoteByCaseKeyValue

`Jube.Test.Security.QueryCase.PenTestCaseNoteByCaseKeyValue` — 8 tests. Category: PenTest. Collection: Database.
| `ReplayAndConcurrentRequestsAreConsistentAsync`                      | Fact | Replay and concurrent requests are
consistent. |
| `OtherVerbsNeverSucceedAndDoNotChangeTheRowSetAsync`                 | Fact | Other verbs never succeed and do not
change the row set. |
| `ResponseExposesOnlyTheDocumentedPropertiesAsync`                    | Fact | Response exposes only the documented
properties. |
| `ParameterTypeConfusionAndBoundariesAsync`                           | Fact | Parameter type confusion and
boundaries. |
| `InjectionInKeyAndValueNeverMatchesLeaksOrDelaysAsync`               | Fact | Injection in key and value never matches
leaks or delays. |
| `CaseVisibilityRequiresWorkflowRoleAndStatusRoleAndLiveParentsAsync` | Fact | Case visibility requires workflow role
and status role and live parents. |
| `ExactMatchOnlyAndTenantIsolationForIdenticalKeyValueAsync`          | Fact | Exact match only and tenant isolation
for identical key value. |
| `AuthenticationAndAuthorisationMatrixAsync`                          | Fact | Authentication and authorisation
matrix. |

| Test | Kind | Verifies |
|------|------|----------|

### PenTestCaseWorkflowDisplayExecution

`Jube.Test.Security.QueryCase.PenTestCaseWorkflowDisplayExecution` — 6 tests. Category: PenTest. Collection: Database.

| Test                                                                 | Kind | Verifies                                                                 |
|----------------------------------------------------------------------|------|--------------------------------------------------------------------------|
| `AuthenticationAndAuthorisationMatrixAsync`                          | Fact | Authentication and authorisation matrix.                                 |
| `CaseDisplayAndTenantIsolationIsUniformAsync`                        | Fact | Case display and tenant isolation is uniform.                            |
| `PayloadValuesAreHtmlEncodedButTheTemplateStaysMarkupAsync`          | Fact | Payload values are html encoded but the template stays markup.           |
| `UnusualStoredPayloadShapesNeverCrashTheRendererAsync`               | Fact | Unusual stored payload shapes never crash the renderer.                  |
| `RequestBodyTypeConfusionAndMassAssignmentNeverCrashOrOverrideAsync` | Fact | Request body type confusion and mass assignment never crash or override. |
| `OtherVerbsNeverSucceedAndReplayIsConsistentAsync`                   | Fact | Other verbs never succeed and replay is consistent.                      |

### PenTestCaseWorkflowFormEntryByCaseKeyValue

`Jube.Test.Security.QueryCase.PenTestCaseWorkflowFormEntryByCaseKeyValue` — 8 tests. Category: PenTest. Collection:
| `ReplayAndConcurrentRequestsAreConsistentAsync`                      | Fact | Replay and concurrent requests are
consistent. |
| `OtherVerbsNeverSucceedAndDoNotChangeTheRowSetAsync`                 | Fact | Other verbs never succeed and do not
change the row set. |
| `ResponseExposesOnlyTheDocumentedPropertiesAsync`                    | Fact | Response exposes only the documented
properties. |
| `ParameterTypeConfusionAndBoundariesAsync`                           | Fact | Parameter type confusion and
boundaries. |
| `InjectionInKeyAndValueNeverMatchesLeaksOrDelaysAsync`               | Fact | Injection in key and value never matches
leaks or delays. |
| `CaseVisibilityRequiresWorkflowRoleAndStatusRoleAndLiveParentsAsync` | Fact | Case visibility requires workflow role
and status role and live parents. |
| `ExactMatchOnlyAndTenantIsolationForIdenticalKeyValueAsync`          | Fact | Exact match only and tenant isolation
for identical key value. |
| `AuthenticationAndAuthorisationMatrixAsync`                          | Fact | Authentication and authorisation
matrix. |
Database.

| Test | Kind | Verifies |
|------|------|----------|

### PenTestCaseWorkflowMacroExecution

`Jube.Test.Security.QueryCase.PenTestCaseWorkflowMacroExecution` — 8 tests. Category: PenTest. Collection: Database.

| Test                                                                                | Kind | Verifies                                                                                |
|-------------------------------------------------------------------------------------|------|-----------------------------------------------------------------------------------------|
| `AuthenticationAndAuthorisationMatrixReachesTheOutboundCallOnlyWhenAuthorisedAsync` | Fact | Authentication and authorisation matrix reaches the outbound call only when authorised. |
| `CaseMacroAndTenantIsolationNeverReachesTheOutboundCallAsync`                       | Fact | Case macro and tenant isolation never reaches the outbound call.                        |
| `OutboundPostCarriesTheCaseButNotTheRawJsonAndEchoIsMinimalAsync`                   | Fact | Outbound post carries the case but not the raw json and echo is minimal.                |
| `HostilePayloadValuesInTheEndpointCannotCrashOrInjectHeadersAsync`                  | Fact | Hostile payload values in the endpoint cannot crash or inject headers.                  |
| `ConfiguredEndpointSchemesAndUnreachableHostsAreSwallowedNotEscalatedAsync`         | Fact | Configured endpoint schemes and unreachable hosts are swallowed not escalated.          |
| `UnusualStoredPayloadShapesStillExecuteTheMacroAsync`                               | Fact | Unusual stored payload shapes still execute the macro.                                  |
| `RequestBodyTypeConfusionAndMassAssignmentNeverCrashOrExecuteTheWrongMacroAsync`    | Fact | Request body type confusion and mass assignment never crash or execute the wrong macro. |
| `VerbTamperingReplayAndConcurrencyAreConsistentAsync`                               | Fact | Verb tampering replay and concurrency are consistent.                                   |

### PenTestEntityAnalysisModelActivationRuleOverrideQuery

`Jube.Test.Security.QueryCase.PenTestEntityAnalysisModelActivationRuleOverrideQuery` — 6 tests. Category: PenTest.
| `ResponseShapeVerbsAndReplayAsync`                         | Fact | Response shape verbs and replay. |
| `ParameterBoundariesAndTypeConfusionAsync`                 | Fact | Parameter boundaries and type confusion. |
| `InjectionInKeyAndValueNeverMatchesLeaksOrDelaysAsync`     | Fact | Injection in key and value never matches leaks or
delays. |
| `OverrideStateRulesAreExactAndLiveOnlyAsync`               | Fact | Override state rules are exact and live only. |
| `TenantIsolationForIdenticalKeysAndForeignModelGuidsAsync` | Fact | Tenant isolation for identical keys and foreign
model guids. |
| `AuthenticationAndAuthorisationMatrixAsync`                | Fact | Authentication and authorisation matrix. |
Collection: Database.

| Test | Kind | Verifies |
|------|------|----------|

### PenTestEntityAnalysisModelOverrideQuery

`Jube.Test.Security.QueryCase.PenTestEntityAnalysisModelOverrideQuery` — 6 tests. Category: PenTest. Collection:
| `ResponseShapeVerbsAndReplayAsync`                         | Fact | Response shape verbs and replay. |
| `ParameterBoundariesAndTypeConfusionAsync`                 | Fact | Parameter boundaries and type confusion. |
| `InjectionInKeyAndValueNeverMatchesLeaksOrDelaysAsync`     | Fact | Injection in key and value never matches leaks or
delays. |
| `OverrideStateRulesAreExactAndLiveOnlyAsync`               | Fact | Override state rules are exact and live only. |
| `TenantIsolationForIdenticalKeysAndForeignModelGuidsAsync` | Fact | Tenant isolation for identical keys and foreign
model guids. |
| `AuthenticationAndAuthorisationMatrixAsync`                | Fact | Authentication and authorisation matrix. |
Database.

| Test | Kind | Verifies |
|------|------|----------|

### PenTestEntityAnalysisModelOverrideValuesQuery

`Jube.Test.Security.QueryCase.PenTestEntityAnalysisModelOverrideValuesQuery` — 5 tests. Category: PenTest. Collection:
Database.

| Test                                     | Kind | Verifies                                 |
|------------------------------------------|------|------------------------------------------|
| `IsPermissionedAndTenantScopedAsync`     | Fact | Is permissioned and tenant scoped.       |
| `ResponseShapeAndVerbsAsync`             | Fact | Response shape and verbs.                |
| `HostileOverrideKeysNeverRunOrLeakAsync` | Fact | Hostile override keys never run or leak. |
| `LimitIsClampedAndNeverFaultsAsync`      | Fact | Limit is clamped and never faults.       |
| `OnlyLiveOverridesAreListedAsync`        | Fact | Only live overrides are listed.          |

### PenTestEntityAnalysisModelSynchronisationNodeStatusEntries

`Jube.Test.Security.QueryCase.PenTestEntityAnalysisModelSynchronisationNodeStatusEntries` — 4 tests. Category: PenTest.
Collection: Database.

| Test                                                                        | Kind | Verifies                                                                           |
|-----------------------------------------------------------------------------|------|------------------------------------------------------------------------------------|
| `AuthenticationAndAuthorisationMatrixAsync`                                 | Fact | Authentication and authorisation matrix.                                           |
| `EachTenantSeesOnlyItsOwnFreshNodesAndParametersCannotChangeTheTenantAsync` | Fact | Each tenant sees only its own fresh nodes and parameters cannot change the tenant. |
| `ShapeVerbsAndReplayAreSafeAsync`                                           | Fact | Shape verbs and replay are safe.                                                   |
| `TenantWithoutAScheduleGetsNoContentAndNothingOfAnotherTenantAsync`         | Fact | Tenant without a schedule gets no content and nothing of another tenant.           |

### PenTestEntityAnalysisPotentialMultiPartStringNames

`Jube.Test.Security.QueryCase.PenTestEntityAnalysisPotentialMultiPartStringNames` — 5 tests. Category: PenTest.
Collection: Database.

| Test                                                                 | Kind | Verifies                                                               |
|----------------------------------------------------------------------|------|------------------------------------------------------------------------|
| `AuthenticationAndAuthorisationMatrixForBothOverloadsAsync`          | Fact | Authentication and authorisation matrix for both overloads.            |
| `CrossTenantModelReferencesAreIndistinguishableFromUnknownOnesAsync` | Fact | Cross tenant model references are indistinguishable from unknown ones. |
| `OnlyLiveStringFieldsAndFunctionsAreListedSortedAndDistinctAsync`    | Fact | Only live string fields and functions are listed sorted and distinct.  |
| `RouteParameterAbuseIsRefusedOrEmptyNeverAnErrorAsync`               | Fact | Route parameter abuse is refused or empty never an error.              |
| `ReplayAndConcurrencyAreConsistentPerTenantAsync`                    | Fact | Replay and concurrency are consistent per tenant.                      |

### PenTestEntityAnalysisRequestXPathInlineScriptNamesByStringIntegerFloatDataType

`Jube.Test.Security.QueryCase.PenTestEntityAnalysisRequestXPathInlineScriptNamesByStringIntegerFloatDataType` — 3 tests.
Category: PenTest. Collection: Database.

| Test                                                                        | Kind | Verifies                                                                        |
|-----------------------------------------------------------------------------|------|---------------------------------------------------------------------------------|
| `AuthenticationAndAuthorisationMatrixAsync`                                 | Fact | Authentication and authorisation matrix.                                        |
| `CrossTenantModelsAreIndistinguishableFromUnknownAndContentIsFilteredAsync` | Fact | Cross tenant models are indistinguishable from unknown and content is filtered. |
| `ResponseShapeRouteAbuseVerbsAndReplayAsync`                                | Fact | Response shape route abuse verbs and replay.                                    |

## Query exhaustive

### PenTestExhaustiveChartConfusion

`Jube.Test.Security.QueryExhaustive.PenTestExhaustiveChartConfusion` — 11 tests. Category: PenTest.
| `HostileVariableNames_AreEscapedAndNeverBecomeMarkupAsync`                 | Fact | Hostile variable names are escaped
and never become markup. |
| `VerbTampering_OnlyGetIsServedAsync`                                       | Fact | Verb tampering only get is
served. |
| `HostileQueryStringAndHeaders_NeverChangeTheChartAsync`                    | Fact | Hostile query string and headers
never change the chart. |
| `IdBoundariesAndTypeConfusion_AreRejectedOrEmptyNeverServerErrorsAsync`    | Fact | Id boundaries and type confusion
are rejected or empty never server errors. |
| `ConcurrentReads_AreDeterministicAndBoundedAsync`                          | Fact | Concurrent reads are deterministic
and bounded. |
| `NumericEdgeCases_NaNInfinityAndOverflowNeverBreakTheSvgAsync`             | Fact | Numeric edge cases na n infinity
and overflow never break the svg. |
| `EmptyStates_UnknownEmptyAndSoftDeletedAreByteForByteEqualAsync`           | Fact | Empty states unknown empty and
soft deleted are byte for byte equal. |
| `IdEnumeration_NeighbouringIdsAreOwnDataOrTheEmptyStateNeverForeignAsync`  | Fact | Id enumeration neighbouring ids
are own data or the empty state never foreign. |
| `CrossTenant_ForeignAndUnknownIdsAnswerIndistinguishablyAsync`             | Fact | Cross tenant foreign and unknown
ids answer indistinguishably. |
| `AuthenticationAndPermissionMatrix_RefusesEveryoneButAuthorisedUsersAsync` | Fact | Authentication and permission
matrix refuses everyone but authorised users. |
| `AuthorisedUser_ReadsOwnDataAsSafeSvgAsync`                                | Fact | Authorised user reads own data as
safe svg. |

| Test | Kind | Verifies |
|------|------|----------|

### PenTestExhaustiveChartErrorHistogram

`Jube.Test.Security.QueryExhaustive.PenTestExhaustiveChartErrorHistogram` — 11 tests. Category: PenTest.
| `HostileVariableNames_AreEscapedAndNeverBecomeMarkupAsync`                 | Fact | Hostile variable names are escaped
and never become markup. |
| `VerbTampering_OnlyGetIsServedAsync`                                       | Fact | Verb tampering only get is
served. |
| `HostileQueryStringAndHeaders_NeverChangeTheChartAsync`                    | Fact | Hostile query string and headers
never change the chart. |
| `IdBoundariesAndTypeConfusion_AreRejectedOrEmptyNeverServerErrorsAsync`    | Fact | Id boundaries and type confusion
are rejected or empty never server errors. |
| `ConcurrentReads_AreDeterministicAndBoundedAsync`                          | Fact | Concurrent reads are deterministic
and bounded. |
| `NumericEdgeCases_NaNInfinityAndOverflowNeverBreakTheSvgAsync`             | Fact | Numeric edge cases na n infinity
and overflow never break the svg. |
| `EmptyStates_UnknownEmptyAndSoftDeletedAreByteForByteEqualAsync`           | Fact | Empty states unknown empty and
soft deleted are byte for byte equal. |
| `IdEnumeration_NeighbouringIdsAreOwnDataOrTheEmptyStateNeverForeignAsync`  | Fact | Id enumeration neighbouring ids
are own data or the empty state never foreign. |
| `CrossTenant_ForeignAndUnknownIdsAnswerIndistinguishablyAsync`             | Fact | Cross tenant foreign and unknown
ids answer indistinguishably. |
| `AuthenticationAndPermissionMatrix_RefusesEveryoneButAuthorisedUsersAsync` | Fact | Authentication and permission
matrix refuses everyone but authorised users. |
| `AuthorisedUser_ReadsOwnDataAsSafeSvgAsync`                                | Fact | Authorised user reads own data as
safe svg. |

| Test | Kind | Verifies |
|------|------|----------|

### PenTestExhaustiveChartLearningCurve

`Jube.Test.Security.QueryExhaustive.PenTestExhaustiveChartLearningCurve` — 11 tests. Category: PenTest.
| `HostileVariableNames_AreEscapedAndNeverBecomeMarkupAsync`                 | Fact | Hostile variable names are escaped
and never become markup. |
| `VerbTampering_OnlyGetIsServedAsync`                                       | Fact | Verb tampering only get is
served. |
| `HostileQueryStringAndHeaders_NeverChangeTheChartAsync`                    | Fact | Hostile query string and headers
never change the chart. |
| `IdBoundariesAndTypeConfusion_AreRejectedOrEmptyNeverServerErrorsAsync`    | Fact | Id boundaries and type confusion
are rejected or empty never server errors. |
| `ConcurrentReads_AreDeterministicAndBoundedAsync`                          | Fact | Concurrent reads are deterministic
and bounded. |
| `NumericEdgeCases_NaNInfinityAndOverflowNeverBreakTheSvgAsync`             | Fact | Numeric edge cases na n infinity
and overflow never break the svg. |
| `EmptyStates_UnknownEmptyAndSoftDeletedAreByteForByteEqualAsync`           | Fact | Empty states unknown empty and
soft deleted are byte for byte equal. |
| `IdEnumeration_NeighbouringIdsAreOwnDataOrTheEmptyStateNeverForeignAsync`  | Fact | Id enumeration neighbouring ids
are own data or the empty state never foreign. |
| `CrossTenant_ForeignAndUnknownIdsAnswerIndistinguishablyAsync`             | Fact | Cross tenant foreign and unknown
ids answer indistinguishably. |
| `AuthenticationAndPermissionMatrix_RefusesEveryoneButAuthorisedUsersAsync` | Fact | Authentication and permission
matrix refuses everyone but authorised users. |
| `AuthorisedUser_ReadsOwnDataAsSafeSvgAsync`                                | Fact | Authorised user reads own data as
safe svg. |

| Test | Kind | Verifies |
|------|------|----------|

### PenTestExhaustiveChartPerformanceCalibration

`Jube.Test.Security.QueryExhaustive.PenTestExhaustiveChartPerformanceCalibration` — 11 tests. Category: PenTest.
| `HostileVariableNames_AreEscapedAndNeverBecomeMarkupAsync`                 | Fact | Hostile variable names are escaped
and never become markup. |
| `VerbTampering_OnlyGetIsServedAsync`                                       | Fact | Verb tampering only get is
served. |
| `HostileQueryStringAndHeaders_NeverChangeTheChartAsync`                    | Fact | Hostile query string and headers
never change the chart. |
| `IdBoundariesAndTypeConfusion_AreRejectedOrEmptyNeverServerErrorsAsync`    | Fact | Id boundaries and type confusion
are rejected or empty never server errors. |
| `ConcurrentReads_AreDeterministicAndBoundedAsync`                          | Fact | Concurrent reads are deterministic
and bounded. |
| `NumericEdgeCases_NaNInfinityAndOverflowNeverBreakTheSvgAsync`             | Fact | Numeric edge cases na n infinity
and overflow never break the svg. |
| `EmptyStates_UnknownEmptyAndSoftDeletedAreByteForByteEqualAsync`           | Fact | Empty states unknown empty and
soft deleted are byte for byte equal. |
| `IdEnumeration_NeighbouringIdsAreOwnDataOrTheEmptyStateNeverForeignAsync`  | Fact | Id enumeration neighbouring ids
are own data or the empty state never foreign. |
| `CrossTenant_ForeignAndUnknownIdsAnswerIndistinguishablyAsync`             | Fact | Cross tenant foreign and unknown
ids answer indistinguishably. |
| `AuthenticationAndPermissionMatrix_RefusesEveryoneButAuthorisedUsersAsync` | Fact | Authentication and permission
matrix refuses everyone but authorised users. |
| `AuthorisedUser_ReadsOwnDataAsSafeSvgAsync`                                | Fact | Authorised user reads own data as
safe svg. |

| Test | Kind | Verifies |
|------|------|----------|

### PenTestExhaustiveChartPerformanceConfusion

`Jube.Test.Security.QueryExhaustive.PenTestExhaustiveChartPerformanceConfusion` — 11 tests. Category: PenTest.
| `HostileVariableNames_AreEscapedAndNeverBecomeMarkupAsync`                 | Fact | Hostile variable names are escaped
and never become markup. |
| `VerbTampering_OnlyGetIsServedAsync`                                       | Fact | Verb tampering only get is
served. |
| `HostileQueryStringAndHeaders_NeverChangeTheChartAsync`                    | Fact | Hostile query string and headers
never change the chart. |
| `IdBoundariesAndTypeConfusion_AreRejectedOrEmptyNeverServerErrorsAsync`    | Fact | Id boundaries and type confusion
are rejected or empty never server errors. |
| `ConcurrentReads_AreDeterministicAndBoundedAsync`                          | Fact | Concurrent reads are deterministic
and bounded. |
| `NumericEdgeCases_NaNInfinityAndOverflowNeverBreakTheSvgAsync`             | Fact | Numeric edge cases na n infinity
and overflow never break the svg. |
| `EmptyStates_UnknownEmptyAndSoftDeletedAreByteForByteEqualAsync`           | Fact | Empty states unknown empty and
soft deleted are byte for byte equal. |
| `IdEnumeration_NeighbouringIdsAreOwnDataOrTheEmptyStateNeverForeignAsync`  | Fact | Id enumeration neighbouring ids
are own data or the empty state never foreign. |
| `CrossTenant_ForeignAndUnknownIdsAnswerIndistinguishablyAsync`             | Fact | Cross tenant foreign and unknown
ids answer indistinguishably. |
| `AuthenticationAndPermissionMatrix_RefusesEveryoneButAuthorisedUsersAsync` | Fact | Authentication and permission
matrix refuses everyone but authorised users. |
| `AuthorisedUser_ReadsOwnDataAsSafeSvgAsync`                                | Fact | Authorised user reads own data as
safe svg. |

| Test | Kind | Verifies |
|------|------|----------|

### PenTestExhaustiveChartPerformanceGains

`Jube.Test.Security.QueryExhaustive.PenTestExhaustiveChartPerformanceGains` — 11 tests. Category: PenTest.
| `HostileVariableNames_AreEscapedAndNeverBecomeMarkupAsync`                 | Fact | Hostile variable names are escaped
and never become markup. |
| `VerbTampering_OnlyGetIsServedAsync`                                       | Fact | Verb tampering only get is
served. |
| `HostileQueryStringAndHeaders_NeverChangeTheChartAsync`                    | Fact | Hostile query string and headers
never change the chart. |
| `IdBoundariesAndTypeConfusion_AreRejectedOrEmptyNeverServerErrorsAsync`    | Fact | Id boundaries and type confusion
are rejected or empty never server errors. |
| `ConcurrentReads_AreDeterministicAndBoundedAsync`                          | Fact | Concurrent reads are deterministic
and bounded. |
| `NumericEdgeCases_NaNInfinityAndOverflowNeverBreakTheSvgAsync`             | Fact | Numeric edge cases na n infinity
and overflow never break the svg. |
| `EmptyStates_UnknownEmptyAndSoftDeletedAreByteForByteEqualAsync`           | Fact | Empty states unknown empty and
soft deleted are byte for byte equal. |
| `IdEnumeration_NeighbouringIdsAreOwnDataOrTheEmptyStateNeverForeignAsync`  | Fact | Id enumeration neighbouring ids
are own data or the empty state never foreign. |
| `CrossTenant_ForeignAndUnknownIdsAnswerIndistinguishablyAsync`             | Fact | Cross tenant foreign and unknown
ids answer indistinguishably. |
| `AuthenticationAndPermissionMatrix_RefusesEveryoneButAuthorisedUsersAsync` | Fact | Authentication and permission
matrix refuses everyone but authorised users. |
| `AuthorisedUser_ReadsOwnDataAsSafeSvgAsync`                                | Fact | Authorised user reads own data as
safe svg. |

| Test | Kind | Verifies |
|------|------|----------|

### PenTestExhaustiveChartPerformanceKs

`Jube.Test.Security.QueryExhaustive.PenTestExhaustiveChartPerformanceKs` — 11 tests. Category: PenTest.
| `HostileVariableNames_AreEscapedAndNeverBecomeMarkupAsync`                 | Fact | Hostile variable names are escaped
and never become markup. |
| `VerbTampering_OnlyGetIsServedAsync`                                       | Fact | Verb tampering only get is
served. |
| `HostileQueryStringAndHeaders_NeverChangeTheChartAsync`                    | Fact | Hostile query string and headers
never change the chart. |
| `IdBoundariesAndTypeConfusion_AreRejectedOrEmptyNeverServerErrorsAsync`    | Fact | Id boundaries and type confusion
are rejected or empty never server errors. |
| `ConcurrentReads_AreDeterministicAndBoundedAsync`                          | Fact | Concurrent reads are deterministic
and bounded. |
| `NumericEdgeCases_NaNInfinityAndOverflowNeverBreakTheSvgAsync`             | Fact | Numeric edge cases na n infinity
and overflow never break the svg. |
| `EmptyStates_UnknownEmptyAndSoftDeletedAreByteForByteEqualAsync`           | Fact | Empty states unknown empty and
soft deleted are byte for byte equal. |
| `IdEnumeration_NeighbouringIdsAreOwnDataOrTheEmptyStateNeverForeignAsync`  | Fact | Id enumeration neighbouring ids
are own data or the empty state never foreign. |
| `CrossTenant_ForeignAndUnknownIdsAnswerIndistinguishablyAsync`             | Fact | Cross tenant foreign and unknown
ids answer indistinguishably. |
| `AuthenticationAndPermissionMatrix_RefusesEveryoneButAuthorisedUsersAsync` | Fact | Authentication and permission
matrix refuses everyone but authorised users. |
| `AuthorisedUser_ReadsOwnDataAsSafeSvgAsync`                                | Fact | Authorised user reads own data as
safe svg. |

| Test | Kind | Verifies |
|------|------|----------|

### PenTestExhaustiveChartPerformanceLift

`Jube.Test.Security.QueryExhaustive.PenTestExhaustiveChartPerformanceLift` — 11 tests. Category: PenTest.
| `HostileVariableNames_AreEscapedAndNeverBecomeMarkupAsync`                 | Fact | Hostile variable names are escaped
and never become markup. |
| `VerbTampering_OnlyGetIsServedAsync`                                       | Fact | Verb tampering only get is
served. |
| `HostileQueryStringAndHeaders_NeverChangeTheChartAsync`                    | Fact | Hostile query string and headers
never change the chart. |
| `IdBoundariesAndTypeConfusion_AreRejectedOrEmptyNeverServerErrorsAsync`    | Fact | Id boundaries and type confusion
are rejected or empty never server errors. |
| `ConcurrentReads_AreDeterministicAndBoundedAsync`                          | Fact | Concurrent reads are deterministic
and bounded. |
| `NumericEdgeCases_NaNInfinityAndOverflowNeverBreakTheSvgAsync`             | Fact | Numeric edge cases na n infinity
and overflow never break the svg. |
| `EmptyStates_UnknownEmptyAndSoftDeletedAreByteForByteEqualAsync`           | Fact | Empty states unknown empty and
soft deleted are byte for byte equal. |
| `IdEnumeration_NeighbouringIdsAreOwnDataOrTheEmptyStateNeverForeignAsync`  | Fact | Id enumeration neighbouring ids
are own data or the empty state never foreign. |
| `CrossTenant_ForeignAndUnknownIdsAnswerIndistinguishablyAsync`             | Fact | Cross tenant foreign and unknown
ids answer indistinguishably. |
| `AuthenticationAndPermissionMatrix_RefusesEveryoneButAuthorisedUsersAsync` | Fact | Authentication and permission
matrix refuses everyone but authorised users. |
| `AuthorisedUser_ReadsOwnDataAsSafeSvgAsync`                                | Fact | Authorised user reads own data as
safe svg. |

| Test | Kind | Verifies |
|------|------|----------|

### PenTestExhaustiveChartPerformanceMeasures

`Jube.Test.Security.QueryExhaustive.PenTestExhaustiveChartPerformanceMeasures` — 11 tests. Category: PenTest.
| `HostileVariableNames_AreEscapedAndNeverBecomeMarkupAsync`                 | Fact | Hostile variable names are escaped
and never become markup. |
| `VerbTampering_OnlyGetIsServedAsync`                                       | Fact | Verb tampering only get is
served. |
| `HostileQueryStringAndHeaders_NeverChangeTheChartAsync`                    | Fact | Hostile query string and headers
never change the chart. |
| `IdBoundariesAndTypeConfusion_AreRejectedOrEmptyNeverServerErrorsAsync`    | Fact | Id boundaries and type confusion
are rejected or empty never server errors. |
| `ConcurrentReads_AreDeterministicAndBoundedAsync`                          | Fact | Concurrent reads are deterministic
and bounded. |
| `NumericEdgeCases_NaNInfinityAndOverflowNeverBreakTheSvgAsync`             | Fact | Numeric edge cases na n infinity
and overflow never break the svg. |
| `EmptyStates_UnknownEmptyAndSoftDeletedAreByteForByteEqualAsync`           | Fact | Empty states unknown empty and
soft deleted are byte for byte equal. |
| `IdEnumeration_NeighbouringIdsAreOwnDataOrTheEmptyStateNeverForeignAsync`  | Fact | Id enumeration neighbouring ids
are own data or the empty state never foreign. |
| `CrossTenant_ForeignAndUnknownIdsAnswerIndistinguishablyAsync`             | Fact | Cross tenant foreign and unknown
ids answer indistinguishably. |
| `AuthenticationAndPermissionMatrix_RefusesEveryoneButAuthorisedUsersAsync` | Fact | Authentication and permission
matrix refuses everyone but authorised users. |
| `AuthorisedUser_ReadsOwnDataAsSafeSvgAsync`                                | Fact | Authorised user reads own data as
safe svg. |

| Test | Kind | Verifies |
|------|------|----------|

### PenTestExhaustiveChartPerformanceParameters

`Jube.Test.Security.QueryExhaustive.PenTestExhaustiveChartPerformanceParameters` — 13 tests. Category: PenTest.

| Test                                                                         | Kind | Verifies                                                                             |
|------------------------------------------------------------------------------|------|--------------------------------------------------------------------------------------|
| `ThresholdAndCostParameters_AreValidatedNeverServerErrorsNorReflectedAsync`  | Fact | Threshold and cost parameters are validated never server errors nor reflected.       |
| `CostsMoveTheMinimumRiskLineOnlyForTheOwnerAndNeverExposeAnotherTenantAsync` | Fact | Costs move the minimum risk line only for the owner and never expose another tenant. |
| `HostileVariableNames_AreEscapedAndNeverBecomeMarkupAsync`                   | Fact | Hostile variable names are escaped and never become markup.                          |
| `VerbTampering_OnlyGetIsServedAsync`                                         | Fact | Verb tampering only get is served.                                                   |
| `HostileQueryStringAndHeaders_NeverChangeTheChartAsync`                      | Fact | Hostile query string and headers never change the chart.                             |
| `IdBoundariesAndTypeConfusion_AreRejectedOrEmptyNeverServerErrorsAsync`      | Fact | Id boundaries and type confusion are rejected or empty never server errors.          |
| `ConcurrentReads_AreDeterministicAndBoundedAsync`                            | Fact | Concurrent reads are deterministic and bounded.                                      |
| `NumericEdgeCases_NaNInfinityAndOverflowNeverBreakTheSvgAsync`               | Fact | Numeric edge cases na n infinity and overflow never break the svg.                   |
| `EmptyStates_UnknownEmptyAndSoftDeletedAreByteForByteEqualAsync`             | Fact | Empty states unknown empty and soft deleted are byte for byte equal.                 |
| `IdEnumeration_NeighbouringIdsAreOwnDataOrTheEmptyStateNeverForeignAsync`    | Fact | Id enumeration neighbouring ids are own data or the empty state never foreign.       |
| `CrossTenant_ForeignAndUnknownIdsAnswerIndistinguishablyAsync`               | Fact | Cross tenant foreign and unknown ids answer indistinguishably.                       |
| `AuthenticationAndPermissionMatrix_RefusesEveryoneButAuthorisedUsersAsync`   | Fact | Authentication and permission matrix refuses everyone but authorised users.          |
| `AuthorisedUser_ReadsOwnDataAsSafeSvgAsync`                                  | Fact | Authorised user reads own data as safe svg.                                          |

### PenTestExhaustiveChartPerformancePrecisionRecall

`Jube.Test.Security.QueryExhaustive.PenTestExhaustiveChartPerformancePrecisionRecall` — 11 tests. Category: PenTest.
| `HostileVariableNames_AreEscapedAndNeverBecomeMarkupAsync`                 | Fact | Hostile variable names are escaped
and never become markup. |
| `VerbTampering_OnlyGetIsServedAsync`                                       | Fact | Verb tampering only get is
served. |
| `HostileQueryStringAndHeaders_NeverChangeTheChartAsync`                    | Fact | Hostile query string and headers
never change the chart. |
| `IdBoundariesAndTypeConfusion_AreRejectedOrEmptyNeverServerErrorsAsync`    | Fact | Id boundaries and type confusion
are rejected or empty never server errors. |
| `ConcurrentReads_AreDeterministicAndBoundedAsync`                          | Fact | Concurrent reads are deterministic
and bounded. |
| `NumericEdgeCases_NaNInfinityAndOverflowNeverBreakTheSvgAsync`             | Fact | Numeric edge cases na n infinity
and overflow never break the svg. |
| `EmptyStates_UnknownEmptyAndSoftDeletedAreByteForByteEqualAsync`           | Fact | Empty states unknown empty and
soft deleted are byte for byte equal. |
| `IdEnumeration_NeighbouringIdsAreOwnDataOrTheEmptyStateNeverForeignAsync`  | Fact | Id enumeration neighbouring ids
are own data or the empty state never foreign. |
| `CrossTenant_ForeignAndUnknownIdsAnswerIndistinguishablyAsync`             | Fact | Cross tenant foreign and unknown
ids answer indistinguishably. |
| `AuthenticationAndPermissionMatrix_RefusesEveryoneButAuthorisedUsersAsync` | Fact | Authentication and permission
matrix refuses everyone but authorised users. |
| `AuthorisedUser_ReadsOwnDataAsSafeSvgAsync`                                | Fact | Authorised user reads own data as
safe svg. |

| Test | Kind | Verifies |
|------|------|----------|

### PenTestExhaustiveChartPerformancePromotionHistory

`Jube.Test.Security.QueryExhaustive.PenTestExhaustiveChartPerformancePromotionHistory` — 11 tests. Category: PenTest.
| `HostileVariableNames_AreEscapedAndNeverBecomeMarkupAsync`                 | Fact | Hostile variable names are escaped
and never become markup. |
| `VerbTampering_OnlyGetIsServedAsync`                                       | Fact | Verb tampering only get is
served. |
| `HostileQueryStringAndHeaders_NeverChangeTheChartAsync`                    | Fact | Hostile query string and headers
never change the chart. |
| `IdBoundariesAndTypeConfusion_AreRejectedOrEmptyNeverServerErrorsAsync`    | Fact | Id boundaries and type confusion
are rejected or empty never server errors. |
| `ConcurrentReads_AreDeterministicAndBoundedAsync`                          | Fact | Concurrent reads are deterministic
and bounded. |
| `NumericEdgeCases_NaNInfinityAndOverflowNeverBreakTheSvgAsync`             | Fact | Numeric edge cases na n infinity
and overflow never break the svg. |
| `EmptyStates_UnknownEmptyAndSoftDeletedAreByteForByteEqualAsync`           | Fact | Empty states unknown empty and
soft deleted are byte for byte equal. |
| `IdEnumeration_NeighbouringIdsAreOwnDataOrTheEmptyStateNeverForeignAsync`  | Fact | Id enumeration neighbouring ids
are own data or the empty state never foreign. |
| `CrossTenant_ForeignAndUnknownIdsAnswerIndistinguishablyAsync`             | Fact | Cross tenant foreign and unknown
ids answer indistinguishably. |
| `AuthenticationAndPermissionMatrix_RefusesEveryoneButAuthorisedUsersAsync` | Fact | Authentication and permission
matrix refuses everyone but authorised users. |
| `AuthorisedUser_ReadsOwnDataAsSafeSvgAsync`                                | Fact | Authorised user reads own data as
safe svg. |

| Test | Kind | Verifies |
|------|------|----------|

### PenTestExhaustiveChartPerformanceRoc

`Jube.Test.Security.QueryExhaustive.PenTestExhaustiveChartPerformanceRoc` — 11 tests. Category: PenTest.
| `HostileVariableNames_AreEscapedAndNeverBecomeMarkupAsync`                 | Fact | Hostile variable names are escaped
and never become markup. |
| `VerbTampering_OnlyGetIsServedAsync`                                       | Fact | Verb tampering only get is
served. |
| `HostileQueryStringAndHeaders_NeverChangeTheChartAsync`                    | Fact | Hostile query string and headers
never change the chart. |
| `IdBoundariesAndTypeConfusion_AreRejectedOrEmptyNeverServerErrorsAsync`    | Fact | Id boundaries and type confusion
are rejected or empty never server errors. |
| `ConcurrentReads_AreDeterministicAndBoundedAsync`                          | Fact | Concurrent reads are deterministic
and bounded. |
| `NumericEdgeCases_NaNInfinityAndOverflowNeverBreakTheSvgAsync`             | Fact | Numeric edge cases na n infinity
and overflow never break the svg. |
| `EmptyStates_UnknownEmptyAndSoftDeletedAreByteForByteEqualAsync`           | Fact | Empty states unknown empty and
soft deleted are byte for byte equal. |
| `IdEnumeration_NeighbouringIdsAreOwnDataOrTheEmptyStateNeverForeignAsync`  | Fact | Id enumeration neighbouring ids
are own data or the empty state never foreign. |
| `CrossTenant_ForeignAndUnknownIdsAnswerIndistinguishablyAsync`             | Fact | Cross tenant foreign and unknown
ids answer indistinguishably. |
| `AuthenticationAndPermissionMatrix_RefusesEveryoneButAuthorisedUsersAsync` | Fact | Authentication and permission
matrix refuses everyone but authorised users. |
| `AuthorisedUser_ReadsOwnDataAsSafeSvgAsync`                                | Fact | Authorised user reads own data as
safe svg. |

| Test | Kind | Verifies |
|------|------|----------|

### PenTestExhaustiveChartPerformanceScoreDistribution

`Jube.Test.Security.QueryExhaustive.PenTestExhaustiveChartPerformanceScoreDistribution` — 11 tests. Category: PenTest.
| `HostileVariableNames_AreEscapedAndNeverBecomeMarkupAsync`                 | Fact | Hostile variable names are escaped
and never become markup. |
| `VerbTampering_OnlyGetIsServedAsync`                                       | Fact | Verb tampering only get is
served. |
| `HostileQueryStringAndHeaders_NeverChangeTheChartAsync`                    | Fact | Hostile query string and headers
never change the chart. |
| `IdBoundariesAndTypeConfusion_AreRejectedOrEmptyNeverServerErrorsAsync`    | Fact | Id boundaries and type confusion
are rejected or empty never server errors. |
| `ConcurrentReads_AreDeterministicAndBoundedAsync`                          | Fact | Concurrent reads are deterministic
and bounded. |
| `NumericEdgeCases_NaNInfinityAndOverflowNeverBreakTheSvgAsync`             | Fact | Numeric edge cases na n infinity
and overflow never break the svg. |
| `EmptyStates_UnknownEmptyAndSoftDeletedAreByteForByteEqualAsync`           | Fact | Empty states unknown empty and
soft deleted are byte for byte equal. |
| `IdEnumeration_NeighbouringIdsAreOwnDataOrTheEmptyStateNeverForeignAsync`  | Fact | Id enumeration neighbouring ids
are own data or the empty state never foreign. |
| `CrossTenant_ForeignAndUnknownIdsAnswerIndistinguishablyAsync`             | Fact | Cross tenant foreign and unknown
ids answer indistinguishably. |
| `AuthenticationAndPermissionMatrix_RefusesEveryoneButAuthorisedUsersAsync` | Fact | Authentication and permission
matrix refuses everyone but authorised users. |
| `AuthorisedUser_ReadsOwnDataAsSafeSvgAsync`                                | Fact | Authorised user reads own data as
safe svg. |

| Test | Kind | Verifies |
|------|------|----------|

### PenTestExhaustiveChartPerformanceThresholdSweep

`Jube.Test.Security.QueryExhaustive.PenTestExhaustiveChartPerformanceThresholdSweep` — 11 tests. Category: PenTest.
| `HostileVariableNames_AreEscapedAndNeverBecomeMarkupAsync`                 | Fact | Hostile variable names are escaped
and never become markup. |
| `VerbTampering_OnlyGetIsServedAsync`                                       | Fact | Verb tampering only get is
served. |
| `HostileQueryStringAndHeaders_NeverChangeTheChartAsync`                    | Fact | Hostile query string and headers
never change the chart. |
| `IdBoundariesAndTypeConfusion_AreRejectedOrEmptyNeverServerErrorsAsync`    | Fact | Id boundaries and type confusion
are rejected or empty never server errors. |
| `ConcurrentReads_AreDeterministicAndBoundedAsync`                          | Fact | Concurrent reads are deterministic
and bounded. |
| `NumericEdgeCases_NaNInfinityAndOverflowNeverBreakTheSvgAsync`             | Fact | Numeric edge cases na n infinity
and overflow never break the svg. |
| `EmptyStates_UnknownEmptyAndSoftDeletedAreByteForByteEqualAsync`           | Fact | Empty states unknown empty and
soft deleted are byte for byte equal. |
| `IdEnumeration_NeighbouringIdsAreOwnDataOrTheEmptyStateNeverForeignAsync`  | Fact | Id enumeration neighbouring ids
are own data or the empty state never foreign. |
| `CrossTenant_ForeignAndUnknownIdsAnswerIndistinguishablyAsync`             | Fact | Cross tenant foreign and unknown
ids answer indistinguishably. |
| `AuthenticationAndPermissionMatrix_RefusesEveryoneButAuthorisedUsersAsync` | Fact | Authentication and permission
matrix refuses everyone but authorised users. |
| `AuthorisedUser_ReadsOwnDataAsSafeSvgAsync`                                | Fact | Authorised user reads own data as
safe svg. |

| Test | Kind | Verifies |
|------|------|----------|

### PenTestExhaustiveChartPerformanceTopology

`Jube.Test.Security.QueryExhaustive.PenTestExhaustiveChartPerformanceTopology` — 11 tests. Category: PenTest.
| `HostileVariableNames_AreEscapedAndNeverBecomeMarkupAsync`                 | Fact | Hostile variable names are escaped
and never become markup. |
| `VerbTampering_OnlyGetIsServedAsync`                                       | Fact | Verb tampering only get is
served. |
| `HostileQueryStringAndHeaders_NeverChangeTheChartAsync`                    | Fact | Hostile query string and headers
never change the chart. |
| `IdBoundariesAndTypeConfusion_AreRejectedOrEmptyNeverServerErrorsAsync`    | Fact | Id boundaries and type confusion
are rejected or empty never server errors. |
| `ConcurrentReads_AreDeterministicAndBoundedAsync`                          | Fact | Concurrent reads are deterministic
and bounded. |
| `NumericEdgeCases_NaNInfinityAndOverflowNeverBreakTheSvgAsync`             | Fact | Numeric edge cases na n infinity
and overflow never break the svg. |
| `EmptyStates_UnknownEmptyAndSoftDeletedAreByteForByteEqualAsync`           | Fact | Empty states unknown empty and
soft deleted are byte for byte equal. |
| `IdEnumeration_NeighbouringIdsAreOwnDataOrTheEmptyStateNeverForeignAsync`  | Fact | Id enumeration neighbouring ids
are own data or the empty state never foreign. |
| `CrossTenant_ForeignAndUnknownIdsAnswerIndistinguishablyAsync`             | Fact | Cross tenant foreign and unknown
ids answer indistinguishably. |
| `AuthenticationAndPermissionMatrix_RefusesEveryoneButAuthorisedUsersAsync` | Fact | Authentication and permission
matrix refuses everyone but authorised users. |
| `AuthorisedUser_ReadsOwnDataAsSafeSvgAsync`                                | Fact | Authorised user reads own data as
safe svg. |

| Test | Kind | Verifies |
|------|------|----------|

### PenTestExhaustiveChartPredictedActual

`Jube.Test.Security.QueryExhaustive.PenTestExhaustiveChartPredictedActual` — 11 tests. Category: PenTest.
| `HostileVariableNames_AreEscapedAndNeverBecomeMarkupAsync`                 | Fact | Hostile variable names are escaped
and never become markup. |
| `VerbTampering_OnlyGetIsServedAsync`                                       | Fact | Verb tampering only get is
served. |
| `HostileQueryStringAndHeaders_NeverChangeTheChartAsync`                    | Fact | Hostile query string and headers
never change the chart. |
| `IdBoundariesAndTypeConfusion_AreRejectedOrEmptyNeverServerErrorsAsync`    | Fact | Id boundaries and type confusion
are rejected or empty never server errors. |
| `ConcurrentReads_AreDeterministicAndBoundedAsync`                          | Fact | Concurrent reads are deterministic
and bounded. |
| `NumericEdgeCases_NaNInfinityAndOverflowNeverBreakTheSvgAsync`             | Fact | Numeric edge cases na n infinity
and overflow never break the svg. |
| `EmptyStates_UnknownEmptyAndSoftDeletedAreByteForByteEqualAsync`           | Fact | Empty states unknown empty and
soft deleted are byte for byte equal. |
| `IdEnumeration_NeighbouringIdsAreOwnDataOrTheEmptyStateNeverForeignAsync`  | Fact | Id enumeration neighbouring ids
are own data or the empty state never foreign. |
| `CrossTenant_ForeignAndUnknownIdsAnswerIndistinguishablyAsync`             | Fact | Cross tenant foreign and unknown
ids answer indistinguishably. |
| `AuthenticationAndPermissionMatrix_RefusesEveryoneButAuthorisedUsersAsync` | Fact | Authentication and permission
matrix refuses everyone but authorised users. |
| `AuthorisedUser_ReadsOwnDataAsSafeSvgAsync`                                | Fact | Authorised user reads own data as
safe svg. |

| Test | Kind | Verifies |
|------|------|----------|

### PenTestExhaustiveChartRoc

`Jube.Test.Security.QueryExhaustive.PenTestExhaustiveChartRoc` — 11 tests. Category: PenTest.
| `HostileVariableNames_AreEscapedAndNeverBecomeMarkupAsync`                 | Fact | Hostile variable names are escaped
and never become markup. |
| `VerbTampering_OnlyGetIsServedAsync`                                       | Fact | Verb tampering only get is
served. |
| `HostileQueryStringAndHeaders_NeverChangeTheChartAsync`                    | Fact | Hostile query string and headers
never change the chart. |
| `IdBoundariesAndTypeConfusion_AreRejectedOrEmptyNeverServerErrorsAsync`    | Fact | Id boundaries and type confusion
are rejected or empty never server errors. |
| `ConcurrentReads_AreDeterministicAndBoundedAsync`                          | Fact | Concurrent reads are deterministic
and bounded. |
| `NumericEdgeCases_NaNInfinityAndOverflowNeverBreakTheSvgAsync`             | Fact | Numeric edge cases na n infinity
and overflow never break the svg. |
| `EmptyStates_UnknownEmptyAndSoftDeletedAreByteForByteEqualAsync`           | Fact | Empty states unknown empty and
soft deleted are byte for byte equal. |
| `IdEnumeration_NeighbouringIdsAreOwnDataOrTheEmptyStateNeverForeignAsync`  | Fact | Id enumeration neighbouring ids
are own data or the empty state never foreign. |
| `CrossTenant_ForeignAndUnknownIdsAnswerIndistinguishablyAsync`             | Fact | Cross tenant foreign and unknown
ids answer indistinguishably. |
| `AuthenticationAndPermissionMatrix_RefusesEveryoneButAuthorisedUsersAsync` | Fact | Authentication and permission
matrix refuses everyone but authorised users. |
| `AuthorisedUser_ReadsOwnDataAsSafeSvgAsync`                                | Fact | Authorised user reads own data as
safe svg. |

| Test | Kind | Verifies |
|------|------|----------|

### PenTestExhaustiveChartSensitivity

`Jube.Test.Security.QueryExhaustive.PenTestExhaustiveChartSensitivity` — 11 tests. Category: PenTest.
| `HostileVariableNames_AreEscapedAndNeverBecomeMarkupAsync`                 | Fact | Hostile variable names are escaped
and never become markup. |
| `VerbTampering_OnlyGetIsServedAsync`                                       | Fact | Verb tampering only get is
served. |
| `HostileQueryStringAndHeaders_NeverChangeTheChartAsync`                    | Fact | Hostile query string and headers
never change the chart. |
| `IdBoundariesAndTypeConfusion_AreRejectedOrEmptyNeverServerErrorsAsync`    | Fact | Id boundaries and type confusion
are rejected or empty never server errors. |
| `ConcurrentReads_AreDeterministicAndBoundedAsync`                          | Fact | Concurrent reads are deterministic
and bounded. |
| `NumericEdgeCases_NaNInfinityAndOverflowNeverBreakTheSvgAsync`             | Fact | Numeric edge cases na n infinity
and overflow never break the svg. |
| `EmptyStates_UnknownEmptyAndSoftDeletedAreByteForByteEqualAsync`           | Fact | Empty states unknown empty and
soft deleted are byte for byte equal. |
| `IdEnumeration_NeighbouringIdsAreOwnDataOrTheEmptyStateNeverForeignAsync`  | Fact | Id enumeration neighbouring ids
are own data or the empty state never foreign. |
| `CrossTenant_ForeignAndUnknownIdsAnswerIndistinguishablyAsync`             | Fact | Cross tenant foreign and unknown
ids answer indistinguishably. |
| `AuthenticationAndPermissionMatrix_RefusesEveryoneButAuthorisedUsersAsync` | Fact | Authentication and permission
matrix refuses everyone but authorised users. |
| `AuthorisedUser_ReadsOwnDataAsSafeSvgAsync`                                | Fact | Authorised user reads own data as
safe svg. |

| Test | Kind | Verifies |
|------|------|----------|

### PenTestExhaustiveChartStylesheet

`Jube.Test.Security.QueryExhaustive.PenTestExhaustiveChartStylesheet` — 3 tests. Category: PenTest.

| Test                                                               | Kind | Verifies                                                            |
|--------------------------------------------------------------------|------|---------------------------------------------------------------------|
| `CssDirectory_HoldsOnlyTheGeneratedChartStylesheet`                | Fact | Css directory holds only the generated chart stylesheet.            |
| `Stylesheet_IsServedAnonymouslyAsInertCssWithNoTenantDataAsync`    | Fact | Stylesheet is served anonymously as inert css with no tenant data.  |
| `Stylesheet_TraversalAndVerbTamperingNeverExposeAnythingElseAsync` | Fact | Stylesheet traversal and verb tampering never expose anything else. |

### PenTestExhaustiveChartVariableHistogram

`Jube.Test.Security.QueryExhaustive.PenTestExhaustiveChartVariableHistogram` — 1 test. Category: PenTest.

| Test                                                                       | Kind | Verifies                                                                       |
|----------------------------------------------------------------------------|------|--------------------------------------------------------------------------------|
| `VariableOfAnotherInstanceOrTenant_IsTheEmptyStateAsync`                   | Fact | Variable of another instance or tenant is the empty state.                     |
| `HostileVariableNames_AreEscapedAndNeverBecomeMarkupAsync`                 | Fact | Hostile variable names are escaped and never become markup.                    |
| `VerbTampering_OnlyGetIsServedAsync`                                       | Fact | Verb tampering only get is served.                                             |
| `HostileQueryStringAndHeaders_NeverChangeTheChartAsync`                    | Fact | Hostile query string and headers never change the chart.                       |
| `IdBoundariesAndTypeConfusion_AreRejectedOrEmptyNeverServerErrorsAsync`    | Fact | Id boundaries and type confusion are rejected or empty never server errors.    |
| `ConcurrentReads_AreDeterministicAndBoundedAsync`                          | Fact | Concurrent reads are deterministic and bounded.                                |
| `NumericEdgeCases_NaNInfinityAndOverflowNeverBreakTheSvgAsync`             | Fact | Numeric edge cases na n infinity and overflow never break the svg.             |
| `EmptyStates_UnknownEmptyAndSoftDeletedAreByteForByteEqualAsync`           | Fact | Empty states unknown empty and soft deleted are byte for byte equal.           |
| `IdEnumeration_NeighbouringIdsAreOwnDataOrTheEmptyStateNeverForeignAsync`  | Fact | Id enumeration neighbouring ids are own data or the empty state never foreign. |
| `CrossTenant_ForeignAndUnknownIdsAnswerIndistinguishablyAsync`             | Fact | Cross tenant foreign and unknown ids answer indistinguishably.                 |
| `AuthenticationAndPermissionMatrix_RefusesEveryoneButAuthorisedUsersAsync` | Fact | Authentication and permission matrix refuses everyone but authorised users.    |
| `AuthorisedUser_ReadsOwnDataAsSafeSvgAsync`                                | Fact | Authorised user reads own data as safe svg.                                    |

### PenTestExhaustiveSearchInstancePromotedTrialInstanceConfusion

`Jube.Test.Security.QueryExhaustive.PenTestExhaustiveSearchInstancePromotedTrialInstanceConfusion` — 1 test. Category:
PenTest.

| Test                                                                          | Kind | Verifies                                                                          |
|-------------------------------------------------------------------------------|------|-----------------------------------------------------------------------------------|
| `Counts_AreConsistentAndRatiosAreFiniteAsync`                                 | Fact | Counts are consistent and ratios are finite.                                      |
| `VerbTampering_OnlyGetIsServedAndNothingIsModifiedAsync`                      | Fact | Verb tampering only get is served and nothing is modified.                        |
| `HostileQueryStringAndHeaders_NeverChangeTheResultAsync`                      | Fact | Hostile query string and headers never change the result.                         |
| `IdBoundariesAndTypeConfusion_AreRejectedOrEmptyNeverServerErrorsAsync`       | Fact | Id boundaries and type confusion are rejected or empty never server errors.       |
| `ConcurrentReads_AreDeterministicAndBoundedAsync`                             | Fact | Concurrent reads are deterministic and bounded.                                   |
| `NumericEdgeCases_NaNInfinityZeroDenominatorAndOverflowSerialiseValidlyAsync` | Fact | Numeric edge cases na n infinity zero denominator and overflow serialise validly. |
| `EmptyResult_IsAWellFormedEmptyDocumentAsync`                                 | Fact | Empty result is a well formed empty document.                                     |
| `SoftDeleted_RowsAndInstancesAreNeverReturnedAsync`                           | Fact | Soft deleted rows and instances are never returned.                               |
| `IdEnumeration_NeighbouringIdsNeverLeakAcrossTenantsAndStayUniformAsync`      | Fact | Id enumeration neighbouring ids never leak across tenants and stay uniform.       |
| `CrossTenant_ForeignIdLooksExactlyLikeANonExistentIdAsync`                    | Fact | Cross tenant foreign id looks exactly like a non existent id.                     |
| `AuthenticationAndPermissionMatrix_RefusesEveryoneButAuthorisedUsersAsync`    | Fact | Authentication and permission matrix refuses everyone but authorised users.       |
| `AuthorisedUser_ReadsOwnDataAsWellFormedJsonAsync`                            | Fact | Authorised user reads own data as well formed json.                               |

### PenTestExhaustiveSearchInstancePromotedTrialInstanceErrorHistogram

`Jube.Test.Security.QueryExhaustive.PenTestExhaustiveSearchInstancePromotedTrialInstanceErrorHistogram` — 1 test.
Category: PenTest.

| Test                                                                          | Kind | Verifies                                                                          |
|-------------------------------------------------------------------------------|------|-----------------------------------------------------------------------------------|
| `Histogram_CountsOnlyTheLiveRowsOfTheOwnersPromotedTrialAsync`                | Fact | Histogram counts only the live rows of the owners promoted trial.                 |
| `VerbTampering_OnlyGetIsServedAndNothingIsModifiedAsync`                      | Fact | Verb tampering only get is served and nothing is modified.                        |
| `HostileQueryStringAndHeaders_NeverChangeTheResultAsync`                      | Fact | Hostile query string and headers never change the result.                         |
| `IdBoundariesAndTypeConfusion_AreRejectedOrEmptyNeverServerErrorsAsync`       | Fact | Id boundaries and type confusion are rejected or empty never server errors.       |
| `ConcurrentReads_AreDeterministicAndBoundedAsync`                             | Fact | Concurrent reads are deterministic and bounded.                                   |
| `NumericEdgeCases_NaNInfinityZeroDenominatorAndOverflowSerialiseValidlyAsync` | Fact | Numeric edge cases na n infinity zero denominator and overflow serialise validly. |
| `EmptyResult_IsAWellFormedEmptyDocumentAsync`                                 | Fact | Empty result is a well formed empty document.                                     |
| `SoftDeleted_RowsAndInstancesAreNeverReturnedAsync`                           | Fact | Soft deleted rows and instances are never returned.                               |
| `IdEnumeration_NeighbouringIdsNeverLeakAcrossTenantsAndStayUniformAsync`      | Fact | Id enumeration neighbouring ids never leak across tenants and stay uniform.       |
| `CrossTenant_ForeignIdLooksExactlyLikeANonExistentIdAsync`                    | Fact | Cross tenant foreign id looks exactly like a non existent id.                     |
| `AuthenticationAndPermissionMatrix_RefusesEveryoneButAuthorisedUsersAsync`    | Fact | Authentication and permission matrix refuses everyone but authorised users.       |
| `AuthorisedUser_ReadsOwnDataAsWellFormedJsonAsync`                            | Fact | Authorised user reads own data as well formed json.                               |

### PenTestExhaustiveSearchInstancePromotedTrialInstanceLearningCurve

`Jube.Test.Security.QueryExhaustive.PenTestExhaustiveSearchInstancePromotedTrialInstanceLearningCurve` — 11 tests.
| `VerbTampering_OnlyGetIsServedAndNothingIsModifiedAsync`                      | Fact | Verb tampering only get is
served and nothing is modified. |
| `HostileQueryStringAndHeaders_NeverChangeTheResultAsync`                      | Fact | Hostile query string and
headers never change the result. |
| `IdBoundariesAndTypeConfusion_AreRejectedOrEmptyNeverServerErrorsAsync`       | Fact | Id boundaries and type
confusion are rejected or empty never server errors. |
| `ConcurrentReads_AreDeterministicAndBoundedAsync`                             | Fact | Concurrent reads are
deterministic and bounded. |
| `NumericEdgeCases_NaNInfinityZeroDenominatorAndOverflowSerialiseValidlyAsync` | Fact | Numeric edge cases na n
infinity zero denominator and overflow serialise validly. |
| `EmptyResult_IsAWellFormedEmptyDocumentAsync`                                 | Fact | Empty result is a well formed
empty document. |
| `SoftDeleted_RowsAndInstancesAreNeverReturnedAsync`                           | Fact | Soft deleted rows and instances
are never returned. |
| `IdEnumeration_NeighbouringIdsNeverLeakAcrossTenantsAndStayUniformAsync`      | Fact | Id enumeration neighbouring ids
never leak across tenants and stay uniform. |
| `CrossTenant_ForeignIdLooksExactlyLikeANonExistentIdAsync`                    | Fact | Cross tenant foreign id looks
exactly like a non existent id. |
| `AuthenticationAndPermissionMatrix_RefusesEveryoneButAuthorisedUsersAsync`    | Fact | Authentication and permission
matrix refuses everyone but authorised users. |
| `AuthorisedUser_ReadsOwnDataAsWellFormedJsonAsync`                            | Fact | Authorised user reads own data
as well formed json. |
Category: PenTest.

| Test | Kind | Verifies |
|------|------|----------|

### PenTestExhaustiveSearchInstancePromotedTrialInstancePredictedActual

`Jube.Test.Security.QueryExhaustive.PenTestExhaustiveSearchInstancePromotedTrialInstancePredictedActual` — 1 test.
Category: PenTest.

| Test                                                                          | Kind | Verifies                                                                          |
|-------------------------------------------------------------------------------|------|-----------------------------------------------------------------------------------|
| `Rows_AreTheLiveOnesAndErrorIsActualMinusPredictedAsync`                      | Fact | Rows are the live ones and error is actual minus predicted.                       |
| `VerbTampering_OnlyGetIsServedAndNothingIsModifiedAsync`                      | Fact | Verb tampering only get is served and nothing is modified.                        |
| `HostileQueryStringAndHeaders_NeverChangeTheResultAsync`                      | Fact | Hostile query string and headers never change the result.                         |
| `IdBoundariesAndTypeConfusion_AreRejectedOrEmptyNeverServerErrorsAsync`       | Fact | Id boundaries and type confusion are rejected or empty never server errors.       |
| `ConcurrentReads_AreDeterministicAndBoundedAsync`                             | Fact | Concurrent reads are deterministic and bounded.                                   |
| `NumericEdgeCases_NaNInfinityZeroDenominatorAndOverflowSerialiseValidlyAsync` | Fact | Numeric edge cases na n infinity zero denominator and overflow serialise validly. |
| `EmptyResult_IsAWellFormedEmptyDocumentAsync`                                 | Fact | Empty result is a well formed empty document.                                     |
| `SoftDeleted_RowsAndInstancesAreNeverReturnedAsync`                           | Fact | Soft deleted rows and instances are never returned.                               |
| `IdEnumeration_NeighbouringIdsNeverLeakAcrossTenantsAndStayUniformAsync`      | Fact | Id enumeration neighbouring ids never leak across tenants and stay uniform.       |
| `CrossTenant_ForeignIdLooksExactlyLikeANonExistentIdAsync`                    | Fact | Cross tenant foreign id looks exactly like a non existent id.                     |
| `AuthenticationAndPermissionMatrix_RefusesEveryoneButAuthorisedUsersAsync`    | Fact | Authentication and permission matrix refuses everyone but authorised users.       |
| `AuthorisedUser_ReadsOwnDataAsWellFormedJsonAsync`                            | Fact | Authorised user reads own data as well formed json.                               |

### PenTestExhaustiveSearchInstancePromotedTrialInstanceQuery

`Jube.Test.Security.QueryExhaustive.PenTestExhaustiveSearchInstancePromotedTrialInstanceQuery` — 1 test. Category:
PenTest.

| Test                                                                          | Kind | Verifies                                                                          |
|-------------------------------------------------------------------------------|------|-----------------------------------------------------------------------------------|
| `StoredJson_IsReturnedAsDataAndNeverOtherTenantsAsync`                        | Fact | Stored json is returned as data and never other tenants.                          |
| `VerbTampering_OnlyGetIsServedAndNothingIsModifiedAsync`                      | Fact | Verb tampering only get is served and nothing is modified.                        |
| `HostileQueryStringAndHeaders_NeverChangeTheResultAsync`                      | Fact | Hostile query string and headers never change the result.                         |
| `IdBoundariesAndTypeConfusion_AreRejectedOrEmptyNeverServerErrorsAsync`       | Fact | Id boundaries and type confusion are rejected or empty never server errors.       |
| `ConcurrentReads_AreDeterministicAndBoundedAsync`                             | Fact | Concurrent reads are deterministic and bounded.                                   |
| `NumericEdgeCases_NaNInfinityZeroDenominatorAndOverflowSerialiseValidlyAsync` | Fact | Numeric edge cases na n infinity zero denominator and overflow serialise validly. |
| `EmptyResult_IsAWellFormedEmptyDocumentAsync`                                 | Fact | Empty result is a well formed empty document.                                     |
| `SoftDeleted_RowsAndInstancesAreNeverReturnedAsync`                           | Fact | Soft deleted rows and instances are never returned.                               |
| `IdEnumeration_NeighbouringIdsNeverLeakAcrossTenantsAndStayUniformAsync`      | Fact | Id enumeration neighbouring ids never leak across tenants and stay uniform.       |
| `CrossTenant_ForeignIdLooksExactlyLikeANonExistentIdAsync`                    | Fact | Cross tenant foreign id looks exactly like a non existent id.                     |
| `AuthenticationAndPermissionMatrix_RefusesEveryoneButAuthorisedUsersAsync`    | Fact | Authentication and permission matrix refuses everyone but authorised users.       |
| `AuthorisedUser_ReadsOwnDataAsWellFormedJsonAsync`                            | Fact | Authorised user reads own data as well formed json.                               |

### PenTestExhaustiveSearchInstancePromotedTrialInstanceRoc

`Jube.Test.Security.QueryExhaustive.PenTestExhaustiveSearchInstancePromotedTrialInstanceRoc` — 1 test. Category:
PenTest.

| Test                                                                          | Kind | Verifies                                                                          |
|-------------------------------------------------------------------------------|------|-----------------------------------------------------------------------------------|
| `RatesAreFiniteFractionsAndOrderedByIdAsync`                                  | Fact | Rates are finite fractions and ordered by id.                                     |
| `VerbTampering_OnlyGetIsServedAndNothingIsModifiedAsync`                      | Fact | Verb tampering only get is served and nothing is modified.                        |
| `HostileQueryStringAndHeaders_NeverChangeTheResultAsync`                      | Fact | Hostile query string and headers never change the result.                         |
| `IdBoundariesAndTypeConfusion_AreRejectedOrEmptyNeverServerErrorsAsync`       | Fact | Id boundaries and type confusion are rejected or empty never server errors.       |
| `ConcurrentReads_AreDeterministicAndBoundedAsync`                             | Fact | Concurrent reads are deterministic and bounded.                                   |
| `NumericEdgeCases_NaNInfinityZeroDenominatorAndOverflowSerialiseValidlyAsync` | Fact | Numeric edge cases na n infinity zero denominator and overflow serialise validly. |
| `EmptyResult_IsAWellFormedEmptyDocumentAsync`                                 | Fact | Empty result is a well formed empty document.                                     |
| `SoftDeleted_RowsAndInstancesAreNeverReturnedAsync`                           | Fact | Soft deleted rows and instances are never returned.                               |
| `IdEnumeration_NeighbouringIdsNeverLeakAcrossTenantsAndStayUniformAsync`      | Fact | Id enumeration neighbouring ids never leak across tenants and stay uniform.       |
| `CrossTenant_ForeignIdLooksExactlyLikeANonExistentIdAsync`                    | Fact | Cross tenant foreign id looks exactly like a non existent id.                     |
| `AuthenticationAndPermissionMatrix_RefusesEveryoneButAuthorisedUsersAsync`    | Fact | Authentication and permission matrix refuses everyone but authorised users.       |
| `AuthorisedUser_ReadsOwnDataAsWellFormedJsonAsync`                            | Fact | Authorised user reads own data as well formed json.                               |

### PenTestExhaustiveSearchInstancePromotedTrialInstanceVariablePrescription

`Jube.Test.Security.QueryExhaustive.PenTestExhaustiveSearchInstancePromotedTrialInstanceVariablePrescription` — 11
| `VerbTampering_OnlyGetIsServedAndNothingIsModifiedAsync`                      | Fact | Verb tampering only get is
served and nothing is modified. |
| `HostileQueryStringAndHeaders_NeverChangeTheResultAsync`                      | Fact | Hostile query string and
headers never change the result. |
| `IdBoundariesAndTypeConfusion_AreRejectedOrEmptyNeverServerErrorsAsync`       | Fact | Id boundaries and type
confusion are rejected or empty never server errors. |
| `ConcurrentReads_AreDeterministicAndBoundedAsync`                             | Fact | Concurrent reads are
deterministic and bounded. |
| `NumericEdgeCases_NaNInfinityZeroDenominatorAndOverflowSerialiseValidlyAsync` | Fact | Numeric edge cases na n
infinity zero denominator and overflow serialise validly. |
| `EmptyResult_IsAWellFormedEmptyDocumentAsync`                                 | Fact | Empty result is a well formed
empty document. |
| `SoftDeleted_RowsAndInstancesAreNeverReturnedAsync`                           | Fact | Soft deleted rows and instances
are never returned. |
| `IdEnumeration_NeighbouringIdsNeverLeakAcrossTenantsAndStayUniformAsync`      | Fact | Id enumeration neighbouring ids
never leak across tenants and stay uniform. |
| `CrossTenant_ForeignIdLooksExactlyLikeANonExistentIdAsync`                    | Fact | Cross tenant foreign id looks
exactly like a non existent id. |
| `AuthenticationAndPermissionMatrix_RefusesEveryoneButAuthorisedUsersAsync`    | Fact | Authentication and permission
matrix refuses everyone but authorised users. |
| `AuthorisedUser_ReadsOwnDataAsWellFormedJsonAsync`                            | Fact | Authorised user reads own data
as well formed json. |
tests. Category: PenTest.

| Test | Kind | Verifies |
|------|------|----------|

### PenTestExhaustiveSearchInstancePromotionHistory

`Jube.Test.Security.QueryExhaustive.PenTestExhaustiveSearchInstancePromotionHistory` — 1 test. Category: PenTest.

| Test                                                                          | Kind | Verifies                                                                          |
|-------------------------------------------------------------------------------|------|-----------------------------------------------------------------------------------|
| `History_NeverCarriesTheSerialisedNetworkAndCountsTrialsAsync`                | Fact | History never carries the serialised network and counts trials.                   |
| `VerbTampering_OnlyGetIsServedAndNothingIsModifiedAsync`                      | Fact | Verb tampering only get is served and nothing is modified.                        |
| `HostileQueryStringAndHeaders_NeverChangeTheResultAsync`                      | Fact | Hostile query string and headers never change the result.                         |
| `IdBoundariesAndTypeConfusion_AreRejectedOrEmptyNeverServerErrorsAsync`       | Fact | Id boundaries and type confusion are rejected or empty never server errors.       |
| `ConcurrentReads_AreDeterministicAndBoundedAsync`                             | Fact | Concurrent reads are deterministic and bounded.                                   |
| `NumericEdgeCases_NaNInfinityZeroDenominatorAndOverflowSerialiseValidlyAsync` | Fact | Numeric edge cases na n infinity zero denominator and overflow serialise validly. |
| `EmptyResult_IsAWellFormedEmptyDocumentAsync`                                 | Fact | Empty result is a well formed empty document.                                     |
| `SoftDeleted_RowsAndInstancesAreNeverReturnedAsync`                           | Fact | Soft deleted rows and instances are never returned.                               |
| `IdEnumeration_NeighbouringIdsNeverLeakAcrossTenantsAndStayUniformAsync`      | Fact | Id enumeration neighbouring ids never leak across tenants and stay uniform.       |
| `CrossTenant_ForeignIdLooksExactlyLikeANonExistentIdAsync`                    | Fact | Cross tenant foreign id looks exactly like a non existent id.                     |
| `AuthenticationAndPermissionMatrix_RefusesEveryoneButAuthorisedUsersAsync`    | Fact | Authentication and permission matrix refuses everyone but authorised users.       |
| `AuthorisedUser_ReadsOwnDataAsWellFormedJsonAsync`                            | Fact | Authorised user reads own data as well formed json.                               |

### PenTestExhaustiveSearchInstanceTrialInstanceListQuery

`Jube.Test.Security.QueryExhaustive.PenTestExhaustiveSearchInstanceTrialInstanceListQuery` — 1 test. Category:
PenTest.

| Test                                                                          | Kind | Verifies                                                                          |
|-------------------------------------------------------------------------------|------|-----------------------------------------------------------------------------------|
| `Trials_AreReturnedForTheOwningInstanceOnlyAsync`                             | Fact | Trials are returned for the owning instance only.                                 |
| `VerbTampering_OnlyGetIsServedAndNothingIsModifiedAsync`                      | Fact | Verb tampering only get is served and nothing is modified.                        |
| `HostileQueryStringAndHeaders_NeverChangeTheResultAsync`                      | Fact | Hostile query string and headers never change the result.                         |
| `IdBoundariesAndTypeConfusion_AreRejectedOrEmptyNeverServerErrorsAsync`       | Fact | Id boundaries and type confusion are rejected or empty never server errors.       |
| `ConcurrentReads_AreDeterministicAndBoundedAsync`                             | Fact | Concurrent reads are deterministic and bounded.                                   |
| `NumericEdgeCases_NaNInfinityZeroDenominatorAndOverflowSerialiseValidlyAsync` | Fact | Numeric edge cases na n infinity zero denominator and overflow serialise validly. |
| `EmptyResult_IsAWellFormedEmptyDocumentAsync`                                 | Fact | Empty result is a well formed empty document.                                     |
| `SoftDeleted_RowsAndInstancesAreNeverReturnedAsync`                           | Fact | Soft deleted rows and instances are never returned.                               |
| `IdEnumeration_NeighbouringIdsNeverLeakAcrossTenantsAndStayUniformAsync`      | Fact | Id enumeration neighbouring ids never leak across tenants and stay uniform.       |
| `CrossTenant_ForeignIdLooksExactlyLikeANonExistentIdAsync`                    | Fact | Cross tenant foreign id looks exactly like a non existent id.                     |
| `AuthenticationAndPermissionMatrix_RefusesEveryoneButAuthorisedUsersAsync`    | Fact | Authentication and permission matrix refuses everyone but authorised users.       |
| `AuthorisedUser_ReadsOwnDataAsWellFormedJsonAsync`                            | Fact | Authorised user reads own data as well formed json.                               |

### PenTestExhaustiveSearchInstanceTrialInstanceVariable

`Jube.Test.Security.QueryExhaustive.PenTestExhaustiveSearchInstanceTrialInstanceVariable` — 1 test. Category: PenTest.

| Test                                                                          | Kind | Verifies                                                                          |
|-------------------------------------------------------------------------------|------|-----------------------------------------------------------------------------------|
| `StoredNames_RoundTripAsDataInJsonAsync`                                      | Fact | Stored names round trip as data in json.                                          |
| `VerbTampering_OnlyGetIsServedAndNothingIsModifiedAsync`                      | Fact | Verb tampering only get is served and nothing is modified.                        |
| `HostileQueryStringAndHeaders_NeverChangeTheResultAsync`                      | Fact | Hostile query string and headers never change the result.                         |
| `IdBoundariesAndTypeConfusion_AreRejectedOrEmptyNeverServerErrorsAsync`       | Fact | Id boundaries and type confusion are rejected or empty never server errors.       |
| `ConcurrentReads_AreDeterministicAndBoundedAsync`                             | Fact | Concurrent reads are deterministic and bounded.                                   |
| `NumericEdgeCases_NaNInfinityZeroDenominatorAndOverflowSerialiseValidlyAsync` | Fact | Numeric edge cases na n infinity zero denominator and overflow serialise validly. |
| `EmptyResult_IsAWellFormedEmptyDocumentAsync`                                 | Fact | Empty result is a well formed empty document.                                     |
| `SoftDeleted_RowsAndInstancesAreNeverReturnedAsync`                           | Fact | Soft deleted rows and instances are never returned.                               |
| `IdEnumeration_NeighbouringIdsNeverLeakAcrossTenantsAndStayUniformAsync`      | Fact | Id enumeration neighbouring ids never leak across tenants and stay uniform.       |
| `CrossTenant_ForeignIdLooksExactlyLikeANonExistentIdAsync`                    | Fact | Cross tenant foreign id looks exactly like a non existent id.                     |
| `AuthenticationAndPermissionMatrix_RefusesEveryoneButAuthorisedUsersAsync`    | Fact | Authentication and permission matrix refuses everyone but authorised users.       |
| `AuthorisedUser_ReadsOwnDataAsWellFormedJsonAsync`                            | Fact | Authorised user reads own data as well formed json.                               |

### PenTestExhaustiveSearchInstanceTrialInstanceVariableVariance

`Jube.Test.Security.QueryExhaustive.PenTestExhaustiveSearchInstanceTrialInstanceVariableVariance` — 11 tests. Category:
| `VerbTampering_OnlyGetIsServedAndNothingIsModifiedAsync`                      | Fact | Verb tampering only get is
served and nothing is modified. |
| `HostileQueryStringAndHeaders_NeverChangeTheResultAsync`                      | Fact | Hostile query string and
headers never change the result. |
| `IdBoundariesAndTypeConfusion_AreRejectedOrEmptyNeverServerErrorsAsync`       | Fact | Id boundaries and type
confusion are rejected or empty never server errors. |
| `ConcurrentReads_AreDeterministicAndBoundedAsync`                             | Fact | Concurrent reads are
deterministic and bounded. |
| `NumericEdgeCases_NaNInfinityZeroDenominatorAndOverflowSerialiseValidlyAsync` | Fact | Numeric edge cases na n
infinity zero denominator and overflow serialise validly. |
| `EmptyResult_IsAWellFormedEmptyDocumentAsync`                                 | Fact | Empty result is a well formed
empty document. |
| `SoftDeleted_RowsAndInstancesAreNeverReturnedAsync`                           | Fact | Soft deleted rows and instances
are never returned. |
| `IdEnumeration_NeighbouringIdsNeverLeakAcrossTenantsAndStayUniformAsync`      | Fact | Id enumeration neighbouring ids
never leak across tenants and stay uniform. |
| `CrossTenant_ForeignIdLooksExactlyLikeANonExistentIdAsync`                    | Fact | Cross tenant foreign id looks
exactly like a non existent id. |
| `AuthenticationAndPermissionMatrix_RefusesEveryoneButAuthorisedUsersAsync`    | Fact | Authentication and permission
matrix refuses everyone but authorised users. |
| `AuthorisedUser_ReadsOwnDataAsWellFormedJsonAsync`                            | Fact | Authorised user reads own data
as well formed json. |
PenTest.

| Test | Kind | Verifies |
|------|------|----------|

### PenTestExhaustiveSearchInstanceVariable

`Jube.Test.Security.QueryExhaustive.PenTestExhaustiveSearchInstanceVariable` — 1 test. Category: PenTest.

| Test                                                                          | Kind | Verifies                                                                          |
|-------------------------------------------------------------------------------|------|-----------------------------------------------------------------------------------|
| `VariablesAndHistograms_AreTheLiveOnesOfTheOwnerAsync`                        | Fact | Variables and histograms are the live ones of the owner.                          |
| `VerbTampering_OnlyGetIsServedAndNothingIsModifiedAsync`                      | Fact | Verb tampering only get is served and nothing is modified.                        |
| `HostileQueryStringAndHeaders_NeverChangeTheResultAsync`                      | Fact | Hostile query string and headers never change the result.                         |
| `IdBoundariesAndTypeConfusion_AreRejectedOrEmptyNeverServerErrorsAsync`       | Fact | Id boundaries and type confusion are rejected or empty never server errors.       |
| `ConcurrentReads_AreDeterministicAndBoundedAsync`                             | Fact | Concurrent reads are deterministic and bounded.                                   |
| `NumericEdgeCases_NaNInfinityZeroDenominatorAndOverflowSerialiseValidlyAsync` | Fact | Numeric edge cases na n infinity zero denominator and overflow serialise validly. |
| `EmptyResult_IsAWellFormedEmptyDocumentAsync`                                 | Fact | Empty result is a well formed empty document.                                     |
| `SoftDeleted_RowsAndInstancesAreNeverReturnedAsync`                           | Fact | Soft deleted rows and instances are never returned.                               |
| `IdEnumeration_NeighbouringIdsNeverLeakAcrossTenantsAndStayUniformAsync`      | Fact | Id enumeration neighbouring ids never leak across tenants and stay uniform.       |
| `CrossTenant_ForeignIdLooksExactlyLikeANonExistentIdAsync`                    | Fact | Cross tenant foreign id looks exactly like a non existent id.                     |
| `AuthenticationAndPermissionMatrix_RefusesEveryoneButAuthorisedUsersAsync`    | Fact | Authentication and permission matrix refuses everyone but authorised users.       |
| `AuthorisedUser_ReadsOwnDataAsWellFormedJsonAsync`                            | Fact | Authorised user reads own data as well formed json.                               |

### PenTestExhaustiveStatisticsChartGrid

`Jube.Test.Security.QueryExhaustive.PenTestExhaustiveStatisticsChartGrid` — 11 tests. Category: PenTest.
| `HostileVariableNames_AreEscapedAndNeverBecomeMarkupAsync`                 | Fact | Hostile variable names are escaped
and never become markup. |
| `VerbTampering_OnlyGetIsServedAsync`                                       | Fact | Verb tampering only get is
served. |
| `HostileQueryStringAndHeaders_NeverChangeTheChartAsync`                    | Fact | Hostile query string and headers
never change the chart. |
| `IdBoundariesAndTypeConfusion_AreRejectedOrEmptyNeverServerErrorsAsync`    | Fact | Id boundaries and type confusion
are rejected or empty never server errors. |
| `ConcurrentReads_AreDeterministicAndBoundedAsync`                          | Fact | Concurrent reads are deterministic
and bounded. |
| `NumericEdgeCases_NaNInfinityAndOverflowNeverBreakTheSvgAsync`             | Fact | Numeric edge cases na n infinity
and overflow never break the svg. |
| `EmptyStates_UnknownEmptyAndSoftDeletedAreByteForByteEqualAsync`           | Fact | Empty states unknown empty and
soft deleted are byte for byte equal. |
| `IdEnumeration_NeighbouringIdsAreOwnDataOrTheEmptyStateNeverForeignAsync`  | Fact | Id enumeration neighbouring ids
are own data or the empty state never foreign. |
| `CrossTenant_ForeignAndUnknownIdsAnswerIndistinguishablyAsync`             | Fact | Cross tenant foreign and unknown
ids answer indistinguishably. |
| `AuthenticationAndPermissionMatrix_RefusesEveryoneButAuthorisedUsersAsync` | Fact | Authentication and permission
matrix refuses everyone but authorised users. |
| `AuthorisedUser_ReadsOwnDataAsSafeSvgAsync`                                | Fact | Authorised user reads own data as
safe svg. |

| Test | Kind | Verifies |
|------|------|----------|

### PenTestExhaustiveStatisticsChartMonteCarlo

`Jube.Test.Security.QueryExhaustive.PenTestExhaustiveStatisticsChartMonteCarlo` — 1 test. Category: PenTest.

| Test                                                                       | Kind | Verifies                                                                       |
|----------------------------------------------------------------------------|------|--------------------------------------------------------------------------------|
| `VariableOfAnotherInstanceOrTenant_IsTheEmptyStateAsync`                   | Fact | Variable of another instance or tenant is the empty state.                     |
| `HostileVariableNames_AreEscapedAndNeverBecomeMarkupAsync`                 | Fact | Hostile variable names are escaped and never become markup.                    |
| `VerbTampering_OnlyGetIsServedAsync`                                       | Fact | Verb tampering only get is served.                                             |
| `HostileQueryStringAndHeaders_NeverChangeTheChartAsync`                    | Fact | Hostile query string and headers never change the chart.                       |
| `IdBoundariesAndTypeConfusion_AreRejectedOrEmptyNeverServerErrorsAsync`    | Fact | Id boundaries and type confusion are rejected or empty never server errors.    |
| `ConcurrentReads_AreDeterministicAndBoundedAsync`                          | Fact | Concurrent reads are deterministic and bounded.                                |
| `NumericEdgeCases_NaNInfinityAndOverflowNeverBreakTheSvgAsync`             | Fact | Numeric edge cases na n infinity and overflow never break the svg.             |
| `EmptyStates_UnknownEmptyAndSoftDeletedAreByteForByteEqualAsync`           | Fact | Empty states unknown empty and soft deleted are byte for byte equal.           |
| `IdEnumeration_NeighbouringIdsAreOwnDataOrTheEmptyStateNeverForeignAsync`  | Fact | Id enumeration neighbouring ids are own data or the empty state never foreign. |
| `CrossTenant_ForeignAndUnknownIdsAnswerIndistinguishablyAsync`             | Fact | Cross tenant foreign and unknown ids answer indistinguishably.                 |
| `AuthenticationAndPermissionMatrix_RefusesEveryoneButAuthorisedUsersAsync` | Fact | Authentication and permission matrix refuses everyone but authorised users.    |
| `AuthorisedUser_ReadsOwnDataAsSafeSvgAsync`                                | Fact | Authorised user reads own data as safe svg.                                    |

### PenTestExhaustiveStatisticsChartMonteCarloSummary

`Jube.Test.Security.QueryExhaustive.PenTestExhaustiveStatisticsChartMonteCarloSummary` — 1 test. Category: PenTest.

| Test                                                                       | Kind | Verifies                                                                       |
|----------------------------------------------------------------------------|------|--------------------------------------------------------------------------------|
| `VariableOfAnotherInstanceOrTenant_IsTheEmptyStateAsync`                   | Fact | Variable of another instance or tenant is the empty state.                     |
| `HostileVariableNames_AreEscapedAndNeverBecomeMarkupAsync`                 | Fact | Hostile variable names are escaped and never become markup.                    |
| `VerbTampering_OnlyGetIsServedAsync`                                       | Fact | Verb tampering only get is served.                                             |
| `HostileQueryStringAndHeaders_NeverChangeTheChartAsync`                    | Fact | Hostile query string and headers never change the chart.                       |
| `IdBoundariesAndTypeConfusion_AreRejectedOrEmptyNeverServerErrorsAsync`    | Fact | Id boundaries and type confusion are rejected or empty never server errors.    |
| `ConcurrentReads_AreDeterministicAndBoundedAsync`                          | Fact | Concurrent reads are deterministic and bounded.                                |
| `NumericEdgeCases_NaNInfinityAndOverflowNeverBreakTheSvgAsync`             | Fact | Numeric edge cases na n infinity and overflow never break the svg.             |
| `EmptyStates_UnknownEmptyAndSoftDeletedAreByteForByteEqualAsync`           | Fact | Empty states unknown empty and soft deleted are byte for byte equal.           |
| `IdEnumeration_NeighbouringIdsAreOwnDataOrTheEmptyStateNeverForeignAsync`  | Fact | Id enumeration neighbouring ids are own data or the empty state never foreign. |
| `CrossTenant_ForeignAndUnknownIdsAnswerIndistinguishablyAsync`             | Fact | Cross tenant foreign and unknown ids answer indistinguishably.                 |
| `AuthenticationAndPermissionMatrix_RefusesEveryoneButAuthorisedUsersAsync` | Fact | Authentication and permission matrix refuses everyone but authorised users.    |
| `AuthorisedUser_ReadsOwnDataAsSafeSvgAsync`                                | Fact | Authorised user reads own data as safe svg.                                    |

### PenTestExhaustiveStatisticsChartVariableClasses

`Jube.Test.Security.QueryExhaustive.PenTestExhaustiveStatisticsChartVariableClasses` — 1 test. Category: PenTest.

| Test                                                                       | Kind | Verifies                                                                       |
|----------------------------------------------------------------------------|------|--------------------------------------------------------------------------------|
| `VariableOfAnotherInstanceOrTenant_IsTheEmptyStateAsync`                   | Fact | Variable of another instance or tenant is the empty state.                     |
| `HostileVariableNames_AreEscapedAndNeverBecomeMarkupAsync`                 | Fact | Hostile variable names are escaped and never become markup.                    |
| `VerbTampering_OnlyGetIsServedAsync`                                       | Fact | Verb tampering only get is served.                                             |
| `HostileQueryStringAndHeaders_NeverChangeTheChartAsync`                    | Fact | Hostile query string and headers never change the chart.                       |
| `IdBoundariesAndTypeConfusion_AreRejectedOrEmptyNeverServerErrorsAsync`    | Fact | Id boundaries and type confusion are rejected or empty never server errors.    |
| `ConcurrentReads_AreDeterministicAndBoundedAsync`                          | Fact | Concurrent reads are deterministic and bounded.                                |
| `NumericEdgeCases_NaNInfinityAndOverflowNeverBreakTheSvgAsync`             | Fact | Numeric edge cases na n infinity and overflow never break the svg.             |
| `EmptyStates_UnknownEmptyAndSoftDeletedAreByteForByteEqualAsync`           | Fact | Empty states unknown empty and soft deleted are byte for byte equal.           |
| `IdEnumeration_NeighbouringIdsAreOwnDataOrTheEmptyStateNeverForeignAsync`  | Fact | Id enumeration neighbouring ids are own data or the empty state never foreign. |
| `CrossTenant_ForeignAndUnknownIdsAnswerIndistinguishablyAsync`             | Fact | Cross tenant foreign and unknown ids answer indistinguishably.                 |
| `AuthenticationAndPermissionMatrix_RefusesEveryoneButAuthorisedUsersAsync` | Fact | Authentication and permission matrix refuses everyone but authorised users.    |
| `AuthorisedUser_ReadsOwnDataAsSafeSvgAsync`                                | Fact | Authorised user reads own data as safe svg.                                    |

### PenTestReady

`Jube.Test.Security.QueryExhaustive.PenTestReady` — 5 tests. Category: PenTest.

| Test                                                                | Kind | Verifies                                                                |
|---------------------------------------------------------------------|------|-------------------------------------------------------------------------|
| `Anonymous_GetAnswersOnlyStatusWithNoBodyOrInformationHeadersAsync` | Fact | Anonymous get answers only status with no body or information headers.  |
| `EveryCallerGetsTheSameAnswerAsAnonymousAsync`                      | Fact | Every caller gets the same answer as anonymous.                         |
| `OtherVerbs_AreNotServedAsync`                                      | Fact | Other verbs are not served.                                             |
| `HostileQueryStringPathAndHeaders_CannotChangeTheAnswerOrLeakAsync` | Fact | Hostile query string path and headers cannot change the answer or leak. |
| `RepeatedProbing_IsCheapAndStableAsync`                             | Fact | Repeated probing is cheap and stable.                                   |

## Visualisation

### PenTestCompletions

`Jube.Test.Security.Visualisation.PenTestCompletions` — 4 tests. Category: PenTest. Collection: Database.

| Test                                                              | Kind | Verifies                                                         |
|-------------------------------------------------------------------|------|------------------------------------------------------------------|
| `AuthenticationMatrix_EveryRoute_RefusesUnauthorisedCallersAsync` | Fact | Authentication matrix every route refuses unauthorised callers.  |
| `OwnModelAndWorkflow_AreServedAndForeignOnesLookMissingAsync`     | Fact | Own model and workflow are served and foreign ones look missing. |
| `ParameterTypeConfusionAndInjection_NeverServerErrorAsync`        | Fact | Parameter type confusion and injection never server error.       |
| `CompletionMetadata_NeverExposesInternalsAndRoutesAreJsonAsync`   | Fact | Completion metadata never exposes internals and routes are json. |

### PenTestCompletionsExhaustiveInstanceDataset

`Jube.Test.Security.Visualisation.PenTestCompletionsExhaustiveInstanceDataset` — 4 tests. Category: PenTest. Collection:
Database.

| Test                                                       | Kind | Verifies                                                    |
|------------------------------------------------------------|------|-------------------------------------------------------------|
| `AuthenticationMatrix_RefusesUnauthorisedCallersAsync`     | Fact | Authentication matrix refuses unauthorised callers.         |
| `OwnDatasetIsServedAndForeignOrMissingLookTheSameAsync`    | Fact | Own dataset is served and foreign or missing look the same. |
| `ParameterTypeConfusionAndInjection_NeverServerErrorAsync` | Fact | Parameter type confusion and injection never server error.  |
| `ResponseNeverExposesInternalsAsync`                       | Fact | Response never exposes internals.                           |

### PenTestIcons

`Jube.Test.Security.Visualisation.PenTestIcons` — 5 tests. Category: PenTest. Collection: Database.

| Test                                                                       | Kind | Verifies                                                                       |
|----------------------------------------------------------------------------|------|--------------------------------------------------------------------------------|
| `AuthenticationMatrix_RefusesUnauthorisedCallers_AndServesTheListingAsync` | Fact | Authentication matrix refuses unauthorised callers and serves the listing.     |
| `Listing_CarriesOnlyPlainImageFileNamesThatExistInTheIconsDirectoryAsync`  | Fact | Listing carries only plain image file names that exist in the icons directory. |
| `ParametersAndPaths_CannotSteerTheDirectoryOrTraverseAsync`                | Fact | Parameters and paths cannot steer the directory or traverse.                   |
| `IconsDirectory_IsNotBrowsableAndOnlyServesImagesAsync`                    | Fact | Icons directory is not browsable and only serves images.                       |
| `VerbsOtherThanGet_AreRefusedAsync`                                        | Fact | Verbs other than get are refused.                                              |

### PenTestTreeChildren

`Jube.Test.Security.Visualisation.PenTestTreeChildren` — 4 tests. Category: PenTest. Collection: Database.

| Test                                                            | Kind   | Verifies                                                         |
|-----------------------------------------------------------------|--------|------------------------------------------------------------------|
| `RouteTable_ExposesExactlyTheServiceNodes`                      | Fact   | Route table exposes exactly the service nodes.                   |
| `Node_AuthenticationMatrix_RefusesUnauthorisedCallersAsync`     | Theory | Node authentication matrix refuses unauthorised callers.         |
| `AllNodes_TenantIsolation_ForeignParentsLookMissingAsync`       | Fact   | All nodes tenant isolation foreign parents look missing.         |
| `AllNodes_ParameterAbuseAndTypeConfusion_NeverServerErrorAsync` | Fact   | All nodes parameter abuse and type confusion never server error. |

### PenTestVisualisationRegistry

`Jube.Test.Security.Visualisation.PenTestVisualisationRegistry` — 13 tests. Category: PenTest. Collection: Database.

| Test                                                              | Kind | Verifies                                                          |
|-------------------------------------------------------------------|------|-------------------------------------------------------------------|
| `AuthenticationMatrix_EveryRoute_RefusesUnauthorisedCallersAsync` | Fact | Authentication matrix every route refuses unauthorised callers.   |
| `ListsAreTenantScoped_AndNeverCarryTheOtherTenantsRowsAsync`      | Fact | Lists are tenant scoped and never carry the other tenants rows.   |
| `ForeignTenantIdsAndGuids_BehaveLikeNotFoundAsync`                | Fact | Foreign tenant ids and guids behave like not found.               |
| `RecallAndAllocation_UnallocatedRegistryNeverLeaksExistenceAsync` | Fact | Recall and allocation unallocated registry never leaks existence. |
| `CreateMassAssignment_ForbiddenFieldsAreNotHonouredAsync`         | Fact | Create mass assignment forbidden fields are not honoured.         |
| `UpdateMassAssignment_ForbiddenFieldsAreNotHonouredAsync`         | Fact | Update mass assignment forbidden fields are not honoured.         |
| `LockedAndDeletedRows_CannotBeUpdatedDeletedOrRevivedAsync`       | Fact | Locked and deleted rows cannot be updated deleted or revived.     |
| `Injection_NameField_RoundTripsAsDataAndNeverExecutesAsync`       | Fact | Injection name field round trips as data and never executes.      |
| `ControlCharactersNulAndSurrogates_InNameNeverServerErrorAsync`   | Fact | Control characters nul and surrogates in name never server error. |
| `RouteAndQueryParameters_AreNeverInterpretedAsync`                | Fact | Route and query parameters are never interpreted.                 |
| `BodyTypeConfusionAndBoundaries_NeverServerErrorAsync`            | Fact | Body type confusion and boundaries never server error.            |
| `DoubleSubmitAndConcurrency_KeepTheTenantConsistentAsync`         | Fact | Double submit and concurrency keep the tenant consistent.         |
| `ResourceConsumption_OversizedBodiesAndListsAreBoundedAsync`      | Fact | Resource consumption oversized bodies and lists are bounded.      |

### PenTestVisualisationRegistryDatasourceRole

`Jube.Test.Security.Visualisation.PenTestVisualisationRegistryDatasourceRole` — 8 tests. Category: PenTest. Collection:
| `ResourceConsumption_OversizedBodiesAreRejectedAsync`                               | Fact | Resource consumption
oversized bodies are rejected. |
| `DuplicateGrants_AreIdempotent_NeverAServerError_AndOneRevokeRemovesTheAccessAsync` | Fact | Duplicate grants are
idempotent never a server error and one revoke removes the access. |
| `ReplayAndConcurrency_NeverServerErrorAsync`                                        | Fact | Replay and concurrency
never server error. |
| `InjectionAndTypeConfusion_NeverServerErrorAndCreateNothingAsync`                   | Fact | Injection and type
confusion never server error and create nothing. |
| `MassAssignment_ForbiddenFieldsAreNotHonouredAsync`                                 | Fact | Mass assignment forbidden
fields are not honoured. |
| `ListAndDelete_AreScopedToTheCallersTenantAsync`                                    | Fact | List and delete are
scoped to the callers tenant. |
| `GrantingAForeignTenantsRoleOrParent_IsRefusedAndCreatesNothingAsync`               | Fact | Granting a foreign
tenants role or parent is refused and creates nothing. |
| `AuthenticationMatrix_EveryRoute_RefusesUnauthorisedCallersAsync`                   | Fact | Authentication matrix
every route refuses unauthorised callers. |
Database.

| Test | Kind | Verifies |
|------|------|----------|

### PenTestVisualisationRegistryDatasourceSeries

`Jube.Test.Security.Visualisation.PenTestVisualisationRegistryDatasourceSeries` — 3 tests. Category: PenTest.
Collection: Database.

| Test                                                                              | Kind | Verifies                                                                           |
|-----------------------------------------------------------------------------------|------|------------------------------------------------------------------------------------|
| `AuthenticationMatrix_RefusesUnauthorisedCallers_AndAuthorisedSeesOwnSeriesAsync` | Fact | Authentication matrix refuses unauthorised callers and authorised sees own series. |
| `ForeignDatasourceIds_BehaveLikeNotFoundAsync`                                    | Fact | Foreign datasource ids behave like not found.                                      |
| `RouteParametersInjectionAndVerbs_NeverServerErrorOrMutateAsync`                  | Fact | Route parameters injection and verbs never server error or mutate.                 |

### PenTestVisualisationRegistryParameter

`Jube.Test.Security.Visualisation.PenTestVisualisationRegistryParameter` — 9 tests. Category: PenTest. Collection:
Database.

| Test                                                                      | Kind | Verifies                                                                   |
|---------------------------------------------------------------------------|------|----------------------------------------------------------------------------|
| `AuthenticationMatrix_EveryRoute_RefusesUnauthorisedCallersAsync`         | Fact | Authentication matrix every route refuses unauthorised callers.            |
| `ForeignTenantParameters_AreInvisibleAndImmutableAsync`                   | Fact | Foreign tenant parameters are invisible and immutable.                     |
| `ActiveOnlyList_FiltersUnallocatedRegistriesWithoutLeakingExistenceAsync` | Fact | Active only list filters unallocated registries without leaking existence. |
| `MassAssignment_ForbiddenFieldsAreNotHonouredAsync`                       | Fact | Mass assignment forbidden fields are not honoured.                         |
| `DefaultValueAndDataType_TypeConfusionIsRefusedAndStoredVerbatimAsync`    | Fact | Default value and data type type confusion is refused and stored verbatim. |
| `StoredDefaultValues_AreOnlyEverBoundAsTypedParametersAsync`              | Fact | Stored default values are only ever bound as typed parameters.             |
| `LockedAndDeletedParameters_CannotBeChangedOrRevivedAsync`                | Fact | Locked and deleted parameters cannot be changed or revived.                |
| `RouteBodyAndVerbAbuse_NeverServerErrorAsync`                             | Fact | Route body and verb abuse never server error.                              |
| `DoubleSubmitAndConcurrency_KeepTheRegistryConsistentAsync`               | Fact | Double submit and concurrency keep the registry consistent.                |

### PenTestVisualisationRegistryParameterRole

`Jube.Test.Security.Visualisation.PenTestVisualisationRegistryParameterRole` — 8 tests. Category: PenTest. Collection:
| `ResourceConsumption_OversizedBodiesAreRejectedAsync`                               | Fact | Resource consumption
oversized bodies are rejected. |
| `DuplicateGrants_AreIdempotent_NeverAServerError_AndOneRevokeRemovesTheAccessAsync` | Fact | Duplicate grants are
idempotent never a server error and one revoke removes the access. |
| `ReplayAndConcurrency_NeverServerErrorAsync`                                        | Fact | Replay and concurrency
never server error. |
| `InjectionAndTypeConfusion_NeverServerErrorAndCreateNothingAsync`                   | Fact | Injection and type
confusion never server error and create nothing. |
| `MassAssignment_ForbiddenFieldsAreNotHonouredAsync`                                 | Fact | Mass assignment forbidden
fields are not honoured. |
| `ListAndDelete_AreScopedToTheCallersTenantAsync`                                    | Fact | List and delete are
scoped to the callers tenant. |
| `GrantingAForeignTenantsRoleOrParent_IsRefusedAndCreatesNothingAsync`               | Fact | Granting a foreign
tenants role or parent is refused and creates nothing. |
| `AuthenticationMatrix_EveryRoute_RefusesUnauthorisedCallersAsync`                   | Fact | Authentication matrix
every route refuses unauthorised callers. |
Database.

| Test | Kind | Verifies |
|------|------|----------|

### PenTestVisualisationRegistryRole

`Jube.Test.Security.Visualisation.PenTestVisualisationRegistryRole` — 8 tests. Category: PenTest. Collection: Database.
| `ResourceConsumption_OversizedBodiesAreRejectedAsync`                               | Fact | Resource consumption
oversized bodies are rejected. |
| `DuplicateGrants_AreIdempotent_NeverAServerError_AndOneRevokeRemovesTheAccessAsync` | Fact | Duplicate grants are
idempotent never a server error and one revoke removes the access. |
| `ReplayAndConcurrency_NeverServerErrorAsync`                                        | Fact | Replay and concurrency
never server error. |
| `InjectionAndTypeConfusion_NeverServerErrorAndCreateNothingAsync`                   | Fact | Injection and type
confusion never server error and create nothing. |
| `MassAssignment_ForbiddenFieldsAreNotHonouredAsync`                                 | Fact | Mass assignment forbidden
fields are not honoured. |
| `ListAndDelete_AreScopedToTheCallersTenantAsync`                                    | Fact | List and delete are
scoped to the callers tenant. |
| `GrantingAForeignTenantsRoleOrParent_IsRefusedAndCreatesNothingAsync`               | Fact | Granting a foreign
tenants role or parent is refused and creates nothing. |
| `AuthenticationMatrix_EveryRoute_RefusesUnauthorisedCallersAsync`                   | Fact | Authentication matrix
every route refuses unauthorised callers. |

| Test | Kind | Verifies |
|------|------|----------|

### PenTestCaseKeyJournal

`Jube.Test.Security.Case.PenTestCaseKeyJournal` — 10 tests. Category: PenTest. Collection: Database.

| Test                                                                                | Kind   | Verifies                                                                               |
|-------------------------------------------------------------------------------------|--------|----------------------------------------------------------------------------------------|
| `Activations_ArePermissionedScopedAndHostileNamesAreOnlyDataAsync`                  | Fact   | Activations are permissioned scoped and hostile names are only data.                   |
| `TransactionActivations_ArePermissionedAndAForeignTransactionLooksNonExistentAsync` | Fact   | Transaction activations are permissioned and a foreign transaction looks non existent. |
| `Reads_AreAuthenticatedAndTenantIsolatedAsync`                                      | Theory | Reads are authenticated and tenant isolated.                                           |
| `Charts_DrawAHostileNameAsTextAndNeverAsMarkupAsync`                                | Fact   | Charts draw a hostile name as text and never as markup.                                |
| `Parameters_HostileValuesInEveryKeyNeverBreakOrLeakAsync`                           | Fact   | Parameters hostile values in every key never break or leak.                            |
| `Parameters_TheAddressStateKeysTheServerDoesNotReadAreIgnoredAsync`                 | Fact   | Parameters the address state keys the server does not read are ignored.                |
| `Limits_AndWindowsAreBoundedAndNeverExhaustTheServerAsync`                          | Theory | Limits and windows are bounded and never exhaust the server.                           |
| `Merge_AndFilterAreParsedDefensivelyAsync`                                          | Fact   | Merge and filter are parsed defensively.                                               |
| `CaseScopedReads_RefuseAForeignCaseWithoutDisclosingItAsync`                        | Fact   | Case scoped reads refuse a foreign case without disclosing it.                         |
| `Writes_NeedAnAntiforgeryCheckOnACookieSessionAsync`                                | Fact   | Writes need an antiforgery check on a cookie session.                                  |

### PenTestCaseKeyJournalWrites

`Jube.Test.Security.Case.PenTestCaseKeyJournalWrites` — 11 tests. Category: PenTest. Collection: Database.

| Test                                                                                 | Kind | Verifies                                                                                   |
|--------------------------------------------------------------------------------------|------|--------------------------------------------------------------------------------------------|
| `TagUpdate_IsPermissionedIsolatedAndAForeignWriteLeavesTheOtherTenantUntouchedAsync` | Fact | Tag update is permissioned isolated and a foreign write leaves the other tenant untouched. |
| `TagUpdate_HostileOversizeAndMalformedInputIsStoredAsDataOrRefusedCleanlyAsync`      | Fact | Tag update hostile oversize and malformed input is stored as data or refused cleanly.      |
| `TagRestore_CannotWriteATagTheModelDoesNotAllowAndIsIsolatedAsync`                   | Fact | Tag restore cannot write a tag the model does not allow and is isolated.                   |
| `Tags_IgnoreTamperedIdentityAndAuditFieldsAsync`                                     | Fact | Tags ignore tampered identity and audit fields.                                            |
| `Tags_ParallelIdenticalWritesProduceOneEffectAsync`                                  | Fact | Tags parallel identical writes produce one effect.                                         |
| `Writes_NeedAnAntiforgeryCheckAndNeverActOnACaseLockedToSomeoneElseAsync`            | Fact | Writes need an antiforgery check and never act on a case locked to someone else.           |
| `Pins_ArePermissionedIsolatedBoundedAndIdempotentAsync`                              | Fact | Pins are permissioned isolated bounded and idempotent.                                     |
| `TagJobs_ArePermissionedOwnedBoundedAndAForeignJobCannotBeTouchedAsync`              | Fact | Tag jobs are permissioned owned bounded and a foreign job cannot be touched.               |
| `TagJobs_BoundTheNumberOfQueuedJobsAndRefuseHostileFiltersCleanlyAsync`              | Fact | Tag jobs bound the number of queued jobs and refuse hostile filters cleanly.               |
| `Replays_ArePermissionedIsolatedBoundedAndHostileNamesAreOnlyDataAsync`              | Fact | Replays are permissioned isolated bounded and hostile names are only data.                 |
| `SavedLayout_IsPerUserBoundedAndHostileJsonIsOnlyDataAsync`                          | Fact | Saved layout is per user bounded and hostile json is only data.                            |

### PenTestGraph

`Jube.Test.Security.Graph.PenTestGraph` — 6 tests. Category: PenTest. Collection: Database.

| Test                                                               | Kind   | Verifies                                                               |
|--------------------------------------------------------------------|--------|------------------------------------------------------------------------|
| `ReadsAreRefusedToOutsidersAndNeverCrossTenantsAsync`              | Theory | Reads are refused to outsiders and never cross tenants.                |
| `HostileValuesAreDrawnAsTextAndNeverAsMarkupAsync`                 | Fact   | Hostile values are drawn as text and never as markup.                  |
| `LimitsAreEnforcedAndBadArgumentsAreClientErrorsAsync`             | Fact   | Limits are enforced and bad arguments are client errors.               |
| `WritesCarryAntiforgeryEvidenceAndStayWithTheirOwnerAsync`         | Fact   | Writes carry antiforgery evidence and stay with their owner.           |
| `CaseNodesAreInvisibleToAViewerWithoutTheCaseWorkflowRolesAsync`   | Fact   | Case nodes are invisible to a viewer without the case workflow roles.  |
| `RestrictedEdgesAreInvisibleToAViewerWithoutTheRegistryGrantAsync` | Fact   | Restricted edges are invisible to a viewer without the registry grant. |
