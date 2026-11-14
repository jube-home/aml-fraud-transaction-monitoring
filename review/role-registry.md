# Slice review: role-registry

Agent-mode review, 2026-11-08. Steps 0, 2, 3, 4, 5. Nothing was built and no .NET or DB-backed test was run. Vitest,
prettier, eslint and `vue-tsc --build` were run on the slice's Vue files and are clean.

## 1. Summary and verdict

Verdict: READY AFTER OWNER DECISIONS. The back end of this slice is well covered (service tests 94 by script count
across the three areas, PenTest 24 across three files) and no cross-tenant, IDOR or mass-assignment hole was found. The
security questions the brief asked about have these answers:

1. **Privilege escalation: yes, by design, undocumented.** `RoleRegistryPermissionService.InsertAsync` (line 448) and
   the validator check only that the role is in the caller's tenant and the specification exists. Nothing requires the
   granter to hold the permission granted, so a user holding only permission 36 can grant 34, 35 and 36 (and anything
   else) to the Role they hold. Separation of duties does exist (34 cannot grant, 36 cannot create roles or users:
   `PenTestRoleRegistryPermission.OnlyPermission34_CannotGrantPermissions_AndOnlyPermission36_CannotManageUsersAsync`)
   but 36 is effectively "administrator". Left as an owner decision, documented in the Permissions page. The same
   applies to 35 (user-registry owns that).
2. **Role-active semantics: the non-landlord path is correct, the landlord path has the shell-auth bug and role
   allocation inherits it.** `Jube.Data/Security/PermissionValidation.cs` non-landlord query requires `ur.Active`,
   `rr.Active`, `rrp.Active`, and not-deleted for role and grant, and `IsActiveUserAsync` requires `rr.Active`. The
   landlord branch (`IsLandlordAsync`, and the select-all-from-`PermissionSpecification` query) joins `RoleRegistry`
   only for tenant lookup and never reads `rr.Active`, so a landlord user with a deactivated Role still holds every
   permission in every service check, not just `/api/Me`. This is `review/shell-auth.md` finding 12; it was not fixed
   here because the file is shell-auth's and the change is a security behaviour change. Shared TODO raised. The new
   `RoleAllocationEffectTests` pin the non-landlord behaviour and intentionally do not assert the landlord one.
3. **IDOR across tenants: closed.** Every repository method filters `TenantRegistryId` (role) or
   `RoleRegistry.TenantRegistryId` (grant). Foreign and non-existent ids are indistinguishable
   (`PenTestRoleRegistryPermission.CrossTenant_ReadUpdateDeleteAndGrant_BehaveLikeNotFoundAsync`,
   `PenTestRoleRegistry.CrossTenant_ReadUpdateAndDelete_BehaveLikeNotFoundAsync`), including moving a grant into a
   foreign role.
4. **Mass assignment: closed** (`MassAssignment_OnCreateAndUpdate_IsIgnoredAsync` in both PenTests; mappers copy only
   Name/Active/Locked or Role/Spec/Active/Locked; repositories stamp tenant, guid, creator and version).
5. **RoleAllocation is for rules, not users: verified.** `[FormRoles]` appears on the DTOs of Entity Analysis Model,
   Case Workflow and its Status, Action, Form, Filter, Display, Macro and XPath, Visualisation, its Datasource and
   Parameter, and Graph (`Jube.Dto` grep). `RoleAllocation.vue` is mounted by `CrudPanel.vue:550` from the area's form
   model. A User holds one role through `UserRegistryDto.RoleRegistryGuid` (`FormKeys Parent = RoleRegistryGuid`). Each
   `*RoleDtoValidator` I listed (grep) carries a `RoleRegistryGuidNotFound` rule, which is the tenant check for an
   allocation; those belong to their own slices and were not read in depth.

Other findings are listed in section 3. Before commit: owner decisions in section 3, owner runs the manual test (section
9), someone with a test database runs the tests in section 5.

## 2. Files reviewed (first-touch pull-ins)

`--closure role-registry`: none, self-contained. `--orphans` not run for the whole tree; unclaimed files this slice owns
are listed in the shared TODO (RolePermissionsCard.vue, PermissionsBeat.vue).

| File                                                                                                                                 | Read                                                    | Notes                                                                                         |
|--------------------------------------------------------------------------------------------------------------------------------------|---------------------------------------------------------|-----------------------------------------------------------------------------------------------|
| Jube.App/Endpoints/Repository/RoleRegistryPermissionEndpoints.cs                                                                     | lines 1-140                                             | `RequireAuthorization` on the group; GET list and byId read, rest matches the sibling pattern |
| Jube.App/Endpoints/Repository/PermissionSpecificationEndpoints.cs                                                                    | whole                                                   | GET only                                                                                      |
| Jube.App/Endpoints/Repository/RoleRegistryEndpoints.cs                                                                               | not read (405 lines, same shape as the permission file) | see Not covered                                                                               |
| Jube.Service/Repository/RoleRegistryPermission/RoleRegistryPermissionService.cs                                                      | whole                                                   | permission `[36]`, tenant from `UserInTenant`, validator, audit via `OperationScope`          |
| Jube.Service/Repository/RoleRegistry/RoleRegistryService.cs                                                                          | lines 34-50, 440-711                                    | permission `[34]`, list set `[34,35,36,6,18-25,31-33]`                                        |
| Jube.Service/Repository/PermissionSpecification/PermissionSpecificationService.cs                                                    | lines 17-103                                            | permission `[36]`                                                                             |
| Jube.Service/Repository/*/*Mapper.cs (role, grant)                                                                                   | whole                                                   |                                                                                               |
| Jube.Data/Repository/RoleRegistryRepository.cs, RoleRegistryPermissionRepository.cs, PermissionSpecificationRepository.cs            | whole                                                   | retired specs 27 and 41 hidden                                                                |
| Jube.Data/Security/PermissionValidation.cs, Jube.Service/Security/PermissionValidation.cs                                            | whole                                                   | inherited from shell-auth, read for role-active semantics only                                |
| Jube.Data/Concurrency/OptimisticConcurrency.cs                                                                                       | whole                                                   | version 0 skips the check                                                                     |
| Jube.Validations/Repository/RoleRegistry and RoleRegistryPermission validators                                                       | whole                                                   |                                                                                               |
| Jube.Dto/Repository/RoleRegistry, RoleRegistryPermission, PermissionSpecification DTOs                                               | whole                                                   |                                                                                               |
| Jube.Resources RoleRegistry, RoleRegistryPermission, PermissionSpecification Resources (resx, fr.resx, cs)                           | whole                                                   |                                                                                               |
| Jube.App/Vue/src/roleAllocation (api, plan, tests), permissions (api, areas, plan, tests)                                                | whole                                                   |                                                                                               |
| Jube.App/Vue/src/components/RoleAllocation.vue, PermissionAllocation.vue, TransferList.vue, PermissionsBeat.vue, RolePermissionsCard.vue | whole                                                   | PermissionsBeat and RolePermissionsCard are unclaimed; reviewed here                          |
| Jube.App/Vue/e2e/roleAllocation.spec.ts, permissionAllocation.spec.ts                                                                    | structure, stubs, test list                             | not run                                                                                       |
| Jube.App/Vue/src/pages/tools/RoleAllocationBench.vue                                                                                     | whole                                                   | tools-bench slice; the e2e hosts                                                              |
| Jube.App/Vue/src/security/roots.ts                                                                                                       | whole                                                   | inherited from login-audit/user-registry                                                      |
| Jube.Tests PenTestRoleRegistryPermission                                                                                             | whole to line 340                                       |                                                                                               |
| Jube.Tests PenTestRoleRegistry                                                                                                       | lines 55-160 and test names                             |                                                                                               |
| Jube.Tests PenTestPermissionSpecification, PermissionParityTests, DynamicSqlTablePermissionParityTests                               | test names or script counts only                        |                                                                                               |
| Jube.Tests Service RoleRegistry, RoleRegistryPermission, PermissionSpecification                                                     | test names; RoleRegistry lines 1-160 and 262-345        |                                                                                               |

AdminSecurityPage.vue was not edited (user-registry owns it). The Roles and Permissions cards there use `roots.ts`
(`securityRoles = 34`, `securityPermissions = 36`), which agrees with the services.

## 3. Findings

| #  | Severity                   | Where                                                                                          | What                                                                                                                                                                                                                                                                                                                                                                                                                                                 | Status                                                                                   |
|----|----------------------------|------------------------------------------------------------------------------------------------|------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|------------------------------------------------------------------------------------------|
| 1  | Medium (design, security)  | RoleRegistryPermissionService.cs:448 InsertAsync, RoleRegistryPermissionDtoValidator.cs        | No rule that the granter holds what they grant; a holder of only 36 can give any role of the tenant (their own included) any permission, including 34, 35, 36.                                                                                                                                                                                                                                                                                       | Left, owner decision. Documented (Permissions page). Shared TODO.                        |
| 2  | Medium (defect, inherited) | Jube.Data/Security/PermissionValidation.cs `IsLandlordAsync` and the landlord permission query | Landlord ignores `RoleRegistry.Active`: a deactivated Role of a landlord user revokes nothing anywhere (same as review/shell-auth.md finding 12). Also, `RoleRegistryPermission` grants and `Active` are irrelevant for landlords, so the Permissions page is misleading for landlord roles.                                                                                                                                                         | Left (shell-auth file, behaviour change). Shared TODO. Documented.                       |
| 3  | Medium (consistency)       | RoleRegistryPermissionRepository.cs:98 UpdateAsync                                             | No `OptimisticConcurrency.Verify` (RoleRegistryRepository has it). A stale PUT silently overwrites; the service's `catch (VersionConflictException)` can never fire. Existing tests send arbitrary versions and expect success (PenTest `MassAssignment_OnCreateAndUpdate_IsIgnoredAsync` sends version 800 and expects `IsSuccess`; `LockedPermission_CannotBeChangedOrDeleted...` sends no version), so adding the check changes tested behaviour. | Left, owner decision. Shared TODO.                                                       |
| 4  | Low-Medium (semantics)     | RoleRegistryPermissionRepository.cs:47 `ExistsRoleRegistryAsync`                               | Ignores the parent Role's `Locked`: grants of a locked Role can be added, changed and removed. The DTO says "Role is locked to read-only and cannot be edited". Also `GetAsync` (line 61) lists grants of soft-deleted Roles.                                                                                                                                                                                                                        | Left, owner decision. Documented. Shared TODO.                                           |
| 5  | Low (test gap)             | Jube.Tests Service RoleRegistry                                                                | No service test for the stale-version conflict that `RoleRegistryRepository.UpdateAsync` enforces (SKILL requires a concurrency conflict).                                                                                                                                                                                                                                                                                                           | Fixed, 1 test added, unrun.                                                              |
| 6  | Low (test gap)             | Jube.Tests                                                                                     | Nothing tests that Role.Active, grant Active, Role Deleted or grant Deleted actually remove a permission (the permission-resolution query); `CR06_*` covers user states only. `GrantedPermission_TakesEffectOnlyForTheGrantedRolesUsersAsync` covers grant and delete by endpoint but not the flags.                                                                                                                                                 | Fixed, `RoleAllocationEffectTests` (8 cases), unrun.                                     |
| 7  | Low (test gap, front end)  | src/permissions/api.ts, src/roleAllocation/api.ts                                              | No vitest for `applyChanges`, `applyRoleChanges`, `permissionApi.grants`; the e2e only checks the differences sent through the `/tools` benches. No refusal case in either e2e spec.                                                                                                                                                                                                                                                                 | Fixed, 2 vitest files (9 tests, run, pass). Refusal journey not added (see Not covered). |
| 8  | Low (coupling)             | e2e/roleAllocation.spec.ts:56, permissionAllocation.spec.ts:84                                 | Both specs reach the components through `/tools?tool=...` benches (tools-bench decision slice). If the benches are gated behind DEV or removed, the only slice e2e coverage of the allocation components disappears; the real hosts are `CrudPanel` (roles card) and `AdminSecurityPage`.                                                                                                                                                            | Left. Shared TODO.                                                                       |
| 9  | Low (UX, security)         | PermissionAllocation.vue                                                                       | The Role selector lists inactive roles with no marker (api.ts has `active` and never shows it); an administrator can grant permissions to a Role that grants nothing. Nothing warns when removing 34 or 36 from the Role the administrator holds themselves.                                                                                                                                                                                         | Left (needs locale keys). Shared TODO.                                                   |
| 10 | Low (perf)                 | permissions/api.ts `grants`                                                                    | Fetches every grant of the tenant and filters by `roleRegistryId` in the browser (there is a server `GetByRoleRegistryId` query in `Jube.Data/Query/GetRoleRegistryPermissionByRoleRegistryIdQuery`, not exposed). Not a leak (tenant scoped). `RolePermissionsCard` repeats it per role.                                                                                                                                                            | Left, info.                                                                              |
| 11 | Low (partial failure)      | permissions/api.ts `applyChanges`, roleAllocation/api.ts `applyRoleChanges`                    | Requests go out in one `Promise.all`; on a refusal the rest may still succeed. The components reload the grants on error (`onError` invalidates), so the display is right.                                                                                                                                                                                                                                                                           | Left, documented.                                                                        |
| 12 | Low (a11y)                 | TransferList.vue                                                                               | `id="transfer-available-title"` and `transfer-assigned-title` are static, duplicated if two lists are on one page. Labels use `truncate` with no `title`, long Georgian or Swedish role names are cut.                                                                                                                                                                                                                                               | Left, info.                                                                              |
| 13 | Info (inventory)           | slices.json                                                                                    | `permissions: [35]` is wrong (Roles 34, Permissions 36); RolePermissionsCard.vue and PermissionsBeat.vue are unclaimed; backendKeywords lacks RoleRegistryPermission and PermissionValidation.                                                                                                                                                                                                                                                       | Left, shared TODO.                                                                       |
| 14 | Info (locale)              | FormModelResources                                                                             | 19 form-model keys per locale are baselined untranslated for this slice, and `[ListColumn Title]` values and `FormChild Label = "Users"` are hard-coded English.                                                                                                                                                                                                                                                                                     | Shared TODO.                                                                             |
| 15 | Info (fact check)          | memory "RoleAllocation is for rules, not users"                                                | Confirmed against code (section 1 item 5).                                                                                                                                                                                                                                                                                                                                                                                                           | None.                                                                                    |

RTL audit of the slice's Vue files: grep for physical
`ml/mr/pl/pr/left/right/text-left/text-right/border-l/border-r/rounded-l/rounded-r/translate-x/uppercase` returns
nothing in RoleAllocation, PermissionAllocation, TransferList, PermissionsBeat. The four transfer chevrons carry
`rtl:rotate-180`, the grid uses `grid-cols`, no flip bug found. Permission ids render in `font-mono` badges next to
Arabic or Urdu names without `dir`/`bdi`; numbers are neutral so low risk. `Lock` icon is symmetric. No code comments
were added or touched.

## 4. Fixes made

1. `Jube.Tests/Service/Repository/RoleRegistry/RoleRegistryServiceTests.cs`: added `using Jube.Data.Concurrency;` and
   `UpdateWithAStaleVersionIsRefusedAndAZeroVersionSkipsTheCheckAsync`, modelled on
   `CaseSearchFilterServiceTests.UpdateWithAStaleVersionIsRefusedAndAZeroVersionSkipsTheCheckAsync`. Unrun, not
   compiled.
2. `Jube.Tests/Service/Authentication/RoleAllocationEffectTests.cs` (new, sibling of `PermissionValidationTenantTests`,
   derives from `AuthenticationTestBase`): 8 cases (baseline; theory over role inactive, role deleted, grant inactive,
   grant deleted; toggle; locked does not revoke; own role only). Overrides `DisposeAsync` to delete grants before the
   base removes the roles. Unrun, not compiled; `UserRegistry.RoleRegistryGuid`, `RoleRegistryPermission` columns and
   `Permissions` (List<int>) were checked against the Poco and the DTO.
3. `Jube.App/Vue/src/permissions/__tests__/api.spec.ts` (5 tests) and `Jube.App/Vue/src/roleAllocation/__tests__/api.spec.ts` (4
   tests): run, 24 of 24 slice vitest tests pass (15 existing plus 9 new).
4. Docs: `docs/Configuration/Administration/Roles/index.md` and `Permissions/index.md` rewritten;
   `docs/Testing/ServiceAuthenticationAndUsers.md` (RoleAllocationEffectTests section) and
   `docs/Testing/ServiceRepositories.md` (RoleRegistryServiceTests 36 to 37, one row).
5. Created 21 per-class resx files (section 6). `.fr.resx` untouched.

## 5. Test and pen-test matrix

| Layer                                                                        | Required                                                                              | Evidence read                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                     | Status                                                       |
|------------------------------------------------------------------------------|---------------------------------------------------------------------------------------|---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|--------------------------------------------------------------|
| Service, RoleRegistry CRUD                                                   | create, update, delete, list, get                                                     | `InsertPersistsAndDropsIdentityFieldsAsync`, `UpdateIncrementsVersion...`, `DeleteRemovesRowAndSubsequentGetReturnsNullAsync`, `DeleteSetsDeletedUserAsync`, `ListAsyncClampsTakeAndPaginatesAsync`, `ListIsScopedToCallersTenantAsync`                                                                                                                                                                                                                                                                                                           | Covered                                                      |
| Service, RoleRegistry validators                                             | one per rule                                                                          | blank, null, over-long (256), duplicate in tenant, duplicate across tenants succeeds                                                                                                                                                                                                                                                                                                                                                                                                                                                              | Covered                                                      |
| Service, RoleRegistry permission refusal per verb                            |                                                                                       | `GetAsyncThrowsForbidden...`, `InsertWithoutPermission...` (writes no row), `UpdateWithoutPermission...`, `DeleteWithoutPermission...`, `ListAsync...Forbidden`                                                                                                                                                                                                                                                                                                                                                                                   | Covered                                                      |
| Service, RoleRegistry tenant isolation, locked, import id                    |                                                                                       | `UpdateForRowInAnotherTenantThrowsNotFoundAsync`, `UpdateOfLockedRowThrowsNotFoundAsync`, `UpdatePreservesImportIdAsync`                                                                                                                                                                                                                                                                                                                                                                                                                          | Covered                                                      |
| Service, RoleRegistry audit, change event, span                              |                                                                                       | `InsertWritesOneAuditRecordOnSuccessAsync`, `ListWritesOneAuditRecordOnSuccessAsync`, `Insert/Update/DeletePublishesChangeEventAsync`, `ListDoesNotPublish...`, `InsertWarnsNotErrors...`, `CatalogueRegistersUniqueRoleRegistryToolNames`                                                                                                                                                                                                                                                                                                        | Covered                                                      |
| Service, RoleRegistry concurrency conflict                                   |                                                                                       | none before                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                       | Added (finding 5), unrun                                     |
| Service, RoleRegistryPermission CRUD, refusals, tenant, locked, audit, event |                                                                                       | 33 tests, names read: `Insert...`, `UpdateTamperingIdentityAndAuditFieldsHasNoEffectAsync`, `UpdateOfLockedRowThrowsNotFoundAsync`, `InsertForRoleRegistryInAnotherTenant...`, `UpdateForRowInAnotherTenant...`, duplicate grant, zero and unknown ids, `InsertWritesOneAuditRecordOnSuccessAsync`, `Insert/DeletePublishesChangeEvent...`                                                                                                                                                                                                        | Covered                                                      |
| Service, RoleRegistryPermission concurrency                                  |                                                                                       | none; and the code has no check (finding 3)                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                       | Left, owner decision                                         |
| Service, PermissionSpecification                                             | list, clamp, paging, forbidden, landlord, retired rows hidden, not tenant scoped      | 25 tests, names read                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                              | Covered                                                      |
| Service, permission resolution (role and grant flags)                        |                                                                                       | `RoleAllocationEffectTests`                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                       | Added (finding 6), unrun                                     |
| PenTest, RoleRegistry                                                        | 401/403 per verb, IDOR, mass assignment, hostile strings, body confusion, concurrency | `Matrix_IsEnforcedPerPermissionAsync` (GET list uses the 15-permission set), `CrossTenant_ReadUpdateAndDelete_BehaveLikeNotFoundAsync`, `MassAssignment_OnCreateAndUpdate_IsIgnoredAsync` (includes nested `roleRegistryPermission` and `__proto__`), `LockedRole_CannotBeUpdatedOrDeleted...`, `DuplicateName_...`, `Injection_InName...`, `Injection_InRouteAndQuery_IsInertAsync`, `TypeConfusion_AndMalformedBodies_AreRejectedAsync`, `DeletingARoleInUse_DoesNotBreakTheUserRoutes...`, `ConcurrentConflictingWrites_NeverServerErrorAsync` | Covered (first three read, rest by name)                     |
| PenTest, RoleRegistryPermission                                              | same, plus separation and effect                                                      | read whole: matrix, 34-only vs 36-only separation, cross-tenant read/update/delete/grant/move, invalid and duplicate specs, mass assignment, lock lifecycle, `GrantedPermission_TakesEffectOnlyForTheGrantedRolesUsersAsync`, injection, type confusion                                                                                                                                                                                                                                                                                           | Covered                                                      |
| PenTest, PermissionSpecification                                             |                                                                                       | `Matrix_IsEnforcedPerPermissionAsync`, `Catalogue_ExposesOnlyIdAndName_...`, `WriteVerbs_AreNotSupported_...`, `Injection_InQueryString_IsInertAsync`, `OddRequestShapes_AreOrderlyAsync` (names only)                                                                                                                                                                                                                                                                                                                                            | Covered                                                      |
| PenTest, self-escalation (36 grants itself)                                  |                                                                                       | none, and the behaviour is by design (finding 1), so a test asserting it would encode an open decision; `GrantedPermission_TakesEffect...` already proves a 36 holder can grant 35                                                                                                                                                                                                                                                                                                                                                                | Not added, owner decision                                    |
| PenTest, antiforgery on mutations                                            |                                                                                       | not specific to this area; handled platform-wide (see shell-auth finding 11), not re-verified here                                                                                                                                                                                                                                                                                                                                                                                                                                                | Not examined                                                 |
| Parity                                                                       |                                                                                       | `PermissionParityTests` (8) and `DynamicSqlTablePermissionParityTests` (4) by script count only                                                                                                                                                                                                                                                                                                                                                                                                                                                   | Not examined                                                 |
| Vitest                                                                       | pure modules                                                                          | plan.spec (roleAllocation 5, permissions 10), new api.spec files (9)                                                                                                                                                                                                                                                                                                                                                                                                                                                                              | Covered, run                                                 |
| Playwright                                                                   | happy path and one refusal                                                            | roleAllocation.spec (4: split, only differences, move all and discard, axe) and permissionAllocation.spec (5: role first and groups, differences, discard, filter, axe); not run by me                                                                                                                                                                                                                                                                                                                                                            | Happy path covered, refusal missing, via benches (finding 8) |

Tests to run in the owner's serial pass: `RoleAllocationEffectTests` (Service, 8 cases),
`RoleRegistryServiceTests.UpdateWithAStaleVersionIsRefusedAndAZeroVersionSkipsTheCheckAsync` (Service), the existing
`PenTestRoleRegistry` and `PenTestRoleRegistryPermission`.

## 6. Translations

SPA (en, pl, ar): owned keys `roleAllocation` 12, `permissions` 22, `transferList` 5, 39 in all. Script audit: pl and ar
missing 0, empty 0, identical-to-English multi-word 0; `t()` keys not in en.json 0; hard-coded UI strings 0. I did not
edit the locale JSON. Proposed keys for ka, id, sv, ur, hi (all 39 each, `{count}` placeholders preserved, machine
quality) are in `review/role-registry.locale-proposals.json` (a sidecar, not the SPA files), for the integration pass.
Not rendered, so Georgian clipping in `truncate` labels, Nastaliq line height in the badges and Devanagari glyphs are
manual-test items (the font-stack TODOs are already on the shared list from other slices).

Server, closed: the slice's three per-class resource classes had `.resx` and `.fr.resx` only. Created
`.pl .ar .ka .id .sv .ur .hi` for each (21 files, written by script from the `.fr.resx` structure, XML parsed, key set
equal to the English file for all 21):

| Class                            | Keys                                                                                                                                                                                                |
|----------------------------------|-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| RoleRegistryResources            | PermissionDenied, NotAuthenticated, NotFound, NameRequired, NameMaxLength (`{0}` kept), NameAlreadyExists                                                                                           |
| RoleRegistryPermissionResources  | PermissionDenied, NotAuthenticated, NotFound, RoleRegistryIdInvalid, RoleRegistryNotFound, PermissionSpecificationIdInvalid, PermissionSpecificationNotFound, PermissionSpecificationAlreadyGranted |
| PermissionSpecificationResources | PermissionDenied, NotAuthenticated                                                                                                                                                                  |

PermissionDenied and NotAuthenticated reuse the wording of the login-audit files for consistency. Ur and ar are
right-to-left: validator messages contain no mixed Latin text. Not compiled: the csproj embeds `.resx` by default glob;
I did not check whether satellite resources for the new cultures need registration beyond what `RequestLocalisation.cs`
does (the shared TODOs from shell-auth and form-model-engine already cover culture registration).

Server, open (shared): `FormModelResources` has no translated keys for this slice's three DTOs (19 per locale are
baselined in `FormModelTranslationBaseline.txt`), `[ListColumn Title = "Permission"|"Active"|"Name"|"Id"]` and
`[FormChild Label = "Users"]` are hard-coded English. Shared TODO.

## 7. Docs written and TODO markers

`docs/Configuration/Administration/Roles/index.md` (rewritten, 16 markers) and
`docs/Configuration/Administration/Permissions/index.md` (rewritten, 11 markers). Written from the DTOs, services,
repositories and Vue components, not the running UI. Line numbers as at writing; re-grep with `grep -n "TODO(" `.

Screenshots (7, all prefixed `role-registry-`): Roles page lines 27 (`role-registry-security-story.png`), 32
(`role-registry-list.png`), 39 (`role-registry-edit.png`), 107 (`role-registry-allocation-transfer-list.png`);
Permissions page lines 40 (`role-registry-permissions-card.png`), 52 (`role-registry-permission-allocation.png`), 55
(`role-registry-permission-search.png`).

TODO (docs): Roles lines 11, 35, 49, 61, 72, 79, 86, 92, 103, 130, 143, 147; Permissions lines 11, 29, 42, 72, 90, 96,
108, 111. The old Razor screenshot references were removed from both pages; the old PNGs (`ListOfRoles.png`,
`TopOfTreeForPermissions.png` and the rest in both folders) were left on disk for the owner to delete. Not done: a table
of all permission numbers and names (generate from the catalogue and `permissions/areas.ts`; `areas.ts` has 55 mappings
and everything else appears under Other).

## 8. Shared-code TODOs raised

9 lines appended to `review/SHARED-TODO.md` under `role-registry`: slices.json corrections, landlord ignores Role.Active
(cites shell-auth finding 12), no concurrency check on grants, Locked role does not freeze grants, no rule on granting
above one's own permissions, FormModelResources and hard-coded DTO titles, SPA locale keys, inactive-role marker and
self-lockout warning, tools-bench dependency of the e2e specs.

## 9. Manual test sheet

Built from the DTOs, services and components. Three permission levels: 34 Roles, 35 Users, 36 Permissions.

1. Permissions: a user with only 34 sees the Roles card and no Permissions card (and `GET /api/RoleRegistryPermission`
   is 403); only 36 sees Permissions and no Roles card; neither sees nothing. A user with only 35 can still list Roles
   in the Role picker of a User.
2. Roles list and form: create a Role (Name required, 257 characters refused, duplicate refused case-insensitively, same
   name in another tenant allowed); edit Name and Active; Lock a Role then try to edit and delete (refused as not
   found); delete an unlocked Role and confirm Users of that Role lose their menu and API access.
3. Role Active: switch a Role with a signed-in user off, refresh that user's page, confirm every permissioned call is
   403 and the menu is empty; switch on, confirm it returns. Repeat for a landlord-tenant user and note that nothing is
   lost (finding 2).
4. Permissions card: pick a Role; counts, areas in order, permission numbers visible, search filters, one permission
   outside the known list appears under Other; toggle several, Save changes, confirm Unsaved count, then reload; Discard
   returns to stored; selector disabled while dirty.
5. Locked grant: lock a grant via the API, confirm its switch is disabled with the Locked badge; lock the Role and
   confirm grants can still be changed (finding 4, owner decision).
6. Escalation: sign in as a user with only 36, grant yourself 34, 35 and 36 on your own Role and confirm it works
   (finding 1); then remove 36 from the Role you hold and confirm you lose the page on the next request and nothing
   warns you (finding 9).
7. Cross-tenant: as tenant A send a grant naming a Role id from tenant B (400, same as an unknown id), edit a tenant B
   grant id (not found), allocate a tenant B Role guid to a Model (refused).
8. Concurrency: two tabs, edit the same Role, the second save shows the conflict; two tabs on the same Role's grants,
   the second save silently wins (finding 3).
9. Role allocation to a record: open a Model, Case Workflow Status and Visualisation, move Roles in the transfer list
   (double click, arrows, move all), Save changes sends only the differences, Discard returns; confirm a User cannot be
   allocated this way.
10. Save failure: stop the API or revoke 36 mid-session, press Save changes, confirm the toast, the reload and a
    coherent display.
11. Refresh and deep link `/administration/security`; back button; narrow width; dark theme; en, pl, ar with
    right-to-left (transfer chevrons mirror, columns order), and the new ka, id, sv, ur, hi once registered (Georgian
    and Swedish truncation in transfer lists, Nastaliq clipping in the badges and buttons, Devanagari).
12. Keyboard only through the Role select, search box, switches, transfer checkboxes and the arrows.
13. Hostile input: Role name `'; drop table`, `<script>`, 20000 characters, emoji and an RLO character; search box `%`
    and a very long string; confirm no HTML execution and no 500.

## 10. Commit pathspecs

`--paths` output from slice_audit (slices.json, unedited):

```
Jube.App/Vue/src/roleAllocation
Jube.App/Vue/src/permissions
Jube.App/Vue/src/components/RoleAllocation.vue
Jube.App/Vue/src/components/PermissionAllocation.vue
Jube.App/Vue/src/components/TransferList.vue
Jube.App/Vue/e2e/roleAllocation.spec.ts
Jube.App/Vue/e2e/permissionAllocation.spec.ts
Jube.App/Endpoints/Repository/RoleRegistryEndpoints.cs
Jube.App/Endpoints/Repository/RoleRegistryPermissionEndpoints.cs
Jube.App/Endpoints/Repository/PermissionSpecificationEndpoints.cs
docs/Configuration/Administration/Roles
docs/Configuration/Administration/Permissions
```

Hand-picked (not in `--paths`): `Jube.App/Vue/src/components/RolePermissionsCard.vue`,
`Jube.App/Vue/src/components/PermissionsBeat.vue`, `Jube.Tests/Service/Authentication/RoleAllocationEffectTests.cs`,
`Jube.Tests/Service/Repository/RoleRegistry/RoleRegistryServiceTests.cs` (the new test and using only),
`docs/Testing/ServiceAuthenticationAndUsers.md` (RoleAllocationEffectTests hunk), `docs/Testing/ServiceRepositories.md`
(RoleRegistryServiceTests hunk), the 21 files
`Jube.Resources/{RoleRegistryResources,RoleRegistryPermissionResources,PermissionSpecificationResources}.{pl,ar,ka,id,sv,ur,hi}.resx`.
`review/role-registry.locale-proposals.json` is an input for the integration pass, not a product file. The backend
services, repositories, DTOs, validators and existing tests belong to the platform and are not in my list.

## 11. Not covered

- Nothing compiled or run on the .NET side. The two new C# edits are unverified by a compiler and by Rider lint. In
  `RoleAllocationEffectTests`, `UserRegistry.RoleRegistryGuid`, `PermissionValidationDto.Permissions` (List<int>) and
  the base class members were read, but the generic FluentAssertions calls (`BeEquivalentTo(new[] { ... })`) and the
  `specs.Length == 0 ? new[] { Users } : specs` conditional were not compiled. The foreign-key order on teardown (grants
  before roles) is inferred from `RoleRegistryServiceTests.DisposeAsync`.
- Pen tests and Playwright specs were not run. `PenTestRoleRegistry` was read only from line 55 to 160 plus test names;
  `PenTestPermissionSpecification`, `PermissionParityTests` and `DynamicSqlTablePermissionParityTests` by names or
  script count.
- `RoleRegistryEndpoints.cs` and the rest of `RoleRegistryPermissionEndpoints.cs` (lines 140-405) were not read; I
  assumed the shape of the first 140 lines and the PenTest status checks.
- I did not read the `*RoleDtoValidator`, repositories or services of the other areas that host the allocation, beyond a
  grep showing a `RoleRegistryGuidNotFound` rule in each; whether each checks the tenant is inferred from
  `ExistsRoleRegistry...` names.
- Migrations for the three tables (`AddRoleRegistryTable`, `AddRoleRegistryPermissionFk`, seed of
  PermissionSpecification and the Administrator role) were not read; I could not check that the catalogue seed matches
  `permissions/areas.ts`.
- The refusal journey for Playwright was not written (needs a stub that returns 403 on save through the bench host,
  which is itself a tools-bench decision).
- The Vue files were checked with vitest, prettier, eslint and `vue-tsc --build` only. Rider lint on C# was not run; no
  new C# comments, one class per file, no partial classes by construction.
- The docs are from code, not the running UI; the menu refresh timing, the Roles card on the record pages of each area
  and the delete warning text are unverified.
- The landlord tenant's `Landlord` flag and how a tenant becomes landlord were not examined (tenant-registry slice).
