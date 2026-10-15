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

### PenTestAdvancedAnalyticsInstance

`Jube.Test.Security.AdvancedAnalytics.PenTestAdvancedAnalyticsInstance` — 8 tests. Category: PenTest. Collection:
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

### PenTestAdvancedAnalyticsInstanceActive

`Jube.Test.Security.AdvancedAnalytics.PenTestAdvancedAnalyticsInstanceActive` — 8 tests. Category: PenTest. Collection:
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

### PenTestAdvancedAnalyticsModel

`Jube.Test.Security.AdvancedAnalytics.PenTestAdvancedAnalyticsModel` — 7 tests. Category: PenTest. Collection: Database.

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

`Jube.Test.Security.Case.PenTestCaseFile` — 14 tests. Category: PenTest. Collection: Database.

| Test                                                                                    | Kind | Verifies                                                                                     |
|-----------------------------------------------------------------------------------------|------|----------------------------------------------------------------------------------------------|
| `Matrix_Download_EveryPrincipalAsync`                                                   | Fact | Matrix download every principal.                                                             |
| `Matrix_Upload_EveryPrincipalAsync`                                                     | Fact | Matrix upload every principal.                                                               |
| `Matrix_Remove_EveryPrincipalAsync`                                                     | Fact | Matrix remove every principal.                                                               |
| `Matrix_ListByKey_EveryPrincipalAsync`                                                  | Fact | Matrix list by key every principal.                                                          |
| `Download_ForeignGatedHiddenAndMissingIds_AreIndistinguishableAsync`                    | Fact | Download foreign gated hidden and missing ids are indistinguishable.                         |
| `Download_IdParameter_InjectionAndTypeConfusionAsync`                                   | Fact | Download id parameter injection and type confusion.                                          |
| `Upload_ToForeignGatedHiddenOrMissingCases_IsRefusedAndStoresNothingAsync`              | Fact | Upload to foreign gated hidden or missing cases is refused and stores nothing.               |
| `Upload_KeyMismatch_CannotPlantFilesOnAnotherCasesKeyAsync`                             | Fact | Upload key mismatch cannot plant files on another cases key.                                 |
| `Upload_FileNames_AreSanitisedAndNeverCarryMarkupOrPathsAsync`                          | Fact | Upload file names are sanitised and never carry markup or paths.                             |
| `Download_SpoofedContentTypes_CanNeverRenderAsActiveContentOfTheApplicationOriginAsync` | Fact | Download spoofed content types can never render as active content of the application origin. |
| `Upload_SizeAndShape_BoundariesAsync`                                                   | Fact | Upload size and shape boundaries.                                                            |
| `Remove_Replay_ForeignIds_AndTypeConfusionAsync`                                        | Fact | Remove replay foreign ids and type confusion.                                                |
| `ListByKey_Injection_MissingParameters_AndCrossKeyProbingAsync`                         | Fact | List by key injection missing parameters and cross key probing.                              |
| `OtherVerbs_AreNotAcceptedAsync`                                                        | Fact | Other verbs are not accepted.                                                                |

### PenTestCaseNote

`Jube.Test.Security.Case.PenTestCaseNote` — 11 tests. Category: PenTest. Collection: Database.

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

| Test                                                                | Kind | Verifies                                                            |
|---------------------------------------------------------------------|------|---------------------------------------------------------------------|
| `Matrix_ReadRoutes_EveryPrincipalAsync`                             | Fact | Matrix read routes every principal.                                 |
| `Matrix_WriteRoutes_EveryPrincipalAsync`                            | Fact | Matrix write routes every principal.                                |
| `ForeignAndGatedIds_BehaveLikeMissing_OnEveryReadRouteAsync`        | Fact | Foreign and gated ids behave like missing on every read route.      |
| `Writes_ToForeignRowsAndParents_AreRefused_AndChangeNothingAsync`   | Fact | Writes to foreign rows and parents are refused and change nothing.  |
| `MassAssignment_ServerOwnedFieldsAreNeverHonouredAsync`             | Fact | Mass assignment server owned fields are never honoured.             |
| `TextFields_Injection_IsStoredAsDataOrRefusedAsync`                 | Fact | Text fields injection is stored as data or refused.                 |
| `TextFields_Limits_AndOversizeBodies_AreEnforcedWithoutErrorsAsync` | Fact | Text fields limits and oversize bodies are enforced without errors. |
| `Boundaries_TypeConfusion_AndMalformedBodies_FailSafelyAsync`       | Fact | Boundaries type confusion and malformed bodies fail safely.         |
| `RouteParameters_Injection_AndTypeConfusion_NeverErrorAsync`        | Fact | Route parameters injection and type confusion never error.          |
| `SoftDelete_Replay_LockedRows_AndConcurrentDuplicatesAsync`         | Fact | Soft delete replay locked rows and concurrent duplicates.           |
| `ConcurrentCreates_OfTheSameName_ProduceOneRowAsync`                | Fact | Concurrent creates of the same name produce one row.                |
| `Verbs_AndOverrides_AreNotAcceptedAsync`                            | Fact | Verbs and overrides are not accepted.                               |

### PenTestCaseWorkflowActionRole

`Jube.Test.Security.Case.PenTestCaseWorkflowActionRole` — 9 tests. Category: PenTest. Collection: Database.

| Test                                                               | Kind | Verifies                                                           |
|--------------------------------------------------------------------|------|--------------------------------------------------------------------|
| `Matrix_EveryPrincipal_OnEveryRouteAsync`                          | Fact | Matrix every principal on every route.                             |
| `Create_WithForeignReferences_IsRefused_AndChangesNothingAsync`    | Fact | Create with foreign references is refused and changes nothing.     |
| `List_ForForeignAndMissingParents_IsIndistinguishableAsync`        | Fact | List for foreign and missing parents is indistinguishable.         |
| `Delete_ForeignIds_AreRefused_ReplayIsRefused_AndRowsSurviveAsync` | Fact | Delete foreign ids are refused replay is refused and rows survive. |
| `DuplicateAndConcurrentGrants_DoNotMultiplyAsync`                  | Fact | Duplicate and concurrent grants do not multiply.                   |
| `MassAssignment_ServerOwnedFieldsAreNeverHonouredAsync`            | Fact | Mass assignment server owned fields are never honoured.            |
| `Boundaries_TypeConfusion_AndInjection_FailSafelyAsync`            | Fact | Boundaries type confusion and injection fail safely.               |
| `RouteParameters_Injection_AndTypeConfusion_NeverErrorAsync`       | Fact | Route parameters injection and type confusion never error.         |
| `Verbs_AndOverrides_AreNotAcceptedAsync`                           | Fact | Verbs and overrides are not accepted.                              |

### PenTestCaseWorkflowDisplay

`Jube.Test.Security.Case.PenTestCaseWorkflowDisplay` — 12 tests. Category: PenTest. Collection: Database.

| Test                                                                | Kind | Verifies                                                            |
|---------------------------------------------------------------------|------|---------------------------------------------------------------------|
| `Matrix_ReadRoutes_EveryPrincipalAsync`                             | Fact | Matrix read routes every principal.                                 |
| `Matrix_WriteRoutes_EveryPrincipalAsync`                            | Fact | Matrix write routes every principal.                                |
| `ForeignAndGatedIds_BehaveLikeMissing_OnEveryReadRouteAsync`        | Fact | Foreign and gated ids behave like missing on every read route.      |
| `Writes_ToForeignRowsAndParents_AreRefused_AndChangeNothingAsync`   | Fact | Writes to foreign rows and parents are refused and change nothing.  |
| `MassAssignment_ServerOwnedFieldsAreNeverHonouredAsync`             | Fact | Mass assignment server owned fields are never honoured.             |
| `TextFields_Injection_IsStoredAsDataOrRefusedAsync`                 | Fact | Text fields injection is stored as data or refused.                 |
| `TextFields_Limits_AndOversizeBodies_AreEnforcedWithoutErrorsAsync` | Fact | Text fields limits and oversize bodies are enforced without errors. |
| `Boundaries_TypeConfusion_AndMalformedBodies_FailSafelyAsync`       | Fact | Boundaries type confusion and malformed bodies fail safely.         |
| `RouteParameters_Injection_AndTypeConfusion_NeverErrorAsync`        | Fact | Route parameters injection and type confusion never error.          |
| `SoftDelete_Replay_LockedRows_AndConcurrentDuplicatesAsync`         | Fact | Soft delete replay locked rows and concurrent duplicates.           |
| `ConcurrentCreates_OfTheSameName_ProduceOneRowAsync`                | Fact | Concurrent creates of the same name produce one row.                |
| `Verbs_AndOverrides_AreNotAcceptedAsync`                            | Fact | Verbs and overrides are not accepted.                               |

### PenTestCaseWorkflowDisplayRole

`Jube.Test.Security.Case.PenTestCaseWorkflowDisplayRole` — 9 tests. Category: PenTest. Collection: Database.

| Test                                                               | Kind | Verifies                                                           |
|--------------------------------------------------------------------|------|--------------------------------------------------------------------|
| `Matrix_EveryPrincipal_OnEveryRouteAsync`                          | Fact | Matrix every principal on every route.                             |
| `Create_WithForeignReferences_IsRefused_AndChangesNothingAsync`    | Fact | Create with foreign references is refused and changes nothing.     |
| `List_ForForeignAndMissingParents_IsIndistinguishableAsync`        | Fact | List for foreign and missing parents is indistinguishable.         |
| `Delete_ForeignIds_AreRefused_ReplayIsRefused_AndRowsSurviveAsync` | Fact | Delete foreign ids are refused replay is refused and rows survive. |
| `DuplicateAndConcurrentGrants_DoNotMultiplyAsync`                  | Fact | Duplicate and concurrent grants do not multiply.                   |
| `MassAssignment_ServerOwnedFieldsAreNeverHonouredAsync`            | Fact | Mass assignment server owned fields are never honoured.            |
| `Boundaries_TypeConfusion_AndInjection_FailSafelyAsync`            | Fact | Boundaries type confusion and injection fail safely.               |
| `RouteParameters_Injection_AndTypeConfusion_NeverErrorAsync`       | Fact | Route parameters injection and type confusion never error.         |
| `Verbs_AndOverrides_AreNotAcceptedAsync`                           | Fact | Verbs and overrides are not accepted.                              |

### PenTestCaseWorkflowFilter

`Jube.Test.Security.Case.PenTestCaseWorkflowFilter` — 12 tests. Category: PenTest. Collection: Database.

| Test                                                                | Kind | Verifies                                                            |
|---------------------------------------------------------------------|------|---------------------------------------------------------------------|
| `Matrix_ReadRoutes_EveryPrincipalAsync`                             | Fact | Matrix read routes every principal.                                 |
| `Matrix_WriteRoutes_EveryPrincipalAsync`                            | Fact | Matrix write routes every principal.                                |
| `ForeignAndGatedIds_BehaveLikeMissing_OnEveryReadRouteAsync`        | Fact | Foreign and gated ids behave like missing on every read route.      |
| `Writes_ToForeignRowsAndParents_AreRefused_AndChangeNothingAsync`   | Fact | Writes to foreign rows and parents are refused and change nothing.  |
| `MassAssignment_ServerOwnedFieldsAreNeverHonouredAsync`             | Fact | Mass assignment server owned fields are never honoured.             |
| `TextFields_Injection_IsStoredAsDataOrRefusedAsync`                 | Fact | Text fields injection is stored as data or refused.                 |
| `TextFields_Limits_AndOversizeBodies_AreEnforcedWithoutErrorsAsync` | Fact | Text fields limits and oversize bodies are enforced without errors. |
| `Boundaries_TypeConfusion_AndMalformedBodies_FailSafelyAsync`       | Fact | Boundaries type confusion and malformed bodies fail safely.         |
| `RouteParameters_Injection_AndTypeConfusion_NeverErrorAsync`        | Fact | Route parameters injection and type confusion never error.          |
| `SoftDelete_Replay_LockedRows_AndConcurrentDuplicatesAsync`         | Fact | Soft delete replay locked rows and concurrent duplicates.           |
| `ConcurrentCreates_OfTheSameName_ProduceOneRowAsync`                | Fact | Concurrent creates of the same name produce one row.                |
| `Verbs_AndOverrides_AreNotAcceptedAsync`                            | Fact | Verbs and overrides are not accepted.                               |

### PenTestCaseWorkflowFilterRole

`Jube.Test.Security.Case.PenTestCaseWorkflowFilterRole` — 6 tests. Category: PenTest. Collection: Database.

| Test                                                                     | Kind | Verifies                                                                   |
|--------------------------------------------------------------------------|------|----------------------------------------------------------------------------|
| `Get_ByParentGuid_IsPermissionedAndTenantIsolatedAsync`                  | Fact | Get by parent guid is permissioned and tenant isolated.                    |
| `Post_IsPermissionedAndRefusesForeignParentsAndRolesAsync`               | Fact | Post is permissioned and refuses foreign parents and roles.                |
| `Post_HappyPathAndMassAssignment_PersistOnlyServerControlledFieldsAsync` | Fact | Post happy path and mass assignment persist only server controlled fields. |
| `Post_DuplicateGrants_DoNotCreateDuplicateRowsAsync`                     | Fact | Post duplicate grants do not create duplicate rows.                        |
| `Post_TypeConfusionAndStructure_FailSafelyAsync`                         | Fact | Post type confusion and structure fail safely.                             |
| `Delete_IsPermissionedAndObjectLevelAuthorisedAsync`                     | Fact | Delete is permissioned and object level authorised.                        |

### PenTestCaseWorkflowForm

`Jube.Test.Security.Case.PenTestCaseWorkflowForm` — 12 tests. Category: PenTest. Collection: Database.

| Test                                                            | Kind | Verifies                                                         |
|-----------------------------------------------------------------|------|------------------------------------------------------------------|
| `Get_List_IsPermissionedAndTenantIsolatedAsync`                 | Fact | Get list is permissioned and tenant isolated.                    |
| `Get_ById_IsPermissionedAndObjectLevelAuthorisedAsync`          | Fact | Get by id is permissioned and object level authorised.           |
| `Get_ByWorkflowRoutes_ArePermissionedAndTenantIsolatedAsync`    | Fact | Get by workflow routes are permissioned and tenant isolated.     |
| `Post_IsPermissionedAndRefusesForeignParentsAsync`              | Fact | Post is permissioned and refuses foreign parents.                |
| `Post_MassAssignment_ForbiddenFieldsAreNotHonouredAsync`        | Fact | Post mass assignment forbidden fields are not honoured.          |
| `Post_StoredHostilePayloads_AreDataAndRoundTripFaithfullyAsync` | Fact | Post stored hostile payloads are data and round trip faithfully. |
| `Post_TypeConfusionAndBoundaries_FailSafelyAsync`               | Fact | Post type confusion and boundaries fail safely.                  |
| `Post_LengthLimitsAndDuplicateNames_AreEnforcedAsync`           | Fact | Post length limits and duplicate names are enforced.             |
| `Put_IsPermissionedAndObjectLevelAuthorisedAsync`               | Fact | Put is permissioned and object level authorised.                 |
| `Put_MassAssignment_LockingAndSoftDelete_BehaveSafelyAsync`     | Fact | Put mass assignment locking and soft delete behave safely.       |
| `Put_TypeConfusionAndInjection_FailSafelyAsync`                 | Fact | Put type confusion and injection fail safely.                    |
| `Delete_IsPermissionedAndObjectLevelAuthorisedAsync`            | Fact | Delete is permissioned and object level authorised.              |

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

| Test                                                                     | Kind | Verifies                                                                   |
|--------------------------------------------------------------------------|------|----------------------------------------------------------------------------|
| `Get_ByParentGuid_IsPermissionedAndTenantIsolatedAsync`                  | Fact | Get by parent guid is permissioned and tenant isolated.                    |
| `Post_IsPermissionedAndRefusesForeignParentsAndRolesAsync`               | Fact | Post is permissioned and refuses foreign parents and roles.                |
| `Post_HappyPathAndMassAssignment_PersistOnlyServerControlledFieldsAsync` | Fact | Post happy path and mass assignment persist only server controlled fields. |
| `Post_DuplicateGrants_DoNotCreateDuplicateRowsAsync`                     | Fact | Post duplicate grants do not create duplicate rows.                        |
| `Post_TypeConfusionAndStructure_FailSafelyAsync`                         | Fact | Post type confusion and structure fail safely.                             |
| `Delete_IsPermissionedAndObjectLevelAuthorisedAsync`                     | Fact | Delete is permissioned and object level authorised.                        |

### PenTestCaseWorkflowMacro

`Jube.Test.Security.Case.PenTestCaseWorkflowMacro` — 13 tests. Category: PenTest. Collection: Database.

| Test                                                            | Kind | Verifies                                                         |
|-----------------------------------------------------------------|------|------------------------------------------------------------------|
| `Get_List_IsPermissionedAndTenantIsolatedAsync`                 | Fact | Get list is permissioned and tenant isolated.                    |
| `Get_ById_IsPermissionedAndObjectLevelAuthorisedAsync`          | Fact | Get by id is permissioned and object level authorised.           |
| `Get_ByWorkflowRoutes_ArePermissionedAndTenantIsolatedAsync`    | Fact | Get by workflow routes are permissioned and tenant isolated.     |
| `Post_IsPermissionedAndRefusesForeignParentsAsync`              | Fact | Post is permissioned and refuses foreign parents.                |
| `Post_MassAssignment_ForbiddenFieldsAreNotHonouredAsync`        | Fact | Post mass assignment forbidden fields are not honoured.          |
| `Post_StoredHostilePayloads_AreDataAndRoundTripFaithfullyAsync` | Fact | Post stored hostile payloads are data and round trip faithfully. |
| `Post_TypeConfusionAndBoundaries_FailSafelyAsync`               | Fact | Post type confusion and boundaries fail safely.                  |
| `Post_LengthLimitsAndDuplicateNames_AreEnforcedAsync`           | Fact | Post length limits and duplicate names are enforced.             |
| `Put_IsPermissionedAndObjectLevelAuthorisedAsync`               | Fact | Put is permissioned and object level authorised.                 |
| `Put_MassAssignment_LockingAndSoftDelete_BehaveSafelyAsync`     | Fact | Put mass assignment locking and soft delete behave safely.       |
| `Put_TypeConfusionAndInjection_FailSafelyAsync`                 | Fact | Put type confusion and injection fail safely.                    |
| `Delete_IsPermissionedAndObjectLevelAuthorisedAsync`            | Fact | Delete is permissioned and object level authorised.              |
| `Post_ImageLocation_RejectsPathsAndMarkupAsync`                 | Fact | Post image location rejects paths and markup.                    |

### PenTestCaseWorkflowMacroRole

`Jube.Test.Security.Case.PenTestCaseWorkflowMacroRole` — 6 tests. Category: PenTest. Collection: Database.

| Test                                                                     | Kind | Verifies                                                                   |
|--------------------------------------------------------------------------|------|----------------------------------------------------------------------------|
| `Get_ByParentGuid_IsPermissionedAndTenantIsolatedAsync`                  | Fact | Get by parent guid is permissioned and tenant isolated.                    |
| `Post_IsPermissionedAndRefusesForeignParentsAndRolesAsync`               | Fact | Post is permissioned and refuses foreign parents and roles.                |
| `Post_HappyPathAndMassAssignment_PersistOnlyServerControlledFieldsAsync` | Fact | Post happy path and mass assignment persist only server controlled fields. |
| `Post_DuplicateGrants_DoNotCreateDuplicateRowsAsync`                     | Fact | Post duplicate grants do not create duplicate rows.                        |
| `Post_TypeConfusionAndStructure_FailSafelyAsync`                         | Fact | Post type confusion and structure fail safely.                             |
| `Delete_IsPermissionedAndObjectLevelAuthorisedAsync`                     | Fact | Delete is permissioned and object level authorised.                        |

### PenTestCaseWorkflowPriority

`Jube.Test.Security.Case.PenTestCaseWorkflowPriority` — 1 test. Category: PenTest. Collection: Database.

| Test                                                              | Kind | Verifies                                                          |
|-------------------------------------------------------------------|------|-------------------------------------------------------------------|
| `Get_Priorities_ArePermissionedAndLeakNothingAboutWorkflowsAsync` | Fact | Get priorities are permissioned and leak nothing about workflows. |

### PenTestCaseWorkflowRole

`Jube.Test.Security.Case.PenTestCaseWorkflowRole` — 6 tests. Category: PenTest. Collection: Database.

| Test                                                                     | Kind | Verifies                                                                   |
|--------------------------------------------------------------------------|------|----------------------------------------------------------------------------|
| `Get_ByParentGuid_IsPermissionedAndTenantIsolatedAsync`                  | Fact | Get by parent guid is permissioned and tenant isolated.                    |
| `Post_IsPermissionedAndRefusesForeignParentsAndRolesAsync`               | Fact | Post is permissioned and refuses foreign parents and roles.                |
| `Post_HappyPathAndMassAssignment_PersistOnlyServerControlledFieldsAsync` | Fact | Post happy path and mass assignment persist only server controlled fields. |
| `Post_DuplicateGrants_DoNotCreateDuplicateRowsAsync`                     | Fact | Post duplicate grants do not create duplicate rows.                        |
| `Post_TypeConfusionAndStructure_FailSafelyAsync`                         | Fact | Post type confusion and structure fail safely.                             |
| `Delete_IsPermissionedAndObjectLevelAuthorisedAsync`                     | Fact | Delete is permissioned and object level authorised.                        |

### PenTestCaseWorkflowStatus

`Jube.Test.Security.Case.PenTestCaseWorkflowStatus` — 13 tests. Category: PenTest. Collection: Database.

| Test                                                            | Kind | Verifies                                                         |
|-----------------------------------------------------------------|------|------------------------------------------------------------------|
| `Get_List_IsPermissionedAndTenantIsolatedAsync`                 | Fact | Get list is permissioned and tenant isolated.                    |
| `Get_ById_IsPermissionedAndObjectLevelAuthorisedAsync`          | Fact | Get by id is permissioned and object level authorised.           |
| `Get_ByWorkflowRoutes_ArePermissionedAndTenantIsolatedAsync`    | Fact | Get by workflow routes are permissioned and tenant isolated.     |
| `Post_IsPermissionedAndRefusesForeignParentsAsync`              | Fact | Post is permissioned and refuses foreign parents.                |
| `Post_MassAssignment_ForbiddenFieldsAreNotHonouredAsync`        | Fact | Post mass assignment forbidden fields are not honoured.          |
| `Post_StoredHostilePayloads_AreDataAndRoundTripFaithfullyAsync` | Fact | Post stored hostile payloads are data and round trip faithfully. |
| `Post_TypeConfusionAndBoundaries_FailSafelyAsync`               | Fact | Post type confusion and boundaries fail safely.                  |
| `Post_LengthLimitsAndDuplicateNames_AreEnforcedAsync`           | Fact | Post length limits and duplicate names are enforced.             |
| `Put_IsPermissionedAndObjectLevelAuthorisedAsync`               | Fact | Put is permissioned and object level authorised.                 |
| `Put_MassAssignment_LockingAndSoftDelete_BehaveSafelyAsync`     | Fact | Put mass assignment locking and soft delete behave safely.       |
| `Put_TypeConfusionAndInjection_FailSafelyAsync`                 | Fact | Put type confusion and injection fail safely.                    |
| `Delete_IsPermissionedAndObjectLevelAuthorisedAsync`            | Fact | Delete is permissioned and object level authorised.              |
| `Post_Priority_IsBoundedAndDuplicatesAreAllowedAsync`           | Fact | Post priority is bounded and duplicates are allowed.             |

### PenTestCaseWorkflowStatusRole

`Jube.Test.Security.Case.PenTestCaseWorkflowStatusRole` — 6 tests. Category: PenTest. Collection: Database.

| Test                                                                     | Kind | Verifies                                                                   |
|--------------------------------------------------------------------------|------|----------------------------------------------------------------------------|
| `Get_ByParentGuid_IsPermissionedAndTenantIsolatedAsync`                  | Fact | Get by parent guid is permissioned and tenant isolated.                    |
| `Post_IsPermissionedAndRefusesForeignParentsAndRolesAsync`               | Fact | Post is permissioned and refuses foreign parents and roles.                |
| `Post_HappyPathAndMassAssignment_PersistOnlyServerControlledFieldsAsync` | Fact | Post happy path and mass assignment persist only server controlled fields. |
| `Post_DuplicateGrants_DoNotCreateDuplicateRowsAsync`                     | Fact | Post duplicate grants do not create duplicate rows.                        |
| `Post_TypeConfusionAndStructure_FailSafelyAsync`                         | Fact | Post type confusion and structure fail safely.                             |
| `Delete_IsPermissionedAndObjectLevelAuthorisedAsync`                     | Fact | Delete is permissioned and object level authorised.                        |

### PenTestCaseWorkflowXPath

`Jube.Test.Security.Case.PenTestCaseWorkflowXPath` — 13 tests. Category: PenTest. Collection: Database.

| Test                                                            | Kind | Verifies                                                         |
|-----------------------------------------------------------------|------|------------------------------------------------------------------|
| `Get_List_IsPermissionedAndTenantIsolatedAsync`                 | Fact | Get list is permissioned and tenant isolated.                    |
| `Get_ById_IsPermissionedAndObjectLevelAuthorisedAsync`          | Fact | Get by id is permissioned and object level authorised.           |
| `Get_ByWorkflowRoutes_ArePermissionedAndTenantIsolatedAsync`    | Fact | Get by workflow routes are permissioned and tenant isolated.     |
| `Post_IsPermissionedAndRefusesForeignParentsAsync`              | Fact | Post is permissioned and refuses foreign parents.                |
| `Post_MassAssignment_ForbiddenFieldsAreNotHonouredAsync`        | Fact | Post mass assignment forbidden fields are not honoured.          |
| `Post_StoredHostilePayloads_AreDataAndRoundTripFaithfullyAsync` | Fact | Post stored hostile payloads are data and round trip faithfully. |
| `Post_TypeConfusionAndBoundaries_FailSafelyAsync`               | Fact | Post type confusion and boundaries fail safely.                  |
| `Post_LengthLimitsAndDuplicateNames_AreEnforcedAsync`           | Fact | Post length limits and duplicate names are enforced.             |
| `Put_IsPermissionedAndObjectLevelAuthorisedAsync`               | Fact | Put is permissioned and object level authorised.                 |
| `Put_MassAssignment_LockingAndSoftDelete_BehaveSafelyAsync`     | Fact | Put mass assignment locking and soft delete behave safely.       |
| `Put_TypeConfusionAndInjection_FailSafelyAsync`                 | Fact | Put type confusion and injection fail safely.                    |
| `Delete_IsPermissionedAndObjectLevelAuthorisedAsync`            | Fact | Delete is permissioned and object level authorised.              |
| `Post_ColourFields_RejectMarkupAndStyleBreakoutsAsync`          | Fact | Post colour fields reject markup and style breakouts.            |

### PenTestCaseWorkflowXPathRole

`Jube.Test.Security.Case.PenTestCaseWorkflowXPathRole` — 6 tests. Category: PenTest. Collection: Database.

| Test                                                                     | Kind | Verifies                                                                   |
|--------------------------------------------------------------------------|------|----------------------------------------------------------------------------|
| `Get_ByParentGuid_IsPermissionedAndTenantIsolatedAsync`                  | Fact | Get by parent guid is permissioned and tenant isolated.                    |
| `Post_IsPermissionedAndRefusesForeignParentsAndRolesAsync`               | Fact | Post is permissioned and refuses foreign parents and roles.                |
| `Post_HappyPathAndMassAssignment_PersistOnlyServerControlledFieldsAsync` | Fact | Post happy path and mass assignment persist only server controlled fields. |
| `Post_DuplicateGrants_DoNotCreateDuplicateRowsAsync`                     | Fact | Post duplicate grants do not create duplicate rows.                        |
| `Post_TypeConfusionAndStructure_FailSafelyAsync`                         | Fact | Post type confusion and structure fail safely.                             |
| `Delete_IsPermissionedAndObjectLevelAuthorisedAsync`                     | Fact | Delete is permissioned and object level authorised.                        |

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

`Jube.Test.Security.DynamicSql.PenTestVisualisationRegistryDatasource` — 9 tests. Category: PenTest.

| Test                                                                         | Kind   | Verifies                                                                           |
|------------------------------------------------------------------------------|--------|------------------------------------------------------------------------------------|
| `AuthenticationAndAuthorisationMatrixAsync`                                  | Theory | Authentication and authorisation matrix.                                           |
| `SavePath_RejectsEveryWriteAsync`                                            | Fact   | Save path rejects every write.                                                     |
| `UpdatePath_RejectsEveryWriteAsync`                                          | Fact   | Update path rejects every write.                                                   |
| `LegitimateSelectDatasourcesCanStillBeSavedAndUpdatedAsync`                  | Fact   | Legitimate select datasources can still be saved and updated.                      |
| `Idor_ForeignIdsBehaveLikeNotFoundAndNothingChangesAsync`                    | Fact   | Idor foreign ids behave like not found and nothing changes.                        |
| `MassAssignment_ServerOwnedFieldsAreNotHonouredAsync`                        | Fact   | Mass assignment server owned fields are not honoured.                              |
| `InjectionInEveryStringFieldAndRouteParameter_IsStoredAsDataOrRejectedAsync` | Fact   | Injection in every string field and route parameter is stored as data or rejected. |
| `TypeConfusionAndMalformedBodies_NeverProduceA500Async`                      | Fact   | Type confusion and malformed bodies never produce a 500.                           |
| `BusinessFlow_DeleteThenUpdate_DoesNotResurrect_AndDoubleSubmitIsSafeAsync`  | Fact   | Business flow delete then update does not resurrect and double submit is safe.     |

### PenTestVisualisationRegistryDatasourceCommandExecution

`Jube.Test.Security.DynamicSql.PenTestVisualisationRegistryDatasourceCommandExecution` — 8 tests. Category: PenTest.

| Test                                                                                    | Kind | Verifies                                                                                      |
|-----------------------------------------------------------------------------------------|------|-----------------------------------------------------------------------------------------------|
| `AuthenticationAndAuthorisationMatrixAsync`                                             | Fact | Authentication and authorisation matrix.                                                      |
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

| Test                                                                                | Kind | Verifies                                                                                |
|-------------------------------------------------------------------------------------|------|-----------------------------------------------------------------------------------------|
| `AuthenticationAndAuthorisationMatrix_RefusesEveryone_ButThePermittedUserAsync`     | Fact | Authentication and authorisation matrix refuses everyone but the permitted user.        |
| `Lifecycle_CreateReadUpdateDelete_HasSoftDeleteAndVersionSemanticsAsync`            | Fact | Lifecycle create read update delete has soft delete and version semantics.              |
| `CrossTenant_ReadUpdateDeleteReset_BehaveLikeNotFoundAsync`                         | Fact | Cross tenant read update delete reset behave like not found.                            |
| `CrossTenantParent_CreateUnderForeignParent_AndMoveToForeignParent_AreRefusedAsync` | Fact | Cross tenant parent create under foreign parent and move to foreign parent are refused. |
| `MassAssignment_ServerOwnedFields_AreNotHonouredAsync`                              | Fact | Mass assignment server owned fields are not honoured.                                   |
| `Injection_EveryTextField_IsStoredOrRejectedAsDataAsync`                            | Fact | Injection every text field is stored or rejected as data.                               |
| `JsonColumns_RefuseInvalidJson_InsteadOfFailingInTheDatabaseAsync`                  | Fact | Json columns refuse invalid json instead of failing in the database.                    |
| `LengthBoundaries_AcceptTheMaximum_AndRefuseOneMoreAsync`                           | Fact | Length boundaries accept the maximum and refuse one more.                               |
| `TypeConfusionAndBoundaries_NeverProduceServerErrors_OrSideEffectsAsync`            | Fact | Type confusion and boundaries never produce server errors or side effects.              |
| `MalformedBodiesAndContentTypes_AreRejectedWithoutServerErrorsAsync`                | Fact | Malformed bodies and content types are rejected without server errors.                  |
| `RouteAndQueryParameterAbuse_NeverServerErrors_OrTouchesRowsAsync`                  | Fact | Route and query parameter abuse never server errors or touches rows.                    |
| `LockedRows_CannotBeUpdatedOrDeletedAsync`                                          | Fact | Locked rows cannot be updated or deleted.                                               |
| `ConcurrentUpdatesAndDoubleSubmit_NeverServerError_AndStayConsistentAsync`          | Fact | Concurrent updates and double submit never server error and stay consistent.            |
| `OversizedBodies_AreRefusedQuicklyAsync`                                            | Fact | Oversized bodies are refused quickly.                                                   |

### PenTestEntityAnalysisModelAbstractionCalculation

`Jube.Test.Security.Model.PenTestEntityAnalysisModelAbstractionCalculation` — 14 tests. Category: PenTest. Collection:
Database.

| Test                                                                                | Kind | Verifies                                                                                |
|-------------------------------------------------------------------------------------|------|-----------------------------------------------------------------------------------------|
| `AuthenticationAndAuthorisationMatrix_RefusesEveryone_ButThePermittedUserAsync`     | Fact | Authentication and authorisation matrix refuses everyone but the permitted user.        |
| `Lifecycle_CreateReadUpdateDelete_HasSoftDeleteAndVersionSemanticsAsync`            | Fact | Lifecycle create read update delete has soft delete and version semantics.              |
| `CrossTenant_ReadUpdateDeleteReset_BehaveLikeNotFoundAsync`                         | Fact | Cross tenant read update delete reset behave like not found.                            |
| `CrossTenantParent_CreateUnderForeignParent_AndMoveToForeignParent_AreRefusedAsync` | Fact | Cross tenant parent create under foreign parent and move to foreign parent are refused. |
| `MassAssignment_ServerOwnedFields_AreNotHonouredAsync`                              | Fact | Mass assignment server owned fields are not honoured.                                   |
| `Injection_EveryTextField_IsStoredOrRejectedAsDataAsync`                            | Fact | Injection every text field is stored or rejected as data.                               |
| `JsonColumns_RefuseInvalidJson_InsteadOfFailingInTheDatabaseAsync`                  | Fact | Json columns refuse invalid json instead of failing in the database.                    |
| `LengthBoundaries_AcceptTheMaximum_AndRefuseOneMoreAsync`                           | Fact | Length boundaries accept the maximum and refuse one more.                               |
| `TypeConfusionAndBoundaries_NeverProduceServerErrors_OrSideEffectsAsync`            | Fact | Type confusion and boundaries never produce server errors or side effects.              |
| `MalformedBodiesAndContentTypes_AreRejectedWithoutServerErrorsAsync`                | Fact | Malformed bodies and content types are rejected without server errors.                  |
| `RouteAndQueryParameterAbuse_NeverServerErrors_OrTouchesRowsAsync`                  | Fact | Route and query parameter abuse never server errors or touches rows.                    |
| `LockedRows_CannotBeUpdatedOrDeletedAsync`                                          | Fact | Locked rows cannot be updated or deleted.                                               |
| `ConcurrentUpdatesAndDoubleSubmit_NeverServerError_AndStayConsistentAsync`          | Fact | Concurrent updates and double submit never server error and stay consistent.            |
| `OversizedBodies_AreRefusedQuicklyAsync`                                            | Fact | Oversized bodies are refused quickly.                                                   |

### PenTestEntityAnalysisModelAbstractionRule

`Jube.Test.Security.Model.PenTestEntityAnalysisModelAbstractionRule` — 14 tests. Category: PenTest. Collection:
Database.

| Test                                                                                | Kind | Verifies                                                                                |
|-------------------------------------------------------------------------------------|------|-----------------------------------------------------------------------------------------|
| `AuthenticationAndAuthorisationMatrix_RefusesEveryone_ButThePermittedUserAsync`     | Fact | Authentication and authorisation matrix refuses everyone but the permitted user.        |
| `Lifecycle_CreateReadUpdateDelete_HasSoftDeleteAndVersionSemanticsAsync`            | Fact | Lifecycle create read update delete has soft delete and version semantics.              |
| `CrossTenant_ReadUpdateDeleteReset_BehaveLikeNotFoundAsync`                         | Fact | Cross tenant read update delete reset behave like not found.                            |
| `CrossTenantParent_CreateUnderForeignParent_AndMoveToForeignParent_AreRefusedAsync` | Fact | Cross tenant parent create under foreign parent and move to foreign parent are refused. |
| `MassAssignment_ServerOwnedFields_AreNotHonouredAsync`                              | Fact | Mass assignment server owned fields are not honoured.                                   |
| `Injection_EveryTextField_IsStoredOrRejectedAsDataAsync`                            | Fact | Injection every text field is stored or rejected as data.                               |
| `JsonColumns_RefuseInvalidJson_InsteadOfFailingInTheDatabaseAsync`                  | Fact | Json columns refuse invalid json instead of failing in the database.                    |
| `LengthBoundaries_AcceptTheMaximum_AndRefuseOneMoreAsync`                           | Fact | Length boundaries accept the maximum and refuse one more.                               |
| `TypeConfusionAndBoundaries_NeverProduceServerErrors_OrSideEffectsAsync`            | Fact | Type confusion and boundaries never produce server errors or side effects.              |
| `MalformedBodiesAndContentTypes_AreRejectedWithoutServerErrorsAsync`                | Fact | Malformed bodies and content types are rejected without server errors.                  |
| `RouteAndQueryParameterAbuse_NeverServerErrors_OrTouchesRowsAsync`                  | Fact | Route and query parameter abuse never server errors or touches rows.                    |
| `LockedRows_CannotBeUpdatedOrDeletedAsync`                                          | Fact | Locked rows cannot be updated or deleted.                                               |
| `ConcurrentUpdatesAndDoubleSubmit_NeverServerError_AndStayConsistentAsync`          | Fact | Concurrent updates and double submit never server error and stay consistent.            |
| `OversizedBodies_AreRefusedQuicklyAsync`                                            | Fact | Oversized bodies are refused quickly.                                                   |

### PenTestEntityAnalysisModelActivationRule

`Jube.Test.Security.Model.PenTestEntityAnalysisModelActivationRule` — 14 tests. Category: PenTest. Collection: Database.

| Test                                                                                | Kind | Verifies                                                                                |
|-------------------------------------------------------------------------------------|------|-----------------------------------------------------------------------------------------|
| `AuthenticationAndAuthorisationMatrix_RefusesEveryone_ButThePermittedUserAsync`     | Fact | Authentication and authorisation matrix refuses everyone but the permitted user.        |
| `Lifecycle_CreateReadUpdateDelete_HasSoftDeleteAndVersionSemanticsAsync`            | Fact | Lifecycle create read update delete has soft delete and version semantics.              |
| `CrossTenant_ReadUpdateDeleteReset_BehaveLikeNotFoundAsync`                         | Fact | Cross tenant read update delete reset behave like not found.                            |
| `CrossTenantParent_CreateUnderForeignParent_AndMoveToForeignParent_AreRefusedAsync` | Fact | Cross tenant parent create under foreign parent and move to foreign parent are refused. |
| `MassAssignment_ServerOwnedFields_AreNotHonouredAsync`                              | Fact | Mass assignment server owned fields are not honoured.                                   |
| `Injection_EveryTextField_IsStoredOrRejectedAsDataAsync`                            | Fact | Injection every text field is stored or rejected as data.                               |
| `JsonColumns_RefuseInvalidJson_InsteadOfFailingInTheDatabaseAsync`                  | Fact | Json columns refuse invalid json instead of failing in the database.                    |
| `LengthBoundaries_AcceptTheMaximum_AndRefuseOneMoreAsync`                           | Fact | Length boundaries accept the maximum and refuse one more.                               |
| `TypeConfusionAndBoundaries_NeverProduceServerErrors_OrSideEffectsAsync`            | Fact | Type confusion and boundaries never produce server errors or side effects.              |
| `MalformedBodiesAndContentTypes_AreRejectedWithoutServerErrorsAsync`                | Fact | Malformed bodies and content types are rejected without server errors.                  |
| `RouteAndQueryParameterAbuse_NeverServerErrors_OrTouchesRowsAsync`                  | Fact | Route and query parameter abuse never server errors or touches rows.                    |
| `LockedRows_CannotBeUpdatedOrDeletedAsync`                                          | Fact | Locked rows cannot be updated or deleted.                                               |
| `ConcurrentUpdatesAndDoubleSubmit_NeverServerError_AndStayConsistentAsync`          | Fact | Concurrent updates and double submit never server error and stay consistent.            |
| `OversizedBodies_AreRefusedQuicklyAsync`                                            | Fact | Oversized bodies are refused quickly.                                                   |

### PenTestEntityAnalysisModelActivationRuleOverride

`Jube.Test.Security.Model.PenTestEntityAnalysisModelActivationRuleOverride` — 14 tests. Category: PenTest. Collection:
Database.

| Test                                                                                | Kind | Verifies                                                                                |
|-------------------------------------------------------------------------------------|------|-----------------------------------------------------------------------------------------|
| `AuthenticationAndAuthorisationMatrix_RefusesEveryone_ButThePermittedUserAsync`     | Fact | Authentication and authorisation matrix refuses everyone but the permitted user.        |
| `Lifecycle_CreateReadUpdateDelete_HasSoftDeleteAndVersionSemanticsAsync`            | Fact | Lifecycle create read update delete has soft delete and version semantics.              |
| `CrossTenant_ReadUpdateDeleteReset_BehaveLikeNotFoundAsync`                         | Fact | Cross tenant read update delete reset behave like not found.                            |
| `CrossTenantParent_CreateUnderForeignParent_AndMoveToForeignParent_AreRefusedAsync` | Fact | Cross tenant parent create under foreign parent and move to foreign parent are refused. |
| `MassAssignment_ServerOwnedFields_AreNotHonouredAsync`                              | Fact | Mass assignment server owned fields are not honoured.                                   |
| `Injection_EveryTextField_IsStoredOrRejectedAsDataAsync`                            | Fact | Injection every text field is stored or rejected as data.                               |
| `JsonColumns_RefuseInvalidJson_InsteadOfFailingInTheDatabaseAsync`                  | Fact | Json columns refuse invalid json instead of failing in the database.                    |
| `LengthBoundaries_AcceptTheMaximum_AndRefuseOneMoreAsync`                           | Fact | Length boundaries accept the maximum and refuse one more.                               |
| `TypeConfusionAndBoundaries_NeverProduceServerErrors_OrSideEffectsAsync`            | Fact | Type confusion and boundaries never produce server errors or side effects.              |
| `MalformedBodiesAndContentTypes_AreRejectedWithoutServerErrorsAsync`                | Fact | Malformed bodies and content types are rejected without server errors.                  |
| `RouteAndQueryParameterAbuse_NeverServerErrors_OrTouchesRowsAsync`                  | Fact | Route and query parameter abuse never server errors or touches rows.                    |
| `LockedRows_CannotBeUpdatedOrDeletedAsync`                                          | Fact | Locked rows cannot be updated or deleted.                                               |
| `ConcurrentUpdatesAndDoubleSubmit_NeverServerError_AndStayConsistentAsync`          | Fact | Concurrent updates and double submit never server error and stay consistent.            |
| `OversizedBodies_AreRefusedQuicklyAsync`                                            | Fact | Oversized bodies are refused quickly.                                                   |

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

| Test                                                                                | Kind | Verifies                                                                                |
|-------------------------------------------------------------------------------------|------|-----------------------------------------------------------------------------------------|
| `AuthenticationAndAuthorisationMatrix_RefusesEveryone_ButThePermittedUserAsync`     | Fact | Authentication and authorisation matrix refuses everyone but the permitted user.        |
| `Lifecycle_CreateReadUpdateDelete_HasSoftDeleteAndVersionSemanticsAsync`            | Fact | Lifecycle create read update delete has soft delete and version semantics.              |
| `CrossTenant_ReadUpdateDeleteReset_BehaveLikeNotFoundAsync`                         | Fact | Cross tenant read update delete reset behave like not found.                            |
| `CrossTenantParent_CreateUnderForeignParent_AndMoveToForeignParent_AreRefusedAsync` | Fact | Cross tenant parent create under foreign parent and move to foreign parent are refused. |
| `MassAssignment_ServerOwnedFields_AreNotHonouredAsync`                              | Fact | Mass assignment server owned fields are not honoured.                                   |
| `Injection_EveryTextField_IsStoredOrRejectedAsDataAsync`                            | Fact | Injection every text field is stored or rejected as data.                               |
| `JsonColumns_RefuseInvalidJson_InsteadOfFailingInTheDatabaseAsync`                  | Fact | Json columns refuse invalid json instead of failing in the database.                    |
| `LengthBoundaries_AcceptTheMaximum_AndRefuseOneMoreAsync`                           | Fact | Length boundaries accept the maximum and refuse one more.                               |
| `TypeConfusionAndBoundaries_NeverProduceServerErrors_OrSideEffectsAsync`            | Fact | Type confusion and boundaries never produce server errors or side effects.              |
| `MalformedBodiesAndContentTypes_AreRejectedWithoutServerErrorsAsync`                | Fact | Malformed bodies and content types are rejected without server errors.                  |
| `RouteAndQueryParameterAbuse_NeverServerErrors_OrTouchesRowsAsync`                  | Fact | Route and query parameter abuse never server errors or touches rows.                    |
| `LockedRows_CannotBeUpdatedOrDeletedAsync`                                          | Fact | Locked rows cannot be updated or deleted.                                               |
| `ConcurrentUpdatesAndDoubleSubmit_NeverServerError_AndStayConsistentAsync`          | Fact | Concurrent updates and double submit never server error and stay consistent.            |
| `OversizedBodies_AreRefusedQuicklyAsync`                                            | Fact | Oversized bodies are refused quickly.                                                   |

### PenTestEntityAnalysisModelDictionaryKvp

`Jube.Test.Security.Model.PenTestEntityAnalysisModelDictionaryKvp` — 20 tests. Category: PenTest. Collection: Database.

| Test                                                                                | Kind | Verifies                                                                                |
|-------------------------------------------------------------------------------------|------|-----------------------------------------------------------------------------------------|
| `AuthenticationAndAuthorisationMatrix_RefusesEveryone_ButThePermittedUserAsync`     | Fact | Authentication and authorisation matrix refuses everyone but the permitted user.        |
| `Lifecycle_CreateReadUpdateDelete_HasSoftDeleteAndVersionSemanticsAsync`            | Fact | Lifecycle create read update delete has soft delete and version semantics.              |
| `CrossTenant_ReadUpdateDeleteReset_BehaveLikeNotFoundAsync`                         | Fact | Cross tenant read update delete reset behave like not found.                            |
| `CrossTenantParent_CreateUnderForeignParent_AndMoveToForeignParent_AreRefusedAsync` | Fact | Cross tenant parent create under foreign parent and move to foreign parent are refused. |
| `MassAssignment_ServerOwnedFields_AreNotHonouredAsync`                              | Fact | Mass assignment server owned fields are not honoured.                                   |
| `Injection_EveryTextField_IsStoredOrRejectedAsDataAsync`                            | Fact | Injection every text field is stored or rejected as data.                               |
| `JsonColumns_RefuseInvalidJson_InsteadOfFailingInTheDatabaseAsync`                  | Fact | Json columns refuse invalid json instead of failing in the database.                    |
| `LengthBoundaries_AcceptTheMaximum_AndRefuseOneMoreAsync`                           | Fact | Length boundaries accept the maximum and refuse one more.                               |
| `TypeConfusionAndBoundaries_NeverProduceServerErrors_OrSideEffectsAsync`            | Fact | Type confusion and boundaries never produce server errors or side effects.              |
| `MalformedBodiesAndContentTypes_AreRejectedWithoutServerErrorsAsync`                | Fact | Malformed bodies and content types are rejected without server errors.                  |
| `RouteAndQueryParameterAbuse_NeverServerErrors_OrTouchesRowsAsync`                  | Fact | Route and query parameter abuse never server errors or touches rows.                    |
| `LockedRows_CannotBeUpdatedOrDeletedAsync`                                          | Fact | Locked rows cannot be updated or deleted.                                               |
| `ConcurrentUpdatesAndDoubleSubmit_NeverServerError_AndStayConsistentAsync`          | Fact | Concurrent updates and double submit never server error and stay consistent.            |
| `OversizedBodies_AreRefusedQuicklyAsync`                                            | Fact | Oversized bodies are refused quickly.                                                   |
| `CsvUpload_AuthorisationAndCrossTenantDictionary_AreRefusedAsync`                   | Fact | Csv upload authorisation and cross tenant dictionary are refused.                       |
| `CsvUpload_ImportsCellsAsData_IncludingFormulaAndMarkupKeysAsync`                   | Fact | Csv upload imports cells as data including formula and markup keys.                     |
| `CsvUpload_MalformedRows_AreSkippedWithoutServerErrorAsync`                         | Fact | Csv upload malformed rows are skipped without server error.                             |
| `CsvUpload_HostileFileNames_AreStoredSanitisedAndNeverUsedAsPathsAsync`             | Fact | Csv upload hostile file names are stored sanitised and never used as paths.             |
| `CsvUpload_MultipartAttacks_AreRefusedWithoutServerErrorsAsync`                     | Fact | Csv upload multipart attacks are refused without server errors.                         |
| `CsvUpload_DictionaryIdAsFormField_IsAcceptedAndTenantScopedAsync`                  | Fact | Csv upload dictionary id as form field is accepted and tenant scoped.                   |

### PenTestEntityAnalysisModelGatewayRule

`Jube.Test.Security.Model.PenTestEntityAnalysisModelGatewayRule` — 14 tests. Category: PenTest. Collection: Database.

| Test                                                                                | Kind | Verifies                                                                                |
|-------------------------------------------------------------------------------------|------|-----------------------------------------------------------------------------------------|
| `AuthenticationAndAuthorisationMatrix_RefusesEveryone_ButThePermittedUserAsync`     | Fact | Authentication and authorisation matrix refuses everyone but the permitted user.        |
| `Lifecycle_CreateReadUpdateDelete_HasSoftDeleteAndVersionSemanticsAsync`            | Fact | Lifecycle create read update delete has soft delete and version semantics.              |
| `CrossTenant_ReadUpdateDeleteReset_BehaveLikeNotFoundAsync`                         | Fact | Cross tenant read update delete reset behave like not found.                            |
| `CrossTenantParent_CreateUnderForeignParent_AndMoveToForeignParent_AreRefusedAsync` | Fact | Cross tenant parent create under foreign parent and move to foreign parent are refused. |
| `MassAssignment_ServerOwnedFields_AreNotHonouredAsync`                              | Fact | Mass assignment server owned fields are not honoured.                                   |
| `Injection_EveryTextField_IsStoredOrRejectedAsDataAsync`                            | Fact | Injection every text field is stored or rejected as data.                               |
| `JsonColumns_RefuseInvalidJson_InsteadOfFailingInTheDatabaseAsync`                  | Fact | Json columns refuse invalid json instead of failing in the database.                    |
| `LengthBoundaries_AcceptTheMaximum_AndRefuseOneMoreAsync`                           | Fact | Length boundaries accept the maximum and refuse one more.                               |
| `TypeConfusionAndBoundaries_NeverProduceServerErrors_OrSideEffectsAsync`            | Fact | Type confusion and boundaries never produce server errors or side effects.              |
| `MalformedBodiesAndContentTypes_AreRejectedWithoutServerErrorsAsync`                | Fact | Malformed bodies and content types are rejected without server errors.                  |
| `RouteAndQueryParameterAbuse_NeverServerErrors_OrTouchesRowsAsync`                  | Fact | Route and query parameter abuse never server errors or touches rows.                    |
| `LockedRows_CannotBeUpdatedOrDeletedAsync`                                          | Fact | Locked rows cannot be updated or deleted.                                               |
| `ConcurrentUpdatesAndDoubleSubmit_NeverServerError_AndStayConsistentAsync`          | Fact | Concurrent updates and double submit never server error and stay consistent.            |
| `OversizedBodies_AreRefusedQuicklyAsync`                                            | Fact | Oversized bodies are refused quickly.                                                   |

### PenTestEntityAnalysisModelHttpAdaptation

`Jube.Test.Security.Model.PenTestEntityAnalysisModelHttpAdaptation` — 15 tests. Category: PenTest. Collection: Database.

| Test                                                                                | Kind | Verifies                                                                                |
|-------------------------------------------------------------------------------------|------|-----------------------------------------------------------------------------------------|
| `AuthenticationAndAuthorisationMatrix_RefusesEveryone_ButThePermittedUserAsync`     | Fact | Authentication and authorisation matrix refuses everyone but the permitted user.        |
| `Lifecycle_CreateReadUpdateDelete_HasSoftDeleteAndVersionSemanticsAsync`            | Fact | Lifecycle create read update delete has soft delete and version semantics.              |
| `CrossTenant_ReadUpdateDeleteReset_BehaveLikeNotFoundAsync`                         | Fact | Cross tenant read update delete reset behave like not found.                            |
| `CrossTenantParent_CreateUnderForeignParent_AndMoveToForeignParent_AreRefusedAsync` | Fact | Cross tenant parent create under foreign parent and move to foreign parent are refused. |
| `MassAssignment_ServerOwnedFields_AreNotHonouredAsync`                              | Fact | Mass assignment server owned fields are not honoured.                                   |
| `Injection_EveryTextField_IsStoredOrRejectedAsDataAsync`                            | Fact | Injection every text field is stored or rejected as data.                               |
| `JsonColumns_RefuseInvalidJson_InsteadOfFailingInTheDatabaseAsync`                  | Fact | Json columns refuse invalid json instead of failing in the database.                    |
| `LengthBoundaries_AcceptTheMaximum_AndRefuseOneMoreAsync`                           | Fact | Length boundaries accept the maximum and refuse one more.                               |
| `TypeConfusionAndBoundaries_NeverProduceServerErrors_OrSideEffectsAsync`            | Fact | Type confusion and boundaries never produce server errors or side effects.              |
| `MalformedBodiesAndContentTypes_AreRejectedWithoutServerErrorsAsync`                | Fact | Malformed bodies and content types are rejected without server errors.                  |
| `RouteAndQueryParameterAbuse_NeverServerErrors_OrTouchesRowsAsync`                  | Fact | Route and query parameter abuse never server errors or touches rows.                    |
| `LockedRows_CannotBeUpdatedOrDeletedAsync`                                          | Fact | Locked rows cannot be updated or deleted.                                               |
| `ConcurrentUpdatesAndDoubleSubmit_NeverServerError_AndStayConsistentAsync`          | Fact | Concurrent updates and double submit never server error and stay consistent.            |
| `OversizedBodies_AreRefusedQuicklyAsync`                                            | Fact | Oversized bodies are refused quickly.                                                   |
| `HttpEndpoint_OnlyPlainPathsOnTheConfiguredHost_AreAcceptedAsync`                   | Fact | Http endpoint only plain paths on the configured host are accepted.                     |

### PenTestExhaustiveSearchInstance

`Jube.Test.Security.Model.PenTestExhaustiveSearchInstance` — 18 tests. Category: PenTest. Collection: Database.

| Test                                                                                | Kind | Verifies                                                                                |
|-------------------------------------------------------------------------------------|------|-----------------------------------------------------------------------------------------|
| `AuthenticationAndAuthorisationMatrix_RefusesEveryone_ButThePermittedUserAsync`     | Fact | Authentication and authorisation matrix refuses everyone but the permitted user.        |
| `Lifecycle_CreateReadUpdateDelete_HasSoftDeleteAndVersionSemanticsAsync`            | Fact | Lifecycle create read update delete has soft delete and version semantics.              |
| `CrossTenant_ReadUpdateDeleteReset_BehaveLikeNotFoundAsync`                         | Fact | Cross tenant read update delete reset behave like not found.                            |
| `CrossTenantParent_CreateUnderForeignParent_AndMoveToForeignParent_AreRefusedAsync` | Fact | Cross tenant parent create under foreign parent and move to foreign parent are refused. |
| `MassAssignment_ServerOwnedFields_AreNotHonouredAsync`                              | Fact | Mass assignment server owned fields are not honoured.                                   |
| `Injection_EveryTextField_IsStoredOrRejectedAsDataAsync`                            | Fact | Injection every text field is stored or rejected as data.                               |
| `JsonColumns_RefuseInvalidJson_InsteadOfFailingInTheDatabaseAsync`                  | Fact | Json columns refuse invalid json instead of failing in the database.                    |
| `LengthBoundaries_AcceptTheMaximum_AndRefuseOneMoreAsync`                           | Fact | Length boundaries accept the maximum and refuse one more.                               |
| `TypeConfusionAndBoundaries_NeverProduceServerErrors_OrSideEffectsAsync`            | Fact | Type confusion and boundaries never produce server errors or side effects.              |
| `MalformedBodiesAndContentTypes_AreRejectedWithoutServerErrorsAsync`                | Fact | Malformed bodies and content types are rejected without server errors.                  |
| `RouteAndQueryParameterAbuse_NeverServerErrors_OrTouchesRowsAsync`                  | Fact | Route and query parameter abuse never server errors or touches rows.                    |
| `LockedRows_CannotBeUpdatedOrDeletedAsync`                                          | Fact | Locked rows cannot be updated or deleted.                                               |
| `ConcurrentUpdatesAndDoubleSubmit_NeverServerError_AndStayConsistentAsync`          | Fact | Concurrent updates and double submit never server error and stay consistent.            |
| `OversizedBodies_AreRefusedQuicklyAsync`                                            | Fact | Oversized bodies are refused quickly.                                                   |
| `Stop_ForeignGuid_LooksLikeMissing_AndDoesNotTouchTheRowAsync`                      | Fact | Stop foreign guid looks like missing and does not touch the row.                        |
| `SetActive_ForeignId_LooksLikeMissing_AndDoesNotTouchTheRowAsync`                   | Fact | Set active foreign id looks like missing and does not touch the row.                    |
| `FilterDefinition_MustBeAPlainBooleanExpression_AndValidJsonAsync`                  | Fact | Filter definition must be a plain boolean expression and valid json.                    |
| `AnomalyDetectorFields_AreValidated_AndTheReportIsServerWrittenAsync`               | Fact | Anomaly detector fields are validated and the report is server written.                 |

### PenTestModelInlineFunction

`Jube.Test.Security.Model.PenTestModelInlineFunction` — 11 tests. Category: PenTest. Collection: Database.

| Test                                                       | Kind | Verifies                                                    |
|------------------------------------------------------------|------|-------------------------------------------------------------|
| `Matrix_EveryRouteEveryRoleAsync`                          | Fact | Matrix every route every role.                              |
| `Bola_ForeignTenantIdsBehaveLikeAbsentIdsAsync`            | Fact | Bola foreign tenant ids behave like absent ids.             |
| `Bola_ForeignParentReferencesAreRefusedAsync`              | Fact | Bola foreign parent references are refused.                 |
| `MassAssignment_ServerOwnedFieldsAreNotHonouredAsync`      | Fact | Mass assignment server owned fields are not honoured.       |
| `Injection_StringFieldsStayDataAndRoundTripAsync`          | Fact | Injection string fields stay data and round trip.           |
| `Injection_RouteAndQueryParametersAsync`                   | Fact | Injection route and query parameters.                       |
| `TypeConfusion_EveryFieldEveryRawValueAsync`               | Fact | Type confusion every field every raw value.                 |
| `RequestStructure_MalformedBodiesAndContentTypesAsync`     | Fact | Request structure malformed bodies and content types.       |
| `BusinessFlow_DeleteReplayAndSoftDeleteAreConsistentAsync` | Fact | Business flow delete replay and soft delete are consistent. |
| `Limits_MaxLengthOversizeAndHostileExpressionsAsync`       | Fact | Limits max length oversize and hostile expressions.         |
| `Consumption_ListsAndParametersAreBoundedAsync`            | Fact | Consumption lists and parameters are bounded.               |

### PenTestModelInlineScript

`Jube.Test.Security.Model.PenTestModelInlineScript` — 13 tests. Category: PenTest. Collection: Database.

| Test                                                       | Kind | Verifies                                                    |
|------------------------------------------------------------|------|-------------------------------------------------------------|
| `Matrix_EveryRouteEveryRoleAsync`                          | Fact | Matrix every route every role.                              |
| `Bola_ForeignTenantIdsBehaveLikeAbsentIdsAsync`            | Fact | Bola foreign tenant ids behave like absent ids.             |
| `Bola_ForeignParentReferencesAreRefusedAsync`              | Fact | Bola foreign parent references are refused.                 |
| `MassAssignment_ServerOwnedFieldsAreNotHonouredAsync`      | Fact | Mass assignment server owned fields are not honoured.       |
| `Injection_StringFieldsStayDataAndRoundTripAsync`          | Fact | Injection string fields stay data and round trip.           |
| `Injection_RouteAndQueryParametersAsync`                   | Fact | Injection route and query parameters.                       |
| `TypeConfusion_EveryFieldEveryRawValueAsync`               | Fact | Type confusion every field every raw value.                 |
| `RequestStructure_MalformedBodiesAndContentTypesAsync`     | Fact | Request structure malformed bodies and content types.       |
| `BusinessFlow_DeleteReplayAndSoftDeleteAreConsistentAsync` | Fact | Business flow delete replay and soft delete are consistent. |
| `Limits_MaxLengthOversizeAndHostileExpressionsAsync`       | Fact | Limits max length oversize and hostile expressions.         |
| `Consumption_ListsAndParametersAreBoundedAsync`            | Fact | Consumption lists and parameters are bounded.               |
| `InlineScriptReference_MustExistAndBeAnIntegerAsync`       | Fact | Inline script reference must exist and be an integer.       |
| `CreatedRowExposesNoScriptSourceAsync`                     | Fact | Created row exposes no script source.                       |

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

| Test                                                       | Kind | Verifies                                                    |
|------------------------------------------------------------|------|-------------------------------------------------------------|
| `Matrix_EveryRouteEveryRoleAsync`                          | Fact | Matrix every route every role.                              |
| `Bola_ForeignTenantIdsBehaveLikeAbsentIdsAsync`            | Fact | Bola foreign tenant ids behave like absent ids.             |
| `Bola_ForeignParentReferencesAreRefusedAsync`              | Fact | Bola foreign parent references are refused.                 |
| `MassAssignment_ServerOwnedFieldsAreNotHonouredAsync`      | Fact | Mass assignment server owned fields are not honoured.       |
| `Injection_StringFieldsStayDataAndRoundTripAsync`          | Fact | Injection string fields stay data and round trip.           |
| `Injection_RouteAndQueryParametersAsync`                   | Fact | Injection route and query parameters.                       |
| `TypeConfusion_EveryFieldEveryRawValueAsync`               | Fact | Type confusion every field every raw value.                 |
| `RequestStructure_MalformedBodiesAndContentTypesAsync`     | Fact | Request structure malformed bodies and content types.       |
| `BusinessFlow_DeleteReplayAndSoftDeleteAreConsistentAsync` | Fact | Business flow delete replay and soft delete are consistent. |
| `Limits_MaxLengthOversizeAndHostileExpressionsAsync`       | Fact | Limits max length oversize and hostile expressions.         |
| `Consumption_ListsAndParametersAreBoundedAsync`            | Fact | Consumption lists and parameters are bounded.               |

### PenTestModelListValue

`Jube.Test.Security.Model.PenTestModelListValue` — 19 tests. Category: PenTest. Collection: Database.

| Test                                                        | Kind | Verifies                                                         |
|-------------------------------------------------------------|------|------------------------------------------------------------------|
| `Matrix_EveryRouteEveryRoleAsync`                           | Fact | Matrix every route every role.                                   |
| `Bola_ForeignTenantIdsBehaveLikeAbsentIdsAsync`             | Fact | Bola foreign tenant ids behave like absent ids.                  |
| `Bola_ForeignParentReferencesAreRefusedAsync`               | Fact | Bola foreign parent references are refused.                      |
| `MassAssignment_ServerOwnedFieldsAreNotHonouredAsync`       | Fact | Mass assignment server owned fields are not honoured.            |
| `Injection_StringFieldsStayDataAndRoundTripAsync`           | Fact | Injection string fields stay data and round trip.                |
| `Injection_RouteAndQueryParametersAsync`                    | Fact | Injection route and query parameters.                            |
| `TypeConfusion_EveryFieldEveryRawValueAsync`                | Fact | Type confusion every field every raw value.                      |
| `RequestStructure_MalformedBodiesAndContentTypesAsync`      | Fact | Request structure malformed bodies and content types.            |
| `BusinessFlow_DeleteReplayAndSoftDeleteAreConsistentAsync`  | Fact | Business flow delete replay and soft delete are consistent.      |
| `Limits_MaxLengthOversizeAndHostileExpressionsAsync`        | Fact | Limits max length oversize and hostile expressions.              |
| `Consumption_ListsAndParametersAreBoundedAsync`             | Fact | Consumption lists and parameters are bounded.                    |
| `ByParentRead_WithoutAnId_IsAnEmptyListNotABadRequestAsync` | Fact | By parent read without an id is an empty list not a bad request. |
| `Values_ExpiryDatesDuplicatesAndUnicodeAsync`               | Fact | Values expiry dates duplicates and unicode.                      |
| `Values_DeleteByIdIsTenantScopedAndBodyIdIsIgnoredAsync`    | Fact | Values delete by id is tenant scoped and body id is ignored.     |
| `Csv_AuthenticationAuthorisationAndTenantScopeAsync`        | Fact | Csv authentication authorisation and tenant scope.               |
| `Csv_FileNamesTraversalAndContentTypesAsync`                | Fact | Csv file names traversal and content types.                      |
| `Csv_ContentAttacksAreRowErrorsOrInertDataAsync`            | Fact | Csv content attacks are row errors or inert data.                |
| `Csv_HugeFilesManyFilesAndAtomicRollbackAsync`              | Fact | Csv huge files many files and atomic rollback.                   |
| `Csv_MalformedMultipartAndCsrfAsync`                        | Fact | Csv malformed multipart and csrf.                                |

### PenTestModelOverride

`Jube.Test.Security.Model.PenTestModelOverride` — 12 tests. Category: PenTest. Collection: Database.

| Test                                                       | Kind | Verifies                                                    |
|------------------------------------------------------------|------|-------------------------------------------------------------|
| `Matrix_EveryRouteEveryRoleAsync`                          | Fact | Matrix every route every role.                              |
| `Bola_ForeignTenantIdsBehaveLikeAbsentIdsAsync`            | Fact | Bola foreign tenant ids behave like absent ids.             |
| `Bola_ForeignParentReferencesAreRefusedAsync`              | Fact | Bola foreign parent references are refused.                 |
| `MassAssignment_ServerOwnedFieldsAreNotHonouredAsync`      | Fact | Mass assignment server owned fields are not honoured.       |
| `Injection_StringFieldsStayDataAndRoundTripAsync`          | Fact | Injection string fields stay data and round trip.           |
| `Injection_RouteAndQueryParametersAsync`                   | Fact | Injection route and query parameters.                       |
| `TypeConfusion_EveryFieldEveryRawValueAsync`               | Fact | Type confusion every field every raw value.                 |
| `RequestStructure_MalformedBodiesAndContentTypesAsync`     | Fact | Request structure malformed bodies and content types.       |
| `BusinessFlow_DeleteReplayAndSoftDeleteAreConsistentAsync` | Fact | Business flow delete replay and soft delete are consistent. |
| `Limits_MaxLengthOversizeAndHostileExpressionsAsync`       | Fact | Limits max length oversize and hostile expressions.         |
| `Consumption_ListsAndParametersAreBoundedAsync`            | Fact | Consumption lists and parameters are bounded.               |
| `ExpiryDate_PastFarFutureAndCrossTenantAreHandledAsync`    | Fact | Expiry date past far future and cross tenant are handled.   |

### PenTestModelReprocessingRule

`Jube.Test.Security.Model.PenTestModelReprocessingRule` — 12 tests. Category: PenTest. Collection: Database.

| Test                                                            | Kind | Verifies                                                         |
|-----------------------------------------------------------------|------|------------------------------------------------------------------|
| `Matrix_EveryRouteEveryRoleAsync`                               | Fact | Matrix every route every role.                                   |
| `Bola_ForeignTenantIdsBehaveLikeAbsentIdsAsync`                 | Fact | Bola foreign tenant ids behave like absent ids.                  |
| `Bola_ForeignParentReferencesAreRefusedAsync`                   | Fact | Bola foreign parent references are refused.                      |
| `MassAssignment_ServerOwnedFieldsAreNotHonouredAsync`           | Fact | Mass assignment server owned fields are not honoured.            |
| `Injection_StringFieldsStayDataAndRoundTripAsync`               | Fact | Injection string fields stay data and round trip.                |
| `Injection_RouteAndQueryParametersAsync`                        | Fact | Injection route and query parameters.                            |
| `TypeConfusion_EveryFieldEveryRawValueAsync`                    | Fact | Type confusion every field every raw value.                      |
| `RequestStructure_MalformedBodiesAndContentTypesAsync`          | Fact | Request structure malformed bodies and content types.            |
| `BusinessFlow_DeleteReplayAndSoftDeleteAreConsistentAsync`      | Fact | Business flow delete replay and soft delete are consistent.      |
| `Limits_MaxLengthOversizeAndHostileExpressionsAsync`            | Fact | Limits max length oversize and hostile expressions.              |
| `Consumption_ListsAndParametersAreBoundedAsync`                 | Fact | Consumption lists and parameters are bounded.                    |
| `Window_ReprocessingValueCannotOverflowTheEngineDateMathsAsync` | Fact | Window reprocessing value cannot overflow the engine date maths. |

### PenTestModelReprocessingRuleInstance

`Jube.Test.Security.Model.PenTestModelReprocessingRuleInstance` — 14 tests. Category: PenTest. Collection: Database.

| Test                                                         | Kind | Verifies                                                        |
|--------------------------------------------------------------|------|-----------------------------------------------------------------|
| `Matrix_EveryRouteEveryRoleAsync`                            | Fact | Matrix every route every role.                                  |
| `Bola_ForeignTenantIdsBehaveLikeAbsentIdsAsync`              | Fact | Bola foreign tenant ids behave like absent ids.                 |
| `Bola_ForeignParentReferencesAreRefusedAsync`                | Fact | Bola foreign parent references are refused.                     |
| `MassAssignment_ServerOwnedFieldsAreNotHonouredAsync`        | Fact | Mass assignment server owned fields are not honoured.           |
| `Injection_StringFieldsStayDataAndRoundTripAsync`            | Fact | Injection string fields stay data and round trip.               |
| `Injection_RouteAndQueryParametersAsync`                     | Fact | Injection route and query parameters.                           |
| `TypeConfusion_EveryFieldEveryRawValueAsync`                 | Fact | Type confusion every field every raw value.                     |
| `RequestStructure_MalformedBodiesAndContentTypesAsync`       | Fact | Request structure malformed bodies and content types.           |
| `BusinessFlow_DeleteReplayAndSoftDeleteAreConsistentAsync`   | Fact | Business flow delete replay and soft delete are consistent.     |
| `Limits_MaxLengthOversizeAndHostileExpressionsAsync`         | Fact | Limits max length oversize and hostile expressions.             |
| `Consumption_ListsAndParametersAreBoundedAsync`              | Fact | Consumption lists and parameters are bounded.                   |
| `Trigger_OnlyForOwnTenantsRuleAndServerAssignsRunStateAsync` | Fact | Trigger only for own tenants rule and server assigns run state. |
| `RunState_EnumsCountsAndDatesAreBoundedAsync`                | Fact | Run state enums counts and dates are bounded.                   |
| `Update_CannotHijackOrResurrectAnInstanceAsync`              | Fact | Update cannot hijack or resurrect an instance.                  |

### PenTestModelRequestXPath

`Jube.Test.Security.Model.PenTestModelRequestXPath` — 14 tests. Category: PenTest. Collection: Database.

| Test                                                         | Kind | Verifies                                                      |
|--------------------------------------------------------------|------|---------------------------------------------------------------|
| `Matrix_EveryRouteEveryRoleAsync`                            | Fact | Matrix every route every role.                                |
| `Bola_ForeignTenantIdsBehaveLikeAbsentIdsAsync`              | Fact | Bola foreign tenant ids behave like absent ids.               |
| `Bola_ForeignParentReferencesAreRefusedAsync`                | Fact | Bola foreign parent references are refused.                   |
| `MassAssignment_ServerOwnedFieldsAreNotHonouredAsync`        | Fact | Mass assignment server owned fields are not honoured.         |
| `Injection_StringFieldsStayDataAndRoundTripAsync`            | Fact | Injection string fields stay data and round trip.             |
| `Injection_RouteAndQueryParametersAsync`                     | Fact | Injection route and query parameters.                         |
| `TypeConfusion_EveryFieldEveryRawValueAsync`                 | Fact | Type confusion every field every raw value.                   |
| `RequestStructure_MalformedBodiesAndContentTypesAsync`       | Fact | Request structure malformed bodies and content types.         |
| `BusinessFlow_DeleteReplayAndSoftDeleteAreConsistentAsync`   | Fact | Business flow delete replay and soft delete are consistent.   |
| `Limits_MaxLengthOversizeAndHostileExpressionsAsync`         | Fact | Limits max length oversize and hostile expressions.           |
| `Consumption_ListsAndParametersAreBoundedAsync`              | Fact | Consumption lists and parameters are bounded.                 |
| `Window_SearchKeyWindowsAndLimitsAreBoundedAsync`            | Fact | Window search key windows and limits are bounded.             |
| `ExtraRoutes_ForeignParentsAndHostileArgumentsAreInertAsync` | Fact | Extra routes foreign parents and hostile arguments are inert. |
| `XPathExpressions_AreStoredAsInertDataAsync`                 | Fact | X path expressions are stored as inert data.                  |

### PenTestModelSanction

`Jube.Test.Security.Model.PenTestModelSanction` — 12 tests. Category: PenTest. Collection: Database.

| Test                                                       | Kind | Verifies                                                    |
|------------------------------------------------------------|------|-------------------------------------------------------------|
| `Matrix_EveryRouteEveryRoleAsync`                          | Fact | Matrix every route every role.                              |
| `Bola_ForeignTenantIdsBehaveLikeAbsentIdsAsync`            | Fact | Bola foreign tenant ids behave like absent ids.             |
| `Bola_ForeignParentReferencesAreRefusedAsync`              | Fact | Bola foreign parent references are refused.                 |
| `MassAssignment_ServerOwnedFieldsAreNotHonouredAsync`      | Fact | Mass assignment server owned fields are not honoured.       |
| `Injection_StringFieldsStayDataAndRoundTripAsync`          | Fact | Injection string fields stay data and round trip.           |
| `Injection_RouteAndQueryParametersAsync`                   | Fact | Injection route and query parameters.                       |
| `TypeConfusion_EveryFieldEveryRawValueAsync`               | Fact | Type confusion every field every raw value.                 |
| `RequestStructure_MalformedBodiesAndContentTypesAsync`     | Fact | Request structure malformed bodies and content types.       |
| `BusinessFlow_DeleteReplayAndSoftDeleteAreConsistentAsync` | Fact | Business flow delete replay and soft delete are consistent. |
| `Limits_MaxLengthOversizeAndHostileExpressionsAsync`       | Fact | Limits max length oversize and hostile expressions.         |
| `Consumption_ListsAndParametersAreBoundedAsync`            | Fact | Consumption lists and parameters are bounded.               |
| `Window_CacheValueCannotOverflowTheEngineDateMathsAsync`   | Fact | Window cache value cannot overflow the engine date maths.   |

### PenTestModelTag

`Jube.Test.Security.Model.PenTestModelTag` — 11 tests. Category: PenTest. Collection: Database.

| Test                                                       | Kind | Verifies                                                    |
|------------------------------------------------------------|------|-------------------------------------------------------------|
| `Matrix_EveryRouteEveryRoleAsync`                          | Fact | Matrix every route every role.                              |
| `Bola_ForeignTenantIdsBehaveLikeAbsentIdsAsync`            | Fact | Bola foreign tenant ids behave like absent ids.             |
| `Bola_ForeignParentReferencesAreRefusedAsync`              | Fact | Bola foreign parent references are refused.                 |
| `MassAssignment_ServerOwnedFieldsAreNotHonouredAsync`      | Fact | Mass assignment server owned fields are not honoured.       |
| `Injection_StringFieldsStayDataAndRoundTripAsync`          | Fact | Injection string fields stay data and round trip.           |
| `Injection_RouteAndQueryParametersAsync`                   | Fact | Injection route and query parameters.                       |
| `TypeConfusion_EveryFieldEveryRawValueAsync`               | Fact | Type confusion every field every raw value.                 |
| `RequestStructure_MalformedBodiesAndContentTypesAsync`     | Fact | Request structure malformed bodies and content types.       |
| `BusinessFlow_DeleteReplayAndSoftDeleteAreConsistentAsync` | Fact | Business flow delete replay and soft delete are consistent. |
| `Limits_MaxLengthOversizeAndHostileExpressionsAsync`       | Fact | Limits max length oversize and hostile expressions.         |
| `Consumption_ListsAndParametersAreBoundedAsync`            | Fact | Consumption lists and parameters are bounded.               |

### PenTestModelTtlCounter

`Jube.Test.Security.Model.PenTestModelTtlCounter` — 13 tests. Category: PenTest. Collection: Database.

| Test                                                          | Kind | Verifies                                                        |
|---------------------------------------------------------------|------|-----------------------------------------------------------------|
| `Matrix_EveryRouteEveryRoleAsync`                             | Fact | Matrix every route every role.                                  |
| `Bola_ForeignTenantIdsBehaveLikeAbsentIdsAsync`               | Fact | Bola foreign tenant ids behave like absent ids.                 |
| `Bola_ForeignParentReferencesAreRefusedAsync`                 | Fact | Bola foreign parent references are refused.                     |
| `MassAssignment_ServerOwnedFieldsAreNotHonouredAsync`         | Fact | Mass assignment server owned fields are not honoured.           |
| `Injection_StringFieldsStayDataAndRoundTripAsync`             | Fact | Injection string fields stay data and round trip.               |
| `Injection_RouteAndQueryParametersAsync`                      | Fact | Injection route and query parameters.                           |
| `TypeConfusion_EveryFieldEveryRawValueAsync`                  | Fact | Type confusion every field every raw value.                     |
| `RequestStructure_MalformedBodiesAndContentTypesAsync`        | Fact | Request structure malformed bodies and content types.           |
| `BusinessFlow_DeleteReplayAndSoftDeleteAreConsistentAsync`    | Fact | Business flow delete replay and soft delete are consistent.     |
| `Limits_MaxLengthOversizeAndHostileExpressionsAsync`          | Fact | Limits max length oversize and hostile expressions.             |
| `Consumption_ListsAndParametersAreBoundedAsync`               | Fact | Consumption lists and parameters are bounded.                   |
| `Window_TtlCounterValueCannotOverflowTheEngineDateMathsAsync` | Fact | Window ttl counter value cannot overflow the engine date maths. |
| `ByGuid_ForeignAndMalformedGuidsBehaveLikeAbsentAsync`        | Fact | By guid foreign and malformed guids behave like absent.         |

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

### PenTestRedisMonitoring

`Jube.Test.Security.Monitoring.PenTestRedisMonitoring` — 5 tests. Category: PenTest. Collection: Database.

| Test                                                           | Kind   | Verifies                                                       |
|----------------------------------------------------------------|--------|----------------------------------------------------------------|
| `AuthorizationMatrixForAreaAsync`                              | Theory | Authorization matrix for area.                                 |
| `ParameterAbuseForAreaAsync`                                   | Theory | Parameter abuse for area.                                      |
| `ConsumptionAndVerbsForAreaAsync`                              | Theory | Consumption and verbs for area.                                |
| `RedisSlowOperation_AuthenticationArgumentsAreRedactedAsync`   | Fact   | Redis slow operation authentication arguments are redacted.    |
| `RedisConnectionAndSentinelEvents_CredentialsAreRedactedAsync` | Fact   | Redis connection and sentinel events credentials are redacted. |

### PenTestSensitiveTextRedactor

`Jube.Test.Security.Monitoring.PenTestSensitiveTextRedactor` — 6 tests. Category: PenTest.

| Test                                          | Kind   | Verifies                                          |
|-----------------------------------------------|--------|---------------------------------------------------|
| `Redact_MasksCredentialShapes`                | Theory | Redact masks credential shapes.                   |
| `RedactRedisCommand_MasksCredentialArguments` | Theory | Redact redis command masks credential arguments.  |
| `RedactRedisKeyName_AuthKeyIsTheCredential`   | Theory | Redact redis key name auth key is the credential. |
| `Redact_LeavesOrdinaryTextUntouched`          | Theory | Redact leaves ordinary text untouched.            |
| `Redact_NullIsNull`                           | Fact   | Redact null is null.                              |
| `Redact_HostileInputIsFastAndNeverThrows`     | Fact   | Redact hostile input is fast and never throws.    |

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

| Test                                                                 | Kind | Verifies                                                                 |
|----------------------------------------------------------------------|------|--------------------------------------------------------------------------|
| `AuthenticationAndAuthorisationMatrixAsync`                          | Fact | Authentication and authorisation matrix.                                 |
| `ExactMatchOnlyAndTenantIsolationForIdenticalKeyValueAsync`          | Fact | Exact match only and tenant isolation for identical key value.           |
| `CaseVisibilityRequiresWorkflowRoleAndStatusRoleAndLiveParentsAsync` | Fact | Case visibility requires workflow role and status role and live parents. |
| `InjectionInKeyAndValueNeverMatchesLeaksOrDelaysAsync`               | Fact | Injection in key and value never matches leaks or delays.                |
| `ParameterTypeConfusionAndBoundariesAsync`                           | Fact | Parameter type confusion and boundaries.                                 |
| `ResponseExposesOnlyTheDocumentedPropertiesAsync`                    | Fact | Response exposes only the documented properties.                         |
| `OtherVerbsNeverSucceedAndDoNotChangeTheRowSetAsync`                 | Fact | Other verbs never succeed and do not change the row set.                 |
| `ReplayAndConcurrentRequestsAreConsistentAsync`                      | Fact | Replay and concurrent requests are consistent.                           |

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

| Test                                                                 | Kind | Verifies                                                                 |
|----------------------------------------------------------------------|------|--------------------------------------------------------------------------|
| `AuthenticationAndAuthorisationMatrixAsync`                          | Fact | Authentication and authorisation matrix.                                 |
| `ExactMatchOnlyAndTenantIsolationForIdenticalKeyValueAsync`          | Fact | Exact match only and tenant isolation for identical key value.           |
| `CaseVisibilityRequiresWorkflowRoleAndStatusRoleAndLiveParentsAsync` | Fact | Case visibility requires workflow role and status role and live parents. |
| `InjectionInKeyAndValueNeverMatchesLeaksOrDelaysAsync`               | Fact | Injection in key and value never matches leaks or delays.                |
| `ParameterTypeConfusionAndBoundariesAsync`                           | Fact | Parameter type confusion and boundaries.                                 |
| `ResponseExposesOnlyTheDocumentedPropertiesAsync`                    | Fact | Response exposes only the documented properties.                         |
| `OtherVerbsNeverSucceedAndDoNotChangeTheRowSetAsync`                 | Fact | Other verbs never succeed and do not change the row set.                 |
| `ReplayAndConcurrentRequestsAreConsistentAsync`                      | Fact | Replay and concurrent requests are consistent.                           |

### PenTestCaseNoteByCaseKeyValue

`Jube.Test.Security.QueryCase.PenTestCaseNoteByCaseKeyValue` — 8 tests. Category: PenTest. Collection: Database.

| Test                                                                 | Kind | Verifies                                                                 |
|----------------------------------------------------------------------|------|--------------------------------------------------------------------------|
| `AuthenticationAndAuthorisationMatrixAsync`                          | Fact | Authentication and authorisation matrix.                                 |
| `ExactMatchOnlyAndTenantIsolationForIdenticalKeyValueAsync`          | Fact | Exact match only and tenant isolation for identical key value.           |
| `CaseVisibilityRequiresWorkflowRoleAndStatusRoleAndLiveParentsAsync` | Fact | Case visibility requires workflow role and status role and live parents. |
| `InjectionInKeyAndValueNeverMatchesLeaksOrDelaysAsync`               | Fact | Injection in key and value never matches leaks or delays.                |
| `ParameterTypeConfusionAndBoundariesAsync`                           | Fact | Parameter type confusion and boundaries.                                 |
| `ResponseExposesOnlyTheDocumentedPropertiesAsync`                    | Fact | Response exposes only the documented properties.                         |
| `OtherVerbsNeverSucceedAndDoNotChangeTheRowSetAsync`                 | Fact | Other verbs never succeed and do not change the row set.                 |
| `ReplayAndConcurrentRequestsAreConsistentAsync`                      | Fact | Replay and concurrent requests are consistent.                           |

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
Database.

| Test                                                                 | Kind | Verifies                                                                 |
|----------------------------------------------------------------------|------|--------------------------------------------------------------------------|
| `AuthenticationAndAuthorisationMatrixAsync`                          | Fact | Authentication and authorisation matrix.                                 |
| `ExactMatchOnlyAndTenantIsolationForIdenticalKeyValueAsync`          | Fact | Exact match only and tenant isolation for identical key value.           |
| `CaseVisibilityRequiresWorkflowRoleAndStatusRoleAndLiveParentsAsync` | Fact | Case visibility requires workflow role and status role and live parents. |
| `InjectionInKeyAndValueNeverMatchesLeaksOrDelaysAsync`               | Fact | Injection in key and value never matches leaks or delays.                |
| `ParameterTypeConfusionAndBoundariesAsync`                           | Fact | Parameter type confusion and boundaries.                                 |
| `ResponseExposesOnlyTheDocumentedPropertiesAsync`                    | Fact | Response exposes only the documented properties.                         |
| `OtherVerbsNeverSucceedAndDoNotChangeTheRowSetAsync`                 | Fact | Other verbs never succeed and do not change the row set.                 |
| `ReplayAndConcurrentRequestsAreConsistentAsync`                      | Fact | Replay and concurrent requests are consistent.                           |

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
Collection: Database.

| Test                                                       | Kind | Verifies                                                     |
|------------------------------------------------------------|------|--------------------------------------------------------------|
| `AuthenticationAndAuthorisationMatrixAsync`                | Fact | Authentication and authorisation matrix.                     |
| `TenantIsolationForIdenticalKeysAndForeignModelGuidsAsync` | Fact | Tenant isolation for identical keys and foreign model guids. |
| `OverrideStateRulesAreExactAndLiveOnlyAsync`               | Fact | Override state rules are exact and live only.                |
| `InjectionInKeyAndValueNeverMatchesLeaksOrDelaysAsync`     | Fact | Injection in key and value never matches leaks or delays.    |
| `ParameterBoundariesAndTypeConfusionAsync`                 | Fact | Parameter boundaries and type confusion.                     |
| `ResponseShapeVerbsAndReplayAsync`                         | Fact | Response shape verbs and replay.                             |

### PenTestEntityAnalysisModelOverrideQuery

`Jube.Test.Security.QueryCase.PenTestEntityAnalysisModelOverrideQuery` — 6 tests. Category: PenTest. Collection:
Database.

| Test                                                       | Kind | Verifies                                                     |
|------------------------------------------------------------|------|--------------------------------------------------------------|
| `AuthenticationAndAuthorisationMatrixAsync`                | Fact | Authentication and authorisation matrix.                     |
| `TenantIsolationForIdenticalKeysAndForeignModelGuidsAsync` | Fact | Tenant isolation for identical keys and foreign model guids. |
| `OverrideStateRulesAreExactAndLiveOnlyAsync`               | Fact | Override state rules are exact and live only.                |
| `InjectionInKeyAndValueNeverMatchesLeaksOrDelaysAsync`     | Fact | Injection in key and value never matches leaks or delays.    |
| `ParameterBoundariesAndTypeConfusionAsync`                 | Fact | Parameter boundaries and type confusion.                     |
| `ResponseShapeVerbsAndReplayAsync`                         | Fact | Response shape verbs and replay.                             |

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

| Test                                                                       | Kind | Verifies                                                                       |
|----------------------------------------------------------------------------|------|--------------------------------------------------------------------------------|
| `AuthorisedUser_ReadsOwnDataAsSafeSvgAsync`                                | Fact | Authorised user reads own data as safe svg.                                    |
| `AuthenticationAndPermissionMatrix_RefusesEveryoneButAuthorisedUsersAsync` | Fact | Authentication and permission matrix refuses everyone but authorised users.    |
| `CrossTenant_ForeignAndUnknownIdsAnswerIndistinguishablyAsync`             | Fact | Cross tenant foreign and unknown ids answer indistinguishably.                 |
| `IdEnumeration_NeighbouringIdsAreOwnDataOrTheEmptyStateNeverForeignAsync`  | Fact | Id enumeration neighbouring ids are own data or the empty state never foreign. |
| `EmptyStates_UnknownEmptyAndSoftDeletedAreByteForByteEqualAsync`           | Fact | Empty states unknown empty and soft deleted are byte for byte equal.           |
| `NumericEdgeCases_NaNInfinityAndOverflowNeverBreakTheSvgAsync`             | Fact | Numeric edge cases na n infinity and overflow never break the svg.             |
| `ConcurrentReads_AreDeterministicAndBoundedAsync`                          | Fact | Concurrent reads are deterministic and bounded.                                |
| `IdBoundariesAndTypeConfusion_AreRejectedOrEmptyNeverServerErrorsAsync`    | Fact | Id boundaries and type confusion are rejected or empty never server errors.    |
| `HostileQueryStringAndHeaders_NeverChangeTheChartAsync`                    | Fact | Hostile query string and headers never change the chart.                       |
| `VerbTampering_OnlyGetIsServedAsync`                                       | Fact | Verb tampering only get is served.                                             |
| `HostileVariableNames_AreEscapedAndNeverBecomeMarkupAsync`                 | Fact | Hostile variable names are escaped and never become markup.                    |

### PenTestExhaustiveChartErrorHistogram

`Jube.Test.Security.QueryExhaustive.PenTestExhaustiveChartErrorHistogram` — 11 tests. Category: PenTest.

| Test                                                                       | Kind | Verifies                                                                       |
|----------------------------------------------------------------------------|------|--------------------------------------------------------------------------------|
| `AuthorisedUser_ReadsOwnDataAsSafeSvgAsync`                                | Fact | Authorised user reads own data as safe svg.                                    |
| `AuthenticationAndPermissionMatrix_RefusesEveryoneButAuthorisedUsersAsync` | Fact | Authentication and permission matrix refuses everyone but authorised users.    |
| `CrossTenant_ForeignAndUnknownIdsAnswerIndistinguishablyAsync`             | Fact | Cross tenant foreign and unknown ids answer indistinguishably.                 |
| `IdEnumeration_NeighbouringIdsAreOwnDataOrTheEmptyStateNeverForeignAsync`  | Fact | Id enumeration neighbouring ids are own data or the empty state never foreign. |
| `EmptyStates_UnknownEmptyAndSoftDeletedAreByteForByteEqualAsync`           | Fact | Empty states unknown empty and soft deleted are byte for byte equal.           |
| `NumericEdgeCases_NaNInfinityAndOverflowNeverBreakTheSvgAsync`             | Fact | Numeric edge cases na n infinity and overflow never break the svg.             |
| `ConcurrentReads_AreDeterministicAndBoundedAsync`                          | Fact | Concurrent reads are deterministic and bounded.                                |
| `IdBoundariesAndTypeConfusion_AreRejectedOrEmptyNeverServerErrorsAsync`    | Fact | Id boundaries and type confusion are rejected or empty never server errors.    |
| `HostileQueryStringAndHeaders_NeverChangeTheChartAsync`                    | Fact | Hostile query string and headers never change the chart.                       |
| `VerbTampering_OnlyGetIsServedAsync`                                       | Fact | Verb tampering only get is served.                                             |
| `HostileVariableNames_AreEscapedAndNeverBecomeMarkupAsync`                 | Fact | Hostile variable names are escaped and never become markup.                    |

### PenTestExhaustiveChartLearningCurve

`Jube.Test.Security.QueryExhaustive.PenTestExhaustiveChartLearningCurve` — 11 tests. Category: PenTest.

| Test                                                                       | Kind | Verifies                                                                       |
|----------------------------------------------------------------------------|------|--------------------------------------------------------------------------------|
| `AuthorisedUser_ReadsOwnDataAsSafeSvgAsync`                                | Fact | Authorised user reads own data as safe svg.                                    |
| `AuthenticationAndPermissionMatrix_RefusesEveryoneButAuthorisedUsersAsync` | Fact | Authentication and permission matrix refuses everyone but authorised users.    |
| `CrossTenant_ForeignAndUnknownIdsAnswerIndistinguishablyAsync`             | Fact | Cross tenant foreign and unknown ids answer indistinguishably.                 |
| `IdEnumeration_NeighbouringIdsAreOwnDataOrTheEmptyStateNeverForeignAsync`  | Fact | Id enumeration neighbouring ids are own data or the empty state never foreign. |
| `EmptyStates_UnknownEmptyAndSoftDeletedAreByteForByteEqualAsync`           | Fact | Empty states unknown empty and soft deleted are byte for byte equal.           |
| `NumericEdgeCases_NaNInfinityAndOverflowNeverBreakTheSvgAsync`             | Fact | Numeric edge cases na n infinity and overflow never break the svg.             |
| `ConcurrentReads_AreDeterministicAndBoundedAsync`                          | Fact | Concurrent reads are deterministic and bounded.                                |
| `IdBoundariesAndTypeConfusion_AreRejectedOrEmptyNeverServerErrorsAsync`    | Fact | Id boundaries and type confusion are rejected or empty never server errors.    |
| `HostileQueryStringAndHeaders_NeverChangeTheChartAsync`                    | Fact | Hostile query string and headers never change the chart.                       |
| `VerbTampering_OnlyGetIsServedAsync`                                       | Fact | Verb tampering only get is served.                                             |
| `HostileVariableNames_AreEscapedAndNeverBecomeMarkupAsync`                 | Fact | Hostile variable names are escaped and never become markup.                    |

### PenTestExhaustiveChartPerformanceCalibration

`Jube.Test.Security.QueryExhaustive.PenTestExhaustiveChartPerformanceCalibration` — 11 tests. Category: PenTest.

| Test                                                                       | Kind | Verifies                                                                       |
|----------------------------------------------------------------------------|------|--------------------------------------------------------------------------------|
| `AuthorisedUser_ReadsOwnDataAsSafeSvgAsync`                                | Fact | Authorised user reads own data as safe svg.                                    |
| `AuthenticationAndPermissionMatrix_RefusesEveryoneButAuthorisedUsersAsync` | Fact | Authentication and permission matrix refuses everyone but authorised users.    |
| `CrossTenant_ForeignAndUnknownIdsAnswerIndistinguishablyAsync`             | Fact | Cross tenant foreign and unknown ids answer indistinguishably.                 |
| `IdEnumeration_NeighbouringIdsAreOwnDataOrTheEmptyStateNeverForeignAsync`  | Fact | Id enumeration neighbouring ids are own data or the empty state never foreign. |
| `EmptyStates_UnknownEmptyAndSoftDeletedAreByteForByteEqualAsync`           | Fact | Empty states unknown empty and soft deleted are byte for byte equal.           |
| `NumericEdgeCases_NaNInfinityAndOverflowNeverBreakTheSvgAsync`             | Fact | Numeric edge cases na n infinity and overflow never break the svg.             |
| `ConcurrentReads_AreDeterministicAndBoundedAsync`                          | Fact | Concurrent reads are deterministic and bounded.                                |
| `IdBoundariesAndTypeConfusion_AreRejectedOrEmptyNeverServerErrorsAsync`    | Fact | Id boundaries and type confusion are rejected or empty never server errors.    |
| `HostileQueryStringAndHeaders_NeverChangeTheChartAsync`                    | Fact | Hostile query string and headers never change the chart.                       |
| `VerbTampering_OnlyGetIsServedAsync`                                       | Fact | Verb tampering only get is served.                                             |
| `HostileVariableNames_AreEscapedAndNeverBecomeMarkupAsync`                 | Fact | Hostile variable names are escaped and never become markup.                    |

### PenTestExhaustiveChartPerformanceConfusion

`Jube.Test.Security.QueryExhaustive.PenTestExhaustiveChartPerformanceConfusion` — 11 tests. Category: PenTest.

| Test                                                                       | Kind | Verifies                                                                       |
|----------------------------------------------------------------------------|------|--------------------------------------------------------------------------------|
| `AuthorisedUser_ReadsOwnDataAsSafeSvgAsync`                                | Fact | Authorised user reads own data as safe svg.                                    |
| `AuthenticationAndPermissionMatrix_RefusesEveryoneButAuthorisedUsersAsync` | Fact | Authentication and permission matrix refuses everyone but authorised users.    |
| `CrossTenant_ForeignAndUnknownIdsAnswerIndistinguishablyAsync`             | Fact | Cross tenant foreign and unknown ids answer indistinguishably.                 |
| `IdEnumeration_NeighbouringIdsAreOwnDataOrTheEmptyStateNeverForeignAsync`  | Fact | Id enumeration neighbouring ids are own data or the empty state never foreign. |
| `EmptyStates_UnknownEmptyAndSoftDeletedAreByteForByteEqualAsync`           | Fact | Empty states unknown empty and soft deleted are byte for byte equal.           |
| `NumericEdgeCases_NaNInfinityAndOverflowNeverBreakTheSvgAsync`             | Fact | Numeric edge cases na n infinity and overflow never break the svg.             |
| `ConcurrentReads_AreDeterministicAndBoundedAsync`                          | Fact | Concurrent reads are deterministic and bounded.                                |
| `IdBoundariesAndTypeConfusion_AreRejectedOrEmptyNeverServerErrorsAsync`    | Fact | Id boundaries and type confusion are rejected or empty never server errors.    |
| `HostileQueryStringAndHeaders_NeverChangeTheChartAsync`                    | Fact | Hostile query string and headers never change the chart.                       |
| `VerbTampering_OnlyGetIsServedAsync`                                       | Fact | Verb tampering only get is served.                                             |
| `HostileVariableNames_AreEscapedAndNeverBecomeMarkupAsync`                 | Fact | Hostile variable names are escaped and never become markup.                    |

### PenTestExhaustiveChartPerformanceGains

`Jube.Test.Security.QueryExhaustive.PenTestExhaustiveChartPerformanceGains` — 11 tests. Category: PenTest.

| Test                                                                       | Kind | Verifies                                                                       |
|----------------------------------------------------------------------------|------|--------------------------------------------------------------------------------|
| `AuthorisedUser_ReadsOwnDataAsSafeSvgAsync`                                | Fact | Authorised user reads own data as safe svg.                                    |
| `AuthenticationAndPermissionMatrix_RefusesEveryoneButAuthorisedUsersAsync` | Fact | Authentication and permission matrix refuses everyone but authorised users.    |
| `CrossTenant_ForeignAndUnknownIdsAnswerIndistinguishablyAsync`             | Fact | Cross tenant foreign and unknown ids answer indistinguishably.                 |
| `IdEnumeration_NeighbouringIdsAreOwnDataOrTheEmptyStateNeverForeignAsync`  | Fact | Id enumeration neighbouring ids are own data or the empty state never foreign. |
| `EmptyStates_UnknownEmptyAndSoftDeletedAreByteForByteEqualAsync`           | Fact | Empty states unknown empty and soft deleted are byte for byte equal.           |
| `NumericEdgeCases_NaNInfinityAndOverflowNeverBreakTheSvgAsync`             | Fact | Numeric edge cases na n infinity and overflow never break the svg.             |
| `ConcurrentReads_AreDeterministicAndBoundedAsync`                          | Fact | Concurrent reads are deterministic and bounded.                                |
| `IdBoundariesAndTypeConfusion_AreRejectedOrEmptyNeverServerErrorsAsync`    | Fact | Id boundaries and type confusion are rejected or empty never server errors.    |
| `HostileQueryStringAndHeaders_NeverChangeTheChartAsync`                    | Fact | Hostile query string and headers never change the chart.                       |
| `VerbTampering_OnlyGetIsServedAsync`                                       | Fact | Verb tampering only get is served.                                             |
| `HostileVariableNames_AreEscapedAndNeverBecomeMarkupAsync`                 | Fact | Hostile variable names are escaped and never become markup.                    |

### PenTestExhaustiveChartPerformanceKs

`Jube.Test.Security.QueryExhaustive.PenTestExhaustiveChartPerformanceKs` — 11 tests. Category: PenTest.

| Test                                                                       | Kind | Verifies                                                                       |
|----------------------------------------------------------------------------|------|--------------------------------------------------------------------------------|
| `AuthorisedUser_ReadsOwnDataAsSafeSvgAsync`                                | Fact | Authorised user reads own data as safe svg.                                    |
| `AuthenticationAndPermissionMatrix_RefusesEveryoneButAuthorisedUsersAsync` | Fact | Authentication and permission matrix refuses everyone but authorised users.    |
| `CrossTenant_ForeignAndUnknownIdsAnswerIndistinguishablyAsync`             | Fact | Cross tenant foreign and unknown ids answer indistinguishably.                 |
| `IdEnumeration_NeighbouringIdsAreOwnDataOrTheEmptyStateNeverForeignAsync`  | Fact | Id enumeration neighbouring ids are own data or the empty state never foreign. |
| `EmptyStates_UnknownEmptyAndSoftDeletedAreByteForByteEqualAsync`           | Fact | Empty states unknown empty and soft deleted are byte for byte equal.           |
| `NumericEdgeCases_NaNInfinityAndOverflowNeverBreakTheSvgAsync`             | Fact | Numeric edge cases na n infinity and overflow never break the svg.             |
| `ConcurrentReads_AreDeterministicAndBoundedAsync`                          | Fact | Concurrent reads are deterministic and bounded.                                |
| `IdBoundariesAndTypeConfusion_AreRejectedOrEmptyNeverServerErrorsAsync`    | Fact | Id boundaries and type confusion are rejected or empty never server errors.    |
| `HostileQueryStringAndHeaders_NeverChangeTheChartAsync`                    | Fact | Hostile query string and headers never change the chart.                       |
| `VerbTampering_OnlyGetIsServedAsync`                                       | Fact | Verb tampering only get is served.                                             |
| `HostileVariableNames_AreEscapedAndNeverBecomeMarkupAsync`                 | Fact | Hostile variable names are escaped and never become markup.                    |

### PenTestExhaustiveChartPerformanceLift

`Jube.Test.Security.QueryExhaustive.PenTestExhaustiveChartPerformanceLift` — 11 tests. Category: PenTest.

| Test                                                                       | Kind | Verifies                                                                       |
|----------------------------------------------------------------------------|------|--------------------------------------------------------------------------------|
| `AuthorisedUser_ReadsOwnDataAsSafeSvgAsync`                                | Fact | Authorised user reads own data as safe svg.                                    |
| `AuthenticationAndPermissionMatrix_RefusesEveryoneButAuthorisedUsersAsync` | Fact | Authentication and permission matrix refuses everyone but authorised users.    |
| `CrossTenant_ForeignAndUnknownIdsAnswerIndistinguishablyAsync`             | Fact | Cross tenant foreign and unknown ids answer indistinguishably.                 |
| `IdEnumeration_NeighbouringIdsAreOwnDataOrTheEmptyStateNeverForeignAsync`  | Fact | Id enumeration neighbouring ids are own data or the empty state never foreign. |
| `EmptyStates_UnknownEmptyAndSoftDeletedAreByteForByteEqualAsync`           | Fact | Empty states unknown empty and soft deleted are byte for byte equal.           |
| `NumericEdgeCases_NaNInfinityAndOverflowNeverBreakTheSvgAsync`             | Fact | Numeric edge cases na n infinity and overflow never break the svg.             |
| `ConcurrentReads_AreDeterministicAndBoundedAsync`                          | Fact | Concurrent reads are deterministic and bounded.                                |
| `IdBoundariesAndTypeConfusion_AreRejectedOrEmptyNeverServerErrorsAsync`    | Fact | Id boundaries and type confusion are rejected or empty never server errors.    |
| `HostileQueryStringAndHeaders_NeverChangeTheChartAsync`                    | Fact | Hostile query string and headers never change the chart.                       |
| `VerbTampering_OnlyGetIsServedAsync`                                       | Fact | Verb tampering only get is served.                                             |
| `HostileVariableNames_AreEscapedAndNeverBecomeMarkupAsync`                 | Fact | Hostile variable names are escaped and never become markup.                    |

### PenTestExhaustiveChartPerformanceMeasures

`Jube.Test.Security.QueryExhaustive.PenTestExhaustiveChartPerformanceMeasures` — 11 tests. Category: PenTest.

| Test                                                                       | Kind | Verifies                                                                       |
|----------------------------------------------------------------------------|------|--------------------------------------------------------------------------------|
| `AuthorisedUser_ReadsOwnDataAsSafeSvgAsync`                                | Fact | Authorised user reads own data as safe svg.                                    |
| `AuthenticationAndPermissionMatrix_RefusesEveryoneButAuthorisedUsersAsync` | Fact | Authentication and permission matrix refuses everyone but authorised users.    |
| `CrossTenant_ForeignAndUnknownIdsAnswerIndistinguishablyAsync`             | Fact | Cross tenant foreign and unknown ids answer indistinguishably.                 |
| `IdEnumeration_NeighbouringIdsAreOwnDataOrTheEmptyStateNeverForeignAsync`  | Fact | Id enumeration neighbouring ids are own data or the empty state never foreign. |
| `EmptyStates_UnknownEmptyAndSoftDeletedAreByteForByteEqualAsync`           | Fact | Empty states unknown empty and soft deleted are byte for byte equal.           |
| `NumericEdgeCases_NaNInfinityAndOverflowNeverBreakTheSvgAsync`             | Fact | Numeric edge cases na n infinity and overflow never break the svg.             |
| `ConcurrentReads_AreDeterministicAndBoundedAsync`                          | Fact | Concurrent reads are deterministic and bounded.                                |
| `IdBoundariesAndTypeConfusion_AreRejectedOrEmptyNeverServerErrorsAsync`    | Fact | Id boundaries and type confusion are rejected or empty never server errors.    |
| `HostileQueryStringAndHeaders_NeverChangeTheChartAsync`                    | Fact | Hostile query string and headers never change the chart.                       |
| `VerbTampering_OnlyGetIsServedAsync`                                       | Fact | Verb tampering only get is served.                                             |
| `HostileVariableNames_AreEscapedAndNeverBecomeMarkupAsync`                 | Fact | Hostile variable names are escaped and never become markup.                    |

### PenTestExhaustiveChartPerformanceParameters

`Jube.Test.Security.QueryExhaustive.PenTestExhaustiveChartPerformanceParameters` — 13 tests. Category: PenTest.

| Test                                                                         | Kind | Verifies                                                                             |
|------------------------------------------------------------------------------|------|--------------------------------------------------------------------------------------|
| `AuthorisedUser_ReadsOwnDataAsSafeSvgAsync`                                  | Fact | Authorised user reads own data as safe svg.                                          |
| `AuthenticationAndPermissionMatrix_RefusesEveryoneButAuthorisedUsersAsync`   | Fact | Authentication and permission matrix refuses everyone but authorised users.          |
| `CrossTenant_ForeignAndUnknownIdsAnswerIndistinguishablyAsync`               | Fact | Cross tenant foreign and unknown ids answer indistinguishably.                       |
| `IdEnumeration_NeighbouringIdsAreOwnDataOrTheEmptyStateNeverForeignAsync`    | Fact | Id enumeration neighbouring ids are own data or the empty state never foreign.       |
| `EmptyStates_UnknownEmptyAndSoftDeletedAreByteForByteEqualAsync`             | Fact | Empty states unknown empty and soft deleted are byte for byte equal.                 |
| `NumericEdgeCases_NaNInfinityAndOverflowNeverBreakTheSvgAsync`               | Fact | Numeric edge cases na n infinity and overflow never break the svg.                   |
| `ConcurrentReads_AreDeterministicAndBoundedAsync`                            | Fact | Concurrent reads are deterministic and bounded.                                      |
| `IdBoundariesAndTypeConfusion_AreRejectedOrEmptyNeverServerErrorsAsync`      | Fact | Id boundaries and type confusion are rejected or empty never server errors.          |
| `HostileQueryStringAndHeaders_NeverChangeTheChartAsync`                      | Fact | Hostile query string and headers never change the chart.                             |
| `VerbTampering_OnlyGetIsServedAsync`                                         | Fact | Verb tampering only get is served.                                                   |
| `HostileVariableNames_AreEscapedAndNeverBecomeMarkupAsync`                   | Fact | Hostile variable names are escaped and never become markup.                          |
| `ThresholdAndCostParameters_AreValidatedNeverServerErrorsNorReflectedAsync`  | Fact | Threshold and cost parameters are validated never server errors nor reflected.       |
| `CostsMoveTheMinimumRiskLineOnlyForTheOwnerAndNeverExposeAnotherTenantAsync` | Fact | Costs move the minimum risk line only for the owner and never expose another tenant. |

### PenTestExhaustiveChartPerformancePrecisionRecall

`Jube.Test.Security.QueryExhaustive.PenTestExhaustiveChartPerformancePrecisionRecall` — 11 tests. Category: PenTest.

| Test                                                                       | Kind | Verifies                                                                       |
|----------------------------------------------------------------------------|------|--------------------------------------------------------------------------------|
| `AuthorisedUser_ReadsOwnDataAsSafeSvgAsync`                                | Fact | Authorised user reads own data as safe svg.                                    |
| `AuthenticationAndPermissionMatrix_RefusesEveryoneButAuthorisedUsersAsync` | Fact | Authentication and permission matrix refuses everyone but authorised users.    |
| `CrossTenant_ForeignAndUnknownIdsAnswerIndistinguishablyAsync`             | Fact | Cross tenant foreign and unknown ids answer indistinguishably.                 |
| `IdEnumeration_NeighbouringIdsAreOwnDataOrTheEmptyStateNeverForeignAsync`  | Fact | Id enumeration neighbouring ids are own data or the empty state never foreign. |
| `EmptyStates_UnknownEmptyAndSoftDeletedAreByteForByteEqualAsync`           | Fact | Empty states unknown empty and soft deleted are byte for byte equal.           |
| `NumericEdgeCases_NaNInfinityAndOverflowNeverBreakTheSvgAsync`             | Fact | Numeric edge cases na n infinity and overflow never break the svg.             |
| `ConcurrentReads_AreDeterministicAndBoundedAsync`                          | Fact | Concurrent reads are deterministic and bounded.                                |
| `IdBoundariesAndTypeConfusion_AreRejectedOrEmptyNeverServerErrorsAsync`    | Fact | Id boundaries and type confusion are rejected or empty never server errors.    |
| `HostileQueryStringAndHeaders_NeverChangeTheChartAsync`                    | Fact | Hostile query string and headers never change the chart.                       |
| `VerbTampering_OnlyGetIsServedAsync`                                       | Fact | Verb tampering only get is served.                                             |
| `HostileVariableNames_AreEscapedAndNeverBecomeMarkupAsync`                 | Fact | Hostile variable names are escaped and never become markup.                    |

### PenTestExhaustiveChartPerformancePromotionHistory

`Jube.Test.Security.QueryExhaustive.PenTestExhaustiveChartPerformancePromotionHistory` — 11 tests. Category: PenTest.

| Test                                                                       | Kind | Verifies                                                                       |
|----------------------------------------------------------------------------|------|--------------------------------------------------------------------------------|
| `AuthorisedUser_ReadsOwnDataAsSafeSvgAsync`                                | Fact | Authorised user reads own data as safe svg.                                    |
| `AuthenticationAndPermissionMatrix_RefusesEveryoneButAuthorisedUsersAsync` | Fact | Authentication and permission matrix refuses everyone but authorised users.    |
| `CrossTenant_ForeignAndUnknownIdsAnswerIndistinguishablyAsync`             | Fact | Cross tenant foreign and unknown ids answer indistinguishably.                 |
| `IdEnumeration_NeighbouringIdsAreOwnDataOrTheEmptyStateNeverForeignAsync`  | Fact | Id enumeration neighbouring ids are own data or the empty state never foreign. |
| `EmptyStates_UnknownEmptyAndSoftDeletedAreByteForByteEqualAsync`           | Fact | Empty states unknown empty and soft deleted are byte for byte equal.           |
| `NumericEdgeCases_NaNInfinityAndOverflowNeverBreakTheSvgAsync`             | Fact | Numeric edge cases na n infinity and overflow never break the svg.             |
| `ConcurrentReads_AreDeterministicAndBoundedAsync`                          | Fact | Concurrent reads are deterministic and bounded.                                |
| `IdBoundariesAndTypeConfusion_AreRejectedOrEmptyNeverServerErrorsAsync`    | Fact | Id boundaries and type confusion are rejected or empty never server errors.    |
| `HostileQueryStringAndHeaders_NeverChangeTheChartAsync`                    | Fact | Hostile query string and headers never change the chart.                       |
| `VerbTampering_OnlyGetIsServedAsync`                                       | Fact | Verb tampering only get is served.                                             |
| `HostileVariableNames_AreEscapedAndNeverBecomeMarkupAsync`                 | Fact | Hostile variable names are escaped and never become markup.                    |

### PenTestExhaustiveChartPerformanceRoc

`Jube.Test.Security.QueryExhaustive.PenTestExhaustiveChartPerformanceRoc` — 11 tests. Category: PenTest.

| Test                                                                       | Kind | Verifies                                                                       |
|----------------------------------------------------------------------------|------|--------------------------------------------------------------------------------|
| `AuthorisedUser_ReadsOwnDataAsSafeSvgAsync`                                | Fact | Authorised user reads own data as safe svg.                                    |
| `AuthenticationAndPermissionMatrix_RefusesEveryoneButAuthorisedUsersAsync` | Fact | Authentication and permission matrix refuses everyone but authorised users.    |
| `CrossTenant_ForeignAndUnknownIdsAnswerIndistinguishablyAsync`             | Fact | Cross tenant foreign and unknown ids answer indistinguishably.                 |
| `IdEnumeration_NeighbouringIdsAreOwnDataOrTheEmptyStateNeverForeignAsync`  | Fact | Id enumeration neighbouring ids are own data or the empty state never foreign. |
| `EmptyStates_UnknownEmptyAndSoftDeletedAreByteForByteEqualAsync`           | Fact | Empty states unknown empty and soft deleted are byte for byte equal.           |
| `NumericEdgeCases_NaNInfinityAndOverflowNeverBreakTheSvgAsync`             | Fact | Numeric edge cases na n infinity and overflow never break the svg.             |
| `ConcurrentReads_AreDeterministicAndBoundedAsync`                          | Fact | Concurrent reads are deterministic and bounded.                                |
| `IdBoundariesAndTypeConfusion_AreRejectedOrEmptyNeverServerErrorsAsync`    | Fact | Id boundaries and type confusion are rejected or empty never server errors.    |
| `HostileQueryStringAndHeaders_NeverChangeTheChartAsync`                    | Fact | Hostile query string and headers never change the chart.                       |
| `VerbTampering_OnlyGetIsServedAsync`                                       | Fact | Verb tampering only get is served.                                             |
| `HostileVariableNames_AreEscapedAndNeverBecomeMarkupAsync`                 | Fact | Hostile variable names are escaped and never become markup.                    |

### PenTestExhaustiveChartPerformanceScoreDistribution

`Jube.Test.Security.QueryExhaustive.PenTestExhaustiveChartPerformanceScoreDistribution` — 11 tests. Category: PenTest.

| Test                                                                       | Kind | Verifies                                                                       |
|----------------------------------------------------------------------------|------|--------------------------------------------------------------------------------|
| `AuthorisedUser_ReadsOwnDataAsSafeSvgAsync`                                | Fact | Authorised user reads own data as safe svg.                                    |
| `AuthenticationAndPermissionMatrix_RefusesEveryoneButAuthorisedUsersAsync` | Fact | Authentication and permission matrix refuses everyone but authorised users.    |
| `CrossTenant_ForeignAndUnknownIdsAnswerIndistinguishablyAsync`             | Fact | Cross tenant foreign and unknown ids answer indistinguishably.                 |
| `IdEnumeration_NeighbouringIdsAreOwnDataOrTheEmptyStateNeverForeignAsync`  | Fact | Id enumeration neighbouring ids are own data or the empty state never foreign. |
| `EmptyStates_UnknownEmptyAndSoftDeletedAreByteForByteEqualAsync`           | Fact | Empty states unknown empty and soft deleted are byte for byte equal.           |
| `NumericEdgeCases_NaNInfinityAndOverflowNeverBreakTheSvgAsync`             | Fact | Numeric edge cases na n infinity and overflow never break the svg.             |
| `ConcurrentReads_AreDeterministicAndBoundedAsync`                          | Fact | Concurrent reads are deterministic and bounded.                                |
| `IdBoundariesAndTypeConfusion_AreRejectedOrEmptyNeverServerErrorsAsync`    | Fact | Id boundaries and type confusion are rejected or empty never server errors.    |
| `HostileQueryStringAndHeaders_NeverChangeTheChartAsync`                    | Fact | Hostile query string and headers never change the chart.                       |
| `VerbTampering_OnlyGetIsServedAsync`                                       | Fact | Verb tampering only get is served.                                             |
| `HostileVariableNames_AreEscapedAndNeverBecomeMarkupAsync`                 | Fact | Hostile variable names are escaped and never become markup.                    |

### PenTestExhaustiveChartPerformanceThresholdSweep

`Jube.Test.Security.QueryExhaustive.PenTestExhaustiveChartPerformanceThresholdSweep` — 11 tests. Category: PenTest.

| Test                                                                       | Kind | Verifies                                                                       |
|----------------------------------------------------------------------------|------|--------------------------------------------------------------------------------|
| `AuthorisedUser_ReadsOwnDataAsSafeSvgAsync`                                | Fact | Authorised user reads own data as safe svg.                                    |
| `AuthenticationAndPermissionMatrix_RefusesEveryoneButAuthorisedUsersAsync` | Fact | Authentication and permission matrix refuses everyone but authorised users.    |
| `CrossTenant_ForeignAndUnknownIdsAnswerIndistinguishablyAsync`             | Fact | Cross tenant foreign and unknown ids answer indistinguishably.                 |
| `IdEnumeration_NeighbouringIdsAreOwnDataOrTheEmptyStateNeverForeignAsync`  | Fact | Id enumeration neighbouring ids are own data or the empty state never foreign. |
| `EmptyStates_UnknownEmptyAndSoftDeletedAreByteForByteEqualAsync`           | Fact | Empty states unknown empty and soft deleted are byte for byte equal.           |
| `NumericEdgeCases_NaNInfinityAndOverflowNeverBreakTheSvgAsync`             | Fact | Numeric edge cases na n infinity and overflow never break the svg.             |
| `ConcurrentReads_AreDeterministicAndBoundedAsync`                          | Fact | Concurrent reads are deterministic and bounded.                                |
| `IdBoundariesAndTypeConfusion_AreRejectedOrEmptyNeverServerErrorsAsync`    | Fact | Id boundaries and type confusion are rejected or empty never server errors.    |
| `HostileQueryStringAndHeaders_NeverChangeTheChartAsync`                    | Fact | Hostile query string and headers never change the chart.                       |
| `VerbTampering_OnlyGetIsServedAsync`                                       | Fact | Verb tampering only get is served.                                             |
| `HostileVariableNames_AreEscapedAndNeverBecomeMarkupAsync`                 | Fact | Hostile variable names are escaped and never become markup.                    |

### PenTestExhaustiveChartPerformanceTopology

`Jube.Test.Security.QueryExhaustive.PenTestExhaustiveChartPerformanceTopology` — 11 tests. Category: PenTest.

| Test                                                                       | Kind | Verifies                                                                       |
|----------------------------------------------------------------------------|------|--------------------------------------------------------------------------------|
| `AuthorisedUser_ReadsOwnDataAsSafeSvgAsync`                                | Fact | Authorised user reads own data as safe svg.                                    |
| `AuthenticationAndPermissionMatrix_RefusesEveryoneButAuthorisedUsersAsync` | Fact | Authentication and permission matrix refuses everyone but authorised users.    |
| `CrossTenant_ForeignAndUnknownIdsAnswerIndistinguishablyAsync`             | Fact | Cross tenant foreign and unknown ids answer indistinguishably.                 |
| `IdEnumeration_NeighbouringIdsAreOwnDataOrTheEmptyStateNeverForeignAsync`  | Fact | Id enumeration neighbouring ids are own data or the empty state never foreign. |
| `EmptyStates_UnknownEmptyAndSoftDeletedAreByteForByteEqualAsync`           | Fact | Empty states unknown empty and soft deleted are byte for byte equal.           |
| `NumericEdgeCases_NaNInfinityAndOverflowNeverBreakTheSvgAsync`             | Fact | Numeric edge cases na n infinity and overflow never break the svg.             |
| `ConcurrentReads_AreDeterministicAndBoundedAsync`                          | Fact | Concurrent reads are deterministic and bounded.                                |
| `IdBoundariesAndTypeConfusion_AreRejectedOrEmptyNeverServerErrorsAsync`    | Fact | Id boundaries and type confusion are rejected or empty never server errors.    |
| `HostileQueryStringAndHeaders_NeverChangeTheChartAsync`                    | Fact | Hostile query string and headers never change the chart.                       |
| `VerbTampering_OnlyGetIsServedAsync`                                       | Fact | Verb tampering only get is served.                                             |
| `HostileVariableNames_AreEscapedAndNeverBecomeMarkupAsync`                 | Fact | Hostile variable names are escaped and never become markup.                    |

### PenTestExhaustiveChartPredictedActual

`Jube.Test.Security.QueryExhaustive.PenTestExhaustiveChartPredictedActual` — 11 tests. Category: PenTest.

| Test                                                                       | Kind | Verifies                                                                       |
|----------------------------------------------------------------------------|------|--------------------------------------------------------------------------------|
| `AuthorisedUser_ReadsOwnDataAsSafeSvgAsync`                                | Fact | Authorised user reads own data as safe svg.                                    |
| `AuthenticationAndPermissionMatrix_RefusesEveryoneButAuthorisedUsersAsync` | Fact | Authentication and permission matrix refuses everyone but authorised users.    |
| `CrossTenant_ForeignAndUnknownIdsAnswerIndistinguishablyAsync`             | Fact | Cross tenant foreign and unknown ids answer indistinguishably.                 |
| `IdEnumeration_NeighbouringIdsAreOwnDataOrTheEmptyStateNeverForeignAsync`  | Fact | Id enumeration neighbouring ids are own data or the empty state never foreign. |
| `EmptyStates_UnknownEmptyAndSoftDeletedAreByteForByteEqualAsync`           | Fact | Empty states unknown empty and soft deleted are byte for byte equal.           |
| `NumericEdgeCases_NaNInfinityAndOverflowNeverBreakTheSvgAsync`             | Fact | Numeric edge cases na n infinity and overflow never break the svg.             |
| `ConcurrentReads_AreDeterministicAndBoundedAsync`                          | Fact | Concurrent reads are deterministic and bounded.                                |
| `IdBoundariesAndTypeConfusion_AreRejectedOrEmptyNeverServerErrorsAsync`    | Fact | Id boundaries and type confusion are rejected or empty never server errors.    |
| `HostileQueryStringAndHeaders_NeverChangeTheChartAsync`                    | Fact | Hostile query string and headers never change the chart.                       |
| `VerbTampering_OnlyGetIsServedAsync`                                       | Fact | Verb tampering only get is served.                                             |
| `HostileVariableNames_AreEscapedAndNeverBecomeMarkupAsync`                 | Fact | Hostile variable names are escaped and never become markup.                    |

### PenTestExhaustiveChartRoc

`Jube.Test.Security.QueryExhaustive.PenTestExhaustiveChartRoc` — 11 tests. Category: PenTest.

| Test                                                                       | Kind | Verifies                                                                       |
|----------------------------------------------------------------------------|------|--------------------------------------------------------------------------------|
| `AuthorisedUser_ReadsOwnDataAsSafeSvgAsync`                                | Fact | Authorised user reads own data as safe svg.                                    |
| `AuthenticationAndPermissionMatrix_RefusesEveryoneButAuthorisedUsersAsync` | Fact | Authentication and permission matrix refuses everyone but authorised users.    |
| `CrossTenant_ForeignAndUnknownIdsAnswerIndistinguishablyAsync`             | Fact | Cross tenant foreign and unknown ids answer indistinguishably.                 |
| `IdEnumeration_NeighbouringIdsAreOwnDataOrTheEmptyStateNeverForeignAsync`  | Fact | Id enumeration neighbouring ids are own data or the empty state never foreign. |
| `EmptyStates_UnknownEmptyAndSoftDeletedAreByteForByteEqualAsync`           | Fact | Empty states unknown empty and soft deleted are byte for byte equal.           |
| `NumericEdgeCases_NaNInfinityAndOverflowNeverBreakTheSvgAsync`             | Fact | Numeric edge cases na n infinity and overflow never break the svg.             |
| `ConcurrentReads_AreDeterministicAndBoundedAsync`                          | Fact | Concurrent reads are deterministic and bounded.                                |
| `IdBoundariesAndTypeConfusion_AreRejectedOrEmptyNeverServerErrorsAsync`    | Fact | Id boundaries and type confusion are rejected or empty never server errors.    |
| `HostileQueryStringAndHeaders_NeverChangeTheChartAsync`                    | Fact | Hostile query string and headers never change the chart.                       |
| `VerbTampering_OnlyGetIsServedAsync`                                       | Fact | Verb tampering only get is served.                                             |
| `HostileVariableNames_AreEscapedAndNeverBecomeMarkupAsync`                 | Fact | Hostile variable names are escaped and never become markup.                    |

### PenTestExhaustiveChartSensitivity

`Jube.Test.Security.QueryExhaustive.PenTestExhaustiveChartSensitivity` — 11 tests. Category: PenTest.

| Test                                                                       | Kind | Verifies                                                                       |
|----------------------------------------------------------------------------|------|--------------------------------------------------------------------------------|
| `AuthorisedUser_ReadsOwnDataAsSafeSvgAsync`                                | Fact | Authorised user reads own data as safe svg.                                    |
| `AuthenticationAndPermissionMatrix_RefusesEveryoneButAuthorisedUsersAsync` | Fact | Authentication and permission matrix refuses everyone but authorised users.    |
| `CrossTenant_ForeignAndUnknownIdsAnswerIndistinguishablyAsync`             | Fact | Cross tenant foreign and unknown ids answer indistinguishably.                 |
| `IdEnumeration_NeighbouringIdsAreOwnDataOrTheEmptyStateNeverForeignAsync`  | Fact | Id enumeration neighbouring ids are own data or the empty state never foreign. |
| `EmptyStates_UnknownEmptyAndSoftDeletedAreByteForByteEqualAsync`           | Fact | Empty states unknown empty and soft deleted are byte for byte equal.           |
| `NumericEdgeCases_NaNInfinityAndOverflowNeverBreakTheSvgAsync`             | Fact | Numeric edge cases na n infinity and overflow never break the svg.             |
| `ConcurrentReads_AreDeterministicAndBoundedAsync`                          | Fact | Concurrent reads are deterministic and bounded.                                |
| `IdBoundariesAndTypeConfusion_AreRejectedOrEmptyNeverServerErrorsAsync`    | Fact | Id boundaries and type confusion are rejected or empty never server errors.    |
| `HostileQueryStringAndHeaders_NeverChangeTheChartAsync`                    | Fact | Hostile query string and headers never change the chart.                       |
| `VerbTampering_OnlyGetIsServedAsync`                                       | Fact | Verb tampering only get is served.                                             |
| `HostileVariableNames_AreEscapedAndNeverBecomeMarkupAsync`                 | Fact | Hostile variable names are escaped and never become markup.                    |

### PenTestExhaustiveChartStylesheet

`Jube.Test.Security.QueryExhaustive.PenTestExhaustiveChartStylesheet` — 3 tests. Category: PenTest.

| Test                                                               | Kind | Verifies                                                            |
|--------------------------------------------------------------------|------|---------------------------------------------------------------------|
| `CssDirectory_HoldsOnlyTheGeneratedChartStylesheet`                | Fact | Css directory holds only the generated chart stylesheet.            |
| `Stylesheet_IsServedAnonymouslyAsInertCssWithNoTenantDataAsync`    | Fact | Stylesheet is served anonymously as inert css with no tenant data.  |
| `Stylesheet_TraversalAndVerbTamperingNeverExposeAnythingElseAsync` | Fact | Stylesheet traversal and verb tampering never expose anything else. |

### PenTestExhaustiveChartVariableHistogram

`Jube.Test.Security.QueryExhaustive.PenTestExhaustiveChartVariableHistogram` — 12 tests. Category: PenTest.

| Test                                                                       | Kind | Verifies                                                                       |
|----------------------------------------------------------------------------|------|--------------------------------------------------------------------------------|
| `AuthorisedUser_ReadsOwnDataAsSafeSvgAsync`                                | Fact | Authorised user reads own data as safe svg.                                    |
| `AuthenticationAndPermissionMatrix_RefusesEveryoneButAuthorisedUsersAsync` | Fact | Authentication and permission matrix refuses everyone but authorised users.    |
| `CrossTenant_ForeignAndUnknownIdsAnswerIndistinguishablyAsync`             | Fact | Cross tenant foreign and unknown ids answer indistinguishably.                 |
| `IdEnumeration_NeighbouringIdsAreOwnDataOrTheEmptyStateNeverForeignAsync`  | Fact | Id enumeration neighbouring ids are own data or the empty state never foreign. |
| `EmptyStates_UnknownEmptyAndSoftDeletedAreByteForByteEqualAsync`           | Fact | Empty states unknown empty and soft deleted are byte for byte equal.           |
| `NumericEdgeCases_NaNInfinityAndOverflowNeverBreakTheSvgAsync`             | Fact | Numeric edge cases na n infinity and overflow never break the svg.             |
| `ConcurrentReads_AreDeterministicAndBoundedAsync`                          | Fact | Concurrent reads are deterministic and bounded.                                |
| `IdBoundariesAndTypeConfusion_AreRejectedOrEmptyNeverServerErrorsAsync`    | Fact | Id boundaries and type confusion are rejected or empty never server errors.    |
| `HostileQueryStringAndHeaders_NeverChangeTheChartAsync`                    | Fact | Hostile query string and headers never change the chart.                       |
| `VerbTampering_OnlyGetIsServedAsync`                                       | Fact | Verb tampering only get is served.                                             |
| `HostileVariableNames_AreEscapedAndNeverBecomeMarkupAsync`                 | Fact | Hostile variable names are escaped and never become markup.                    |
| `VariableOfAnotherInstanceOrTenant_IsTheEmptyStateAsync`                   | Fact | Variable of another instance or tenant is the empty state.                     |

### PenTestExhaustiveSearchInstancePromotedTrialInstanceConfusion

`Jube.Test.Security.QueryExhaustive.PenTestExhaustiveSearchInstancePromotedTrialInstanceConfusion` — 12 tests. Category:
PenTest.

| Test                                                                          | Kind | Verifies                                                                          |
|-------------------------------------------------------------------------------|------|-----------------------------------------------------------------------------------|
| `Counts_AreConsistentAndRatiosAreFiniteAsync`                                 | Fact | Counts are consistent and ratios are finite.                                      |
| `AuthorisedUser_ReadsOwnDataAsWellFormedJsonAsync`                            | Fact | Authorised user reads own data as well formed json.                               |
| `AuthenticationAndPermissionMatrix_RefusesEveryoneButAuthorisedUsersAsync`    | Fact | Authentication and permission matrix refuses everyone but authorised users.       |
| `CrossTenant_ForeignIdLooksExactlyLikeANonExistentIdAsync`                    | Fact | Cross tenant foreign id looks exactly like a non existent id.                     |
| `IdEnumeration_NeighbouringIdsNeverLeakAcrossTenantsAndStayUniformAsync`      | Fact | Id enumeration neighbouring ids never leak across tenants and stay uniform.       |
| `SoftDeleted_RowsAndInstancesAreNeverReturnedAsync`                           | Fact | Soft deleted rows and instances are never returned.                               |
| `EmptyResult_IsAWellFormedEmptyDocumentAsync`                                 | Fact | Empty result is a well formed empty document.                                     |
| `NumericEdgeCases_NaNInfinityZeroDenominatorAndOverflowSerialiseValidlyAsync` | Fact | Numeric edge cases na n infinity zero denominator and overflow serialise validly. |
| `ConcurrentReads_AreDeterministicAndBoundedAsync`                             | Fact | Concurrent reads are deterministic and bounded.                                   |
| `IdBoundariesAndTypeConfusion_AreRejectedOrEmptyNeverServerErrorsAsync`       | Fact | Id boundaries and type confusion are rejected or empty never server errors.       |
| `HostileQueryStringAndHeaders_NeverChangeTheResultAsync`                      | Fact | Hostile query string and headers never change the result.                         |
| `VerbTampering_OnlyGetIsServedAndNothingIsModifiedAsync`                      | Fact | Verb tampering only get is served and nothing is modified.                        |

### PenTestExhaustiveSearchInstancePromotedTrialInstanceErrorHistogram

`Jube.Test.Security.QueryExhaustive.PenTestExhaustiveSearchInstancePromotedTrialInstanceErrorHistogram` — 12 tests.
Category: PenTest.

| Test                                                                          | Kind | Verifies                                                                          |
|-------------------------------------------------------------------------------|------|-----------------------------------------------------------------------------------|
| `Histogram_CountsOnlyTheLiveRowsOfTheOwnersPromotedTrialAsync`                | Fact | Histogram counts only the live rows of the owners promoted trial.                 |
| `AuthorisedUser_ReadsOwnDataAsWellFormedJsonAsync`                            | Fact | Authorised user reads own data as well formed json.                               |
| `AuthenticationAndPermissionMatrix_RefusesEveryoneButAuthorisedUsersAsync`    | Fact | Authentication and permission matrix refuses everyone but authorised users.       |
| `CrossTenant_ForeignIdLooksExactlyLikeANonExistentIdAsync`                    | Fact | Cross tenant foreign id looks exactly like a non existent id.                     |
| `IdEnumeration_NeighbouringIdsNeverLeakAcrossTenantsAndStayUniformAsync`      | Fact | Id enumeration neighbouring ids never leak across tenants and stay uniform.       |
| `SoftDeleted_RowsAndInstancesAreNeverReturnedAsync`                           | Fact | Soft deleted rows and instances are never returned.                               |
| `EmptyResult_IsAWellFormedEmptyDocumentAsync`                                 | Fact | Empty result is a well formed empty document.                                     |
| `NumericEdgeCases_NaNInfinityZeroDenominatorAndOverflowSerialiseValidlyAsync` | Fact | Numeric edge cases na n infinity zero denominator and overflow serialise validly. |
| `ConcurrentReads_AreDeterministicAndBoundedAsync`                             | Fact | Concurrent reads are deterministic and bounded.                                   |
| `IdBoundariesAndTypeConfusion_AreRejectedOrEmptyNeverServerErrorsAsync`       | Fact | Id boundaries and type confusion are rejected or empty never server errors.       |
| `HostileQueryStringAndHeaders_NeverChangeTheResultAsync`                      | Fact | Hostile query string and headers never change the result.                         |
| `VerbTampering_OnlyGetIsServedAndNothingIsModifiedAsync`                      | Fact | Verb tampering only get is served and nothing is modified.                        |

### PenTestExhaustiveSearchInstancePromotedTrialInstanceLearningCurve

`Jube.Test.Security.QueryExhaustive.PenTestExhaustiveSearchInstancePromotedTrialInstanceLearningCurve` — 11 tests.
Category: PenTest.

| Test                                                                          | Kind | Verifies                                                                          |
|-------------------------------------------------------------------------------|------|-----------------------------------------------------------------------------------|
| `AuthorisedUser_ReadsOwnDataAsWellFormedJsonAsync`                            | Fact | Authorised user reads own data as well formed json.                               |
| `AuthenticationAndPermissionMatrix_RefusesEveryoneButAuthorisedUsersAsync`    | Fact | Authentication and permission matrix refuses everyone but authorised users.       |
| `CrossTenant_ForeignIdLooksExactlyLikeANonExistentIdAsync`                    | Fact | Cross tenant foreign id looks exactly like a non existent id.                     |
| `IdEnumeration_NeighbouringIdsNeverLeakAcrossTenantsAndStayUniformAsync`      | Fact | Id enumeration neighbouring ids never leak across tenants and stay uniform.       |
| `SoftDeleted_RowsAndInstancesAreNeverReturnedAsync`                           | Fact | Soft deleted rows and instances are never returned.                               |
| `EmptyResult_IsAWellFormedEmptyDocumentAsync`                                 | Fact | Empty result is a well formed empty document.                                     |
| `NumericEdgeCases_NaNInfinityZeroDenominatorAndOverflowSerialiseValidlyAsync` | Fact | Numeric edge cases na n infinity zero denominator and overflow serialise validly. |
| `ConcurrentReads_AreDeterministicAndBoundedAsync`                             | Fact | Concurrent reads are deterministic and bounded.                                   |
| `IdBoundariesAndTypeConfusion_AreRejectedOrEmptyNeverServerErrorsAsync`       | Fact | Id boundaries and type confusion are rejected or empty never server errors.       |
| `HostileQueryStringAndHeaders_NeverChangeTheResultAsync`                      | Fact | Hostile query string and headers never change the result.                         |
| `VerbTampering_OnlyGetIsServedAndNothingIsModifiedAsync`                      | Fact | Verb tampering only get is served and nothing is modified.                        |

### PenTestExhaustiveSearchInstancePromotedTrialInstancePredictedActual

`Jube.Test.Security.QueryExhaustive.PenTestExhaustiveSearchInstancePromotedTrialInstancePredictedActual` — 12 tests.
Category: PenTest.

| Test                                                                          | Kind | Verifies                                                                          |
|-------------------------------------------------------------------------------|------|-----------------------------------------------------------------------------------|
| `Rows_AreTheLiveOnesAndErrorIsActualMinusPredictedAsync`                      | Fact | Rows are the live ones and error is actual minus predicted.                       |
| `AuthorisedUser_ReadsOwnDataAsWellFormedJsonAsync`                            | Fact | Authorised user reads own data as well formed json.                               |
| `AuthenticationAndPermissionMatrix_RefusesEveryoneButAuthorisedUsersAsync`    | Fact | Authentication and permission matrix refuses everyone but authorised users.       |
| `CrossTenant_ForeignIdLooksExactlyLikeANonExistentIdAsync`                    | Fact | Cross tenant foreign id looks exactly like a non existent id.                     |
| `IdEnumeration_NeighbouringIdsNeverLeakAcrossTenantsAndStayUniformAsync`      | Fact | Id enumeration neighbouring ids never leak across tenants and stay uniform.       |
| `SoftDeleted_RowsAndInstancesAreNeverReturnedAsync`                           | Fact | Soft deleted rows and instances are never returned.                               |
| `EmptyResult_IsAWellFormedEmptyDocumentAsync`                                 | Fact | Empty result is a well formed empty document.                                     |
| `NumericEdgeCases_NaNInfinityZeroDenominatorAndOverflowSerialiseValidlyAsync` | Fact | Numeric edge cases na n infinity zero denominator and overflow serialise validly. |
| `ConcurrentReads_AreDeterministicAndBoundedAsync`                             | Fact | Concurrent reads are deterministic and bounded.                                   |
| `IdBoundariesAndTypeConfusion_AreRejectedOrEmptyNeverServerErrorsAsync`       | Fact | Id boundaries and type confusion are rejected or empty never server errors.       |
| `HostileQueryStringAndHeaders_NeverChangeTheResultAsync`                      | Fact | Hostile query string and headers never change the result.                         |
| `VerbTampering_OnlyGetIsServedAndNothingIsModifiedAsync`                      | Fact | Verb tampering only get is served and nothing is modified.                        |

### PenTestExhaustiveSearchInstancePromotedTrialInstanceQuery

`Jube.Test.Security.QueryExhaustive.PenTestExhaustiveSearchInstancePromotedTrialInstanceQuery` — 12 tests. Category:
PenTest.

| Test                                                                          | Kind | Verifies                                                                          |
|-------------------------------------------------------------------------------|------|-----------------------------------------------------------------------------------|
| `StoredJson_IsReturnedAsDataAndNeverOtherTenantsAsync`                        | Fact | Stored json is returned as data and never other tenants.                          |
| `AuthorisedUser_ReadsOwnDataAsWellFormedJsonAsync`                            | Fact | Authorised user reads own data as well formed json.                               |
| `AuthenticationAndPermissionMatrix_RefusesEveryoneButAuthorisedUsersAsync`    | Fact | Authentication and permission matrix refuses everyone but authorised users.       |
| `CrossTenant_ForeignIdLooksExactlyLikeANonExistentIdAsync`                    | Fact | Cross tenant foreign id looks exactly like a non existent id.                     |
| `IdEnumeration_NeighbouringIdsNeverLeakAcrossTenantsAndStayUniformAsync`      | Fact | Id enumeration neighbouring ids never leak across tenants and stay uniform.       |
| `SoftDeleted_RowsAndInstancesAreNeverReturnedAsync`                           | Fact | Soft deleted rows and instances are never returned.                               |
| `EmptyResult_IsAWellFormedEmptyDocumentAsync`                                 | Fact | Empty result is a well formed empty document.                                     |
| `NumericEdgeCases_NaNInfinityZeroDenominatorAndOverflowSerialiseValidlyAsync` | Fact | Numeric edge cases na n infinity zero denominator and overflow serialise validly. |
| `ConcurrentReads_AreDeterministicAndBoundedAsync`                             | Fact | Concurrent reads are deterministic and bounded.                                   |
| `IdBoundariesAndTypeConfusion_AreRejectedOrEmptyNeverServerErrorsAsync`       | Fact | Id boundaries and type confusion are rejected or empty never server errors.       |
| `HostileQueryStringAndHeaders_NeverChangeTheResultAsync`                      | Fact | Hostile query string and headers never change the result.                         |
| `VerbTampering_OnlyGetIsServedAndNothingIsModifiedAsync`                      | Fact | Verb tampering only get is served and nothing is modified.                        |

### PenTestExhaustiveSearchInstancePromotedTrialInstanceRoc

`Jube.Test.Security.QueryExhaustive.PenTestExhaustiveSearchInstancePromotedTrialInstanceRoc` — 12 tests. Category:
PenTest.

| Test                                                                          | Kind | Verifies                                                                          |
|-------------------------------------------------------------------------------|------|-----------------------------------------------------------------------------------|
| `RatesAreFiniteFractionsAndOrderedByIdAsync`                                  | Fact | Rates are finite fractions and ordered by id.                                     |
| `AuthorisedUser_ReadsOwnDataAsWellFormedJsonAsync`                            | Fact | Authorised user reads own data as well formed json.                               |
| `AuthenticationAndPermissionMatrix_RefusesEveryoneButAuthorisedUsersAsync`    | Fact | Authentication and permission matrix refuses everyone but authorised users.       |
| `CrossTenant_ForeignIdLooksExactlyLikeANonExistentIdAsync`                    | Fact | Cross tenant foreign id looks exactly like a non existent id.                     |
| `IdEnumeration_NeighbouringIdsNeverLeakAcrossTenantsAndStayUniformAsync`      | Fact | Id enumeration neighbouring ids never leak across tenants and stay uniform.       |
| `SoftDeleted_RowsAndInstancesAreNeverReturnedAsync`                           | Fact | Soft deleted rows and instances are never returned.                               |
| `EmptyResult_IsAWellFormedEmptyDocumentAsync`                                 | Fact | Empty result is a well formed empty document.                                     |
| `NumericEdgeCases_NaNInfinityZeroDenominatorAndOverflowSerialiseValidlyAsync` | Fact | Numeric edge cases na n infinity zero denominator and overflow serialise validly. |
| `ConcurrentReads_AreDeterministicAndBoundedAsync`                             | Fact | Concurrent reads are deterministic and bounded.                                   |
| `IdBoundariesAndTypeConfusion_AreRejectedOrEmptyNeverServerErrorsAsync`       | Fact | Id boundaries and type confusion are rejected or empty never server errors.       |
| `HostileQueryStringAndHeaders_NeverChangeTheResultAsync`                      | Fact | Hostile query string and headers never change the result.                         |
| `VerbTampering_OnlyGetIsServedAndNothingIsModifiedAsync`                      | Fact | Verb tampering only get is served and nothing is modified.                        |

### PenTestExhaustiveSearchInstancePromotedTrialInstanceVariablePrescription

`Jube.Test.Security.QueryExhaustive.PenTestExhaustiveSearchInstancePromotedTrialInstanceVariablePrescription` — 11
tests. Category: PenTest.

| Test                                                                          | Kind | Verifies                                                                          |
|-------------------------------------------------------------------------------|------|-----------------------------------------------------------------------------------|
| `AuthorisedUser_ReadsOwnDataAsWellFormedJsonAsync`                            | Fact | Authorised user reads own data as well formed json.                               |
| `AuthenticationAndPermissionMatrix_RefusesEveryoneButAuthorisedUsersAsync`    | Fact | Authentication and permission matrix refuses everyone but authorised users.       |
| `CrossTenant_ForeignIdLooksExactlyLikeANonExistentIdAsync`                    | Fact | Cross tenant foreign id looks exactly like a non existent id.                     |
| `IdEnumeration_NeighbouringIdsNeverLeakAcrossTenantsAndStayUniformAsync`      | Fact | Id enumeration neighbouring ids never leak across tenants and stay uniform.       |
| `SoftDeleted_RowsAndInstancesAreNeverReturnedAsync`                           | Fact | Soft deleted rows and instances are never returned.                               |
| `EmptyResult_IsAWellFormedEmptyDocumentAsync`                                 | Fact | Empty result is a well formed empty document.                                     |
| `NumericEdgeCases_NaNInfinityZeroDenominatorAndOverflowSerialiseValidlyAsync` | Fact | Numeric edge cases na n infinity zero denominator and overflow serialise validly. |
| `ConcurrentReads_AreDeterministicAndBoundedAsync`                             | Fact | Concurrent reads are deterministic and bounded.                                   |
| `IdBoundariesAndTypeConfusion_AreRejectedOrEmptyNeverServerErrorsAsync`       | Fact | Id boundaries and type confusion are rejected or empty never server errors.       |
| `HostileQueryStringAndHeaders_NeverChangeTheResultAsync`                      | Fact | Hostile query string and headers never change the result.                         |
| `VerbTampering_OnlyGetIsServedAndNothingIsModifiedAsync`                      | Fact | Verb tampering only get is served and nothing is modified.                        |

### PenTestExhaustiveSearchInstancePromotionHistory

`Jube.Test.Security.QueryExhaustive.PenTestExhaustiveSearchInstancePromotionHistory` — 12 tests. Category: PenTest.

| Test                                                                          | Kind | Verifies                                                                          |
|-------------------------------------------------------------------------------|------|-----------------------------------------------------------------------------------|
| `History_NeverCarriesTheSerialisedNetworkAndCountsTrialsAsync`                | Fact | History never carries the serialised network and counts trials.                   |
| `AuthorisedUser_ReadsOwnDataAsWellFormedJsonAsync`                            | Fact | Authorised user reads own data as well formed json.                               |
| `AuthenticationAndPermissionMatrix_RefusesEveryoneButAuthorisedUsersAsync`    | Fact | Authentication and permission matrix refuses everyone but authorised users.       |
| `CrossTenant_ForeignIdLooksExactlyLikeANonExistentIdAsync`                    | Fact | Cross tenant foreign id looks exactly like a non existent id.                     |
| `IdEnumeration_NeighbouringIdsNeverLeakAcrossTenantsAndStayUniformAsync`      | Fact | Id enumeration neighbouring ids never leak across tenants and stay uniform.       |
| `SoftDeleted_RowsAndInstancesAreNeverReturnedAsync`                           | Fact | Soft deleted rows and instances are never returned.                               |
| `EmptyResult_IsAWellFormedEmptyDocumentAsync`                                 | Fact | Empty result is a well formed empty document.                                     |
| `NumericEdgeCases_NaNInfinityZeroDenominatorAndOverflowSerialiseValidlyAsync` | Fact | Numeric edge cases na n infinity zero denominator and overflow serialise validly. |
| `ConcurrentReads_AreDeterministicAndBoundedAsync`                             | Fact | Concurrent reads are deterministic and bounded.                                   |
| `IdBoundariesAndTypeConfusion_AreRejectedOrEmptyNeverServerErrorsAsync`       | Fact | Id boundaries and type confusion are rejected or empty never server errors.       |
| `HostileQueryStringAndHeaders_NeverChangeTheResultAsync`                      | Fact | Hostile query string and headers never change the result.                         |
| `VerbTampering_OnlyGetIsServedAndNothingIsModifiedAsync`                      | Fact | Verb tampering only get is served and nothing is modified.                        |

### PenTestExhaustiveSearchInstanceTrialInstanceListQuery

`Jube.Test.Security.QueryExhaustive.PenTestExhaustiveSearchInstanceTrialInstanceListQuery` — 12 tests. Category:
PenTest.

| Test                                                                          | Kind | Verifies                                                                          |
|-------------------------------------------------------------------------------|------|-----------------------------------------------------------------------------------|
| `Trials_AreReturnedForTheOwningInstanceOnlyAsync`                             | Fact | Trials are returned for the owning instance only.                                 |
| `AuthorisedUser_ReadsOwnDataAsWellFormedJsonAsync`                            | Fact | Authorised user reads own data as well formed json.                               |
| `AuthenticationAndPermissionMatrix_RefusesEveryoneButAuthorisedUsersAsync`    | Fact | Authentication and permission matrix refuses everyone but authorised users.       |
| `CrossTenant_ForeignIdLooksExactlyLikeANonExistentIdAsync`                    | Fact | Cross tenant foreign id looks exactly like a non existent id.                     |
| `IdEnumeration_NeighbouringIdsNeverLeakAcrossTenantsAndStayUniformAsync`      | Fact | Id enumeration neighbouring ids never leak across tenants and stay uniform.       |
| `SoftDeleted_RowsAndInstancesAreNeverReturnedAsync`                           | Fact | Soft deleted rows and instances are never returned.                               |
| `EmptyResult_IsAWellFormedEmptyDocumentAsync`                                 | Fact | Empty result is a well formed empty document.                                     |
| `NumericEdgeCases_NaNInfinityZeroDenominatorAndOverflowSerialiseValidlyAsync` | Fact | Numeric edge cases na n infinity zero denominator and overflow serialise validly. |
| `ConcurrentReads_AreDeterministicAndBoundedAsync`                             | Fact | Concurrent reads are deterministic and bounded.                                   |
| `IdBoundariesAndTypeConfusion_AreRejectedOrEmptyNeverServerErrorsAsync`       | Fact | Id boundaries and type confusion are rejected or empty never server errors.       |
| `HostileQueryStringAndHeaders_NeverChangeTheResultAsync`                      | Fact | Hostile query string and headers never change the result.                         |
| `VerbTampering_OnlyGetIsServedAndNothingIsModifiedAsync`                      | Fact | Verb tampering only get is served and nothing is modified.                        |

### PenTestExhaustiveSearchInstanceTrialInstanceVariable

`Jube.Test.Security.QueryExhaustive.PenTestExhaustiveSearchInstanceTrialInstanceVariable` — 12 tests. Category: PenTest.

| Test                                                                          | Kind | Verifies                                                                          |
|-------------------------------------------------------------------------------|------|-----------------------------------------------------------------------------------|
| `StoredNames_RoundTripAsDataInJsonAsync`                                      | Fact | Stored names round trip as data in json.                                          |
| `AuthorisedUser_ReadsOwnDataAsWellFormedJsonAsync`                            | Fact | Authorised user reads own data as well formed json.                               |
| `AuthenticationAndPermissionMatrix_RefusesEveryoneButAuthorisedUsersAsync`    | Fact | Authentication and permission matrix refuses everyone but authorised users.       |
| `CrossTenant_ForeignIdLooksExactlyLikeANonExistentIdAsync`                    | Fact | Cross tenant foreign id looks exactly like a non existent id.                     |
| `IdEnumeration_NeighbouringIdsNeverLeakAcrossTenantsAndStayUniformAsync`      | Fact | Id enumeration neighbouring ids never leak across tenants and stay uniform.       |
| `SoftDeleted_RowsAndInstancesAreNeverReturnedAsync`                           | Fact | Soft deleted rows and instances are never returned.                               |
| `EmptyResult_IsAWellFormedEmptyDocumentAsync`                                 | Fact | Empty result is a well formed empty document.                                     |
| `NumericEdgeCases_NaNInfinityZeroDenominatorAndOverflowSerialiseValidlyAsync` | Fact | Numeric edge cases na n infinity zero denominator and overflow serialise validly. |
| `ConcurrentReads_AreDeterministicAndBoundedAsync`                             | Fact | Concurrent reads are deterministic and bounded.                                   |
| `IdBoundariesAndTypeConfusion_AreRejectedOrEmptyNeverServerErrorsAsync`       | Fact | Id boundaries and type confusion are rejected or empty never server errors.       |
| `HostileQueryStringAndHeaders_NeverChangeTheResultAsync`                      | Fact | Hostile query string and headers never change the result.                         |
| `VerbTampering_OnlyGetIsServedAndNothingIsModifiedAsync`                      | Fact | Verb tampering only get is served and nothing is modified.                        |

### PenTestExhaustiveSearchInstanceTrialInstanceVariableVariance

`Jube.Test.Security.QueryExhaustive.PenTestExhaustiveSearchInstanceTrialInstanceVariableVariance` — 11 tests. Category:
PenTest.

| Test                                                                          | Kind | Verifies                                                                          |
|-------------------------------------------------------------------------------|------|-----------------------------------------------------------------------------------|
| `AuthorisedUser_ReadsOwnDataAsWellFormedJsonAsync`                            | Fact | Authorised user reads own data as well formed json.                               |
| `AuthenticationAndPermissionMatrix_RefusesEveryoneButAuthorisedUsersAsync`    | Fact | Authentication and permission matrix refuses everyone but authorised users.       |
| `CrossTenant_ForeignIdLooksExactlyLikeANonExistentIdAsync`                    | Fact | Cross tenant foreign id looks exactly like a non existent id.                     |
| `IdEnumeration_NeighbouringIdsNeverLeakAcrossTenantsAndStayUniformAsync`      | Fact | Id enumeration neighbouring ids never leak across tenants and stay uniform.       |
| `SoftDeleted_RowsAndInstancesAreNeverReturnedAsync`                           | Fact | Soft deleted rows and instances are never returned.                               |
| `EmptyResult_IsAWellFormedEmptyDocumentAsync`                                 | Fact | Empty result is a well formed empty document.                                     |
| `NumericEdgeCases_NaNInfinityZeroDenominatorAndOverflowSerialiseValidlyAsync` | Fact | Numeric edge cases na n infinity zero denominator and overflow serialise validly. |
| `ConcurrentReads_AreDeterministicAndBoundedAsync`                             | Fact | Concurrent reads are deterministic and bounded.                                   |
| `IdBoundariesAndTypeConfusion_AreRejectedOrEmptyNeverServerErrorsAsync`       | Fact | Id boundaries and type confusion are rejected or empty never server errors.       |
| `HostileQueryStringAndHeaders_NeverChangeTheResultAsync`                      | Fact | Hostile query string and headers never change the result.                         |
| `VerbTampering_OnlyGetIsServedAndNothingIsModifiedAsync`                      | Fact | Verb tampering only get is served and nothing is modified.                        |

### PenTestExhaustiveSearchInstanceVariable

`Jube.Test.Security.QueryExhaustive.PenTestExhaustiveSearchInstanceVariable` — 12 tests. Category: PenTest.

| Test                                                                          | Kind | Verifies                                                                          |
|-------------------------------------------------------------------------------|------|-----------------------------------------------------------------------------------|
| `VariablesAndHistograms_AreTheLiveOnesOfTheOwnerAsync`                        | Fact | Variables and histograms are the live ones of the owner.                          |
| `AuthorisedUser_ReadsOwnDataAsWellFormedJsonAsync`                            | Fact | Authorised user reads own data as well formed json.                               |
| `AuthenticationAndPermissionMatrix_RefusesEveryoneButAuthorisedUsersAsync`    | Fact | Authentication and permission matrix refuses everyone but authorised users.       |
| `CrossTenant_ForeignIdLooksExactlyLikeANonExistentIdAsync`                    | Fact | Cross tenant foreign id looks exactly like a non existent id.                     |
| `IdEnumeration_NeighbouringIdsNeverLeakAcrossTenantsAndStayUniformAsync`      | Fact | Id enumeration neighbouring ids never leak across tenants and stay uniform.       |
| `SoftDeleted_RowsAndInstancesAreNeverReturnedAsync`                           | Fact | Soft deleted rows and instances are never returned.                               |
| `EmptyResult_IsAWellFormedEmptyDocumentAsync`                                 | Fact | Empty result is a well formed empty document.                                     |
| `NumericEdgeCases_NaNInfinityZeroDenominatorAndOverflowSerialiseValidlyAsync` | Fact | Numeric edge cases na n infinity zero denominator and overflow serialise validly. |
| `ConcurrentReads_AreDeterministicAndBoundedAsync`                             | Fact | Concurrent reads are deterministic and bounded.                                   |
| `IdBoundariesAndTypeConfusion_AreRejectedOrEmptyNeverServerErrorsAsync`       | Fact | Id boundaries and type confusion are rejected or empty never server errors.       |
| `HostileQueryStringAndHeaders_NeverChangeTheResultAsync`                      | Fact | Hostile query string and headers never change the result.                         |
| `VerbTampering_OnlyGetIsServedAndNothingIsModifiedAsync`                      | Fact | Verb tampering only get is served and nothing is modified.                        |

### PenTestExhaustiveStatisticsChartGrid

`Jube.Test.Security.QueryExhaustive.PenTestExhaustiveStatisticsChartGrid` — 11 tests. Category: PenTest.

| Test                                                                       | Kind | Verifies                                                                       |
|----------------------------------------------------------------------------|------|--------------------------------------------------------------------------------|
| `AuthorisedUser_ReadsOwnDataAsSafeSvgAsync`                                | Fact | Authorised user reads own data as safe svg.                                    |
| `AuthenticationAndPermissionMatrix_RefusesEveryoneButAuthorisedUsersAsync` | Fact | Authentication and permission matrix refuses everyone but authorised users.    |
| `CrossTenant_ForeignAndUnknownIdsAnswerIndistinguishablyAsync`             | Fact | Cross tenant foreign and unknown ids answer indistinguishably.                 |
| `IdEnumeration_NeighbouringIdsAreOwnDataOrTheEmptyStateNeverForeignAsync`  | Fact | Id enumeration neighbouring ids are own data or the empty state never foreign. |
| `EmptyStates_UnknownEmptyAndSoftDeletedAreByteForByteEqualAsync`           | Fact | Empty states unknown empty and soft deleted are byte for byte equal.           |
| `NumericEdgeCases_NaNInfinityAndOverflowNeverBreakTheSvgAsync`             | Fact | Numeric edge cases na n infinity and overflow never break the svg.             |
| `ConcurrentReads_AreDeterministicAndBoundedAsync`                          | Fact | Concurrent reads are deterministic and bounded.                                |
| `IdBoundariesAndTypeConfusion_AreRejectedOrEmptyNeverServerErrorsAsync`    | Fact | Id boundaries and type confusion are rejected or empty never server errors.    |
| `HostileQueryStringAndHeaders_NeverChangeTheChartAsync`                    | Fact | Hostile query string and headers never change the chart.                       |
| `VerbTampering_OnlyGetIsServedAsync`                                       | Fact | Verb tampering only get is served.                                             |
| `HostileVariableNames_AreEscapedAndNeverBecomeMarkupAsync`                 | Fact | Hostile variable names are escaped and never become markup.                    |

### PenTestExhaustiveStatisticsChartMonteCarlo

`Jube.Test.Security.QueryExhaustive.PenTestExhaustiveStatisticsChartMonteCarlo` — 12 tests. Category: PenTest.

| Test                                                                       | Kind | Verifies                                                                       |
|----------------------------------------------------------------------------|------|--------------------------------------------------------------------------------|
| `VariableOfAnotherInstanceOrTenant_IsTheEmptyStateAsync`                   | Fact | Variable of another instance or tenant is the empty state.                     |
| `AuthorisedUser_ReadsOwnDataAsSafeSvgAsync`                                | Fact | Authorised user reads own data as safe svg.                                    |
| `AuthenticationAndPermissionMatrix_RefusesEveryoneButAuthorisedUsersAsync` | Fact | Authentication and permission matrix refuses everyone but authorised users.    |
| `CrossTenant_ForeignAndUnknownIdsAnswerIndistinguishablyAsync`             | Fact | Cross tenant foreign and unknown ids answer indistinguishably.                 |
| `IdEnumeration_NeighbouringIdsAreOwnDataOrTheEmptyStateNeverForeignAsync`  | Fact | Id enumeration neighbouring ids are own data or the empty state never foreign. |
| `EmptyStates_UnknownEmptyAndSoftDeletedAreByteForByteEqualAsync`           | Fact | Empty states unknown empty and soft deleted are byte for byte equal.           |
| `NumericEdgeCases_NaNInfinityAndOverflowNeverBreakTheSvgAsync`             | Fact | Numeric edge cases na n infinity and overflow never break the svg.             |
| `ConcurrentReads_AreDeterministicAndBoundedAsync`                          | Fact | Concurrent reads are deterministic and bounded.                                |
| `IdBoundariesAndTypeConfusion_AreRejectedOrEmptyNeverServerErrorsAsync`    | Fact | Id boundaries and type confusion are rejected or empty never server errors.    |
| `HostileQueryStringAndHeaders_NeverChangeTheChartAsync`                    | Fact | Hostile query string and headers never change the chart.                       |
| `VerbTampering_OnlyGetIsServedAsync`                                       | Fact | Verb tampering only get is served.                                             |
| `HostileVariableNames_AreEscapedAndNeverBecomeMarkupAsync`                 | Fact | Hostile variable names are escaped and never become markup.                    |

### PenTestExhaustiveStatisticsChartMonteCarloSummary

`Jube.Test.Security.QueryExhaustive.PenTestExhaustiveStatisticsChartMonteCarloSummary` — 12 tests. Category: PenTest.

| Test                                                                       | Kind | Verifies                                                                       |
|----------------------------------------------------------------------------|------|--------------------------------------------------------------------------------|
| `VariableOfAnotherInstanceOrTenant_IsTheEmptyStateAsync`                   | Fact | Variable of another instance or tenant is the empty state.                     |
| `AuthorisedUser_ReadsOwnDataAsSafeSvgAsync`                                | Fact | Authorised user reads own data as safe svg.                                    |
| `AuthenticationAndPermissionMatrix_RefusesEveryoneButAuthorisedUsersAsync` | Fact | Authentication and permission matrix refuses everyone but authorised users.    |
| `CrossTenant_ForeignAndUnknownIdsAnswerIndistinguishablyAsync`             | Fact | Cross tenant foreign and unknown ids answer indistinguishably.                 |
| `IdEnumeration_NeighbouringIdsAreOwnDataOrTheEmptyStateNeverForeignAsync`  | Fact | Id enumeration neighbouring ids are own data or the empty state never foreign. |
| `EmptyStates_UnknownEmptyAndSoftDeletedAreByteForByteEqualAsync`           | Fact | Empty states unknown empty and soft deleted are byte for byte equal.           |
| `NumericEdgeCases_NaNInfinityAndOverflowNeverBreakTheSvgAsync`             | Fact | Numeric edge cases na n infinity and overflow never break the svg.             |
| `ConcurrentReads_AreDeterministicAndBoundedAsync`                          | Fact | Concurrent reads are deterministic and bounded.                                |
| `IdBoundariesAndTypeConfusion_AreRejectedOrEmptyNeverServerErrorsAsync`    | Fact | Id boundaries and type confusion are rejected or empty never server errors.    |
| `HostileQueryStringAndHeaders_NeverChangeTheChartAsync`                    | Fact | Hostile query string and headers never change the chart.                       |
| `VerbTampering_OnlyGetIsServedAsync`                                       | Fact | Verb tampering only get is served.                                             |
| `HostileVariableNames_AreEscapedAndNeverBecomeMarkupAsync`                 | Fact | Hostile variable names are escaped and never become markup.                    |

### PenTestExhaustiveStatisticsChartVariableClasses

`Jube.Test.Security.QueryExhaustive.PenTestExhaustiveStatisticsChartVariableClasses` — 12 tests. Category: PenTest.

| Test                                                                       | Kind | Verifies                                                                       |
|----------------------------------------------------------------------------|------|--------------------------------------------------------------------------------|
| `VariableOfAnotherInstanceOrTenant_IsTheEmptyStateAsync`                   | Fact | Variable of another instance or tenant is the empty state.                     |
| `AuthorisedUser_ReadsOwnDataAsSafeSvgAsync`                                | Fact | Authorised user reads own data as safe svg.                                    |
| `AuthenticationAndPermissionMatrix_RefusesEveryoneButAuthorisedUsersAsync` | Fact | Authentication and permission matrix refuses everyone but authorised users.    |
| `CrossTenant_ForeignAndUnknownIdsAnswerIndistinguishablyAsync`             | Fact | Cross tenant foreign and unknown ids answer indistinguishably.                 |
| `IdEnumeration_NeighbouringIdsAreOwnDataOrTheEmptyStateNeverForeignAsync`  | Fact | Id enumeration neighbouring ids are own data or the empty state never foreign. |
| `EmptyStates_UnknownEmptyAndSoftDeletedAreByteForByteEqualAsync`           | Fact | Empty states unknown empty and soft deleted are byte for byte equal.           |
| `NumericEdgeCases_NaNInfinityAndOverflowNeverBreakTheSvgAsync`             | Fact | Numeric edge cases na n infinity and overflow never break the svg.             |
| `ConcurrentReads_AreDeterministicAndBoundedAsync`                          | Fact | Concurrent reads are deterministic and bounded.                                |
| `IdBoundariesAndTypeConfusion_AreRejectedOrEmptyNeverServerErrorsAsync`    | Fact | Id boundaries and type confusion are rejected or empty never server errors.    |
| `HostileQueryStringAndHeaders_NeverChangeTheChartAsync`                    | Fact | Hostile query string and headers never change the chart.                       |
| `VerbTampering_OnlyGetIsServedAsync`                                       | Fact | Verb tampering only get is served.                                             |
| `HostileVariableNames_AreEscapedAndNeverBecomeMarkupAsync`                 | Fact | Hostile variable names are escaped and never become markup.                    |

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
Database.

| Test                                                                                | Kind | Verifies                                                                                |
|-------------------------------------------------------------------------------------|------|-----------------------------------------------------------------------------------------|
| `AuthenticationMatrix_EveryRoute_RefusesUnauthorisedCallersAsync`                   | Fact | Authentication matrix every route refuses unauthorised callers.                         |
| `GrantingAForeignTenantsRoleOrParent_IsRefusedAndCreatesNothingAsync`               | Fact | Granting a foreign tenants role or parent is refused and creates nothing.               |
| `ListAndDelete_AreScopedToTheCallersTenantAsync`                                    | Fact | List and delete are scoped to the callers tenant.                                       |
| `MassAssignment_ForbiddenFieldsAreNotHonouredAsync`                                 | Fact | Mass assignment forbidden fields are not honoured.                                      |
| `InjectionAndTypeConfusion_NeverServerErrorAndCreateNothingAsync`                   | Fact | Injection and type confusion never server error and create nothing.                     |
| `ReplayAndConcurrency_NeverServerErrorAsync`                                        | Fact | Replay and concurrency never server error.                                              |
| `DuplicateGrants_AreIdempotent_NeverAServerError_AndOneRevokeRemovesTheAccessAsync` | Fact | Duplicate grants are idempotent never a server error and one revoke removes the access. |
| `ResourceConsumption_OversizedBodiesAreRejectedAsync`                               | Fact | Resource consumption oversized bodies are rejected.                                     |

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
Database.

| Test                                                                                | Kind | Verifies                                                                                |
|-------------------------------------------------------------------------------------|------|-----------------------------------------------------------------------------------------|
| `AuthenticationMatrix_EveryRoute_RefusesUnauthorisedCallersAsync`                   | Fact | Authentication matrix every route refuses unauthorised callers.                         |
| `GrantingAForeignTenantsRoleOrParent_IsRefusedAndCreatesNothingAsync`               | Fact | Granting a foreign tenants role or parent is refused and creates nothing.               |
| `ListAndDelete_AreScopedToTheCallersTenantAsync`                                    | Fact | List and delete are scoped to the callers tenant.                                       |
| `MassAssignment_ForbiddenFieldsAreNotHonouredAsync`                                 | Fact | Mass assignment forbidden fields are not honoured.                                      |
| `InjectionAndTypeConfusion_NeverServerErrorAndCreateNothingAsync`                   | Fact | Injection and type confusion never server error and create nothing.                     |
| `ReplayAndConcurrency_NeverServerErrorAsync`                                        | Fact | Replay and concurrency never server error.                                              |
| `DuplicateGrants_AreIdempotent_NeverAServerError_AndOneRevokeRemovesTheAccessAsync` | Fact | Duplicate grants are idempotent never a server error and one revoke removes the access. |
| `ResourceConsumption_OversizedBodiesAreRejectedAsync`                               | Fact | Resource consumption oversized bodies are rejected.                                     |

### PenTestVisualisationRegistryRole

`Jube.Test.Security.Visualisation.PenTestVisualisationRegistryRole` — 8 tests. Category: PenTest. Collection: Database.

| Test                                                                                | Kind | Verifies                                                                                |
|-------------------------------------------------------------------------------------|------|-----------------------------------------------------------------------------------------|
| `AuthenticationMatrix_EveryRoute_RefusesUnauthorisedCallersAsync`                   | Fact | Authentication matrix every route refuses unauthorised callers.                         |
| `GrantingAForeignTenantsRoleOrParent_IsRefusedAndCreatesNothingAsync`               | Fact | Granting a foreign tenants role or parent is refused and creates nothing.               |
| `ListAndDelete_AreScopedToTheCallersTenantAsync`                                    | Fact | List and delete are scoped to the callers tenant.                                       |
| `MassAssignment_ForbiddenFieldsAreNotHonouredAsync`                                 | Fact | Mass assignment forbidden fields are not honoured.                                      |
| `InjectionAndTypeConfusion_NeverServerErrorAndCreateNothingAsync`                   | Fact | Injection and type confusion never server error and create nothing.                     |
| `ReplayAndConcurrency_NeverServerErrorAsync`                                        | Fact | Replay and concurrency never server error.                                              |
| `DuplicateGrants_AreIdempotent_NeverAServerError_AndOneRevokeRemovesTheAccessAsync` | Fact | Duplicate grants are idempotent never a server error and one revoke removes the access. |
| `ResourceConsumption_OversizedBodiesAreRejectedAsync`                               | Fact | Resource consumption oversized bodies are rejected.                                     |
