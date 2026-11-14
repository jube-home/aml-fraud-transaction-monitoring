# Reprocessing permission split (owner decision: yes, 2026-11-08)

Nothing was built, run, staged or committed. Every new test is unrun.

## 1. Summary

Permission 26 ("Read Write Reprocessing") used to gate everything on both reprocessing services: rule CRUD, trigger,
stop and delete, reads. It now gates only the rule. Two new permissions carry the run verbs. Reads accept any of the
three.

| Number            | Name (catalogue, English only)                                                         | Verbs                                                                                                                                      |
|-------------------|----------------------------------------------------------------------------------------|--------------------------------------------------------------------------------------------------------------------------------------------|
| 26 (kept)         | Read Write Reprocessing Rule (renamed by the migration from "Read Write Reprocessing") | rule Create, Update, Delete, Validate, ParseRule, Execute, BuildRuleFromBuilderJson                                                        |
| 73 (new)          | Trigger Reprocessing Run                                                               | instance Create, InsertByExistingUpdateUncompleted (trigger), Update (requeue), Validate                                                   |
| 74 (new)          | Stop or Delete Reprocessing Run                                                        | instance Delete (stops a running one)                                                                                                      |
| any of 26, 73, 74 | n/a                                                                                    | every read: rule and instance List, Get, GetBy..., ListPaged, FilterFields, Filter, Count; the model tree node; the two dynamic SQL tables |

Numbers: highest taken in migrations is 72 (Svirfd). 73 and 74 were free in every migration, service constant, test and
doc at the time of writing (`svirfdMenu.spec.ts` even uses 73 as a "does not grant Svirfd" example). The Force-override
approvals agent and the Svirfd tenant switch have migrations at 20263108610001 and 20263108620001 and took no permission
id. 52 to 59 are also unused but were left alone. Re-grep for 73 and 74 before merging if another agent adds a
permission.

## 2. Verb matrix (from the code)

| Operation                                                                                        | Service method                                                                                                             | Before | After               |
|--------------------------------------------------------------------------------------------------|----------------------------------------------------------------------------------------------------------------------------|--------|---------------------|
| Rule list, get, by model, paged, filter fields, filter, count                                    | GetAsync, GetByIdAsync, GetByEntityAnalysisModelIdAsync, ListAsync, FilterFieldsAsync, FilterAsync, CountAsync             | 26     | 26 or 73 or 74      |
| Rule create, update, delete                                                                      | InsertAsync, UpdateAsync, DeleteAsync                                                                                      | 26     | 26                  |
| Rule validate, parse, execute, build from builder JSON                                           | ValidateAsync, ParseRuleAsync, ExecuteAsync, BuildRuleFromBuilderJsonAsync                                                 | 26     | 26 (authoring aids) |
| Instance list, get, by rule, paged, filter fields, filter, count                                 | GetAsync, GetByIdAsync, GetByEntityAnalysisModelReprocessingIdAsync, ListAsync, FilterFieldsAsync, FilterAsync, CountAsync | 26     | 26 or 73 or 74      |
| Instance create, trigger, update, validate                                                       | InsertAsync, InsertByExistingUpdateUncompletedAsync, UpdateAsync, ValidateAsync                                            | 26     | 73                  |
| Instance delete (stop)                                                                           | DeleteAsync                                                                                                                | 26     | 74                  |
| Model tree Reprocessing node, SQL tables `EntityAnalysisModelReprocessingRule` and `...Instance` | TreeChildrenNodes.Reprocessing, DynamicSqlTableAccess                                                                      | 26     | 26 or 73 or 74      |

Endpoints (`EntityAnalysisModelReprocessingRule*Endpoints.cs`) are thin and call these services, so there was nothing to
change in them. Nothing in the engine reads 26.

Why 26 stays on the rule: it is named "Read Write Reprocessing" and the rule is the thing it edits.

## 3. Files changed

Code (Jube.Service): `Security/ReprocessingPermissions.cs` (new, constants and the Read set), both services (per-verb
field, `EnsurePermitted(op, required)` overload, public `EditPermissions`, `TriggerPermissions`, `StopPermissions`; the
`permissions` field is now the any-of read set so `DynamicSqlTablePermissionParityTests` still holds),
`ServiceToolCatalogue.EntityAnalysisModelReprocessingRule.cs` and `...Instance.cs` (write tools point at the verb
property), `Query/TreeChildren/TreeChildrenNodes.cs`, `DynamicSql/DynamicSqlTableAccess.cs` (two lines).
Ask Jooby: `AgentDefinitions.cs` hubs `model-admin`, `model-support`, `model-support.navigation`,
`model-support.reprocessing` now include 73 and 74 (hand-typed any-of lists; leaf lists are derived from the catalogue
and need nothing).
Migration: `Jube.Migrations/Branches/ReprocessingPermissions/SplitReprocessingPermission.cs`, number 20263108630001,
empty Down (). It renames spec 26, inserts specs 73 and 74, then per new permission runs `BuildBackfillSql`: one grant
per Role holding a non-deleted 26 grant (copies Active and Locked, so an inactive 26 stays inactive and nothing widens),
`CreatedUser = 'Administrator'`, Version 1, new Guid, `NOT EXISTS` guard so it is idempotent, then the Administrator
Role (id 1) is given both if missing. Roles are tenant scoped so every tenant is covered.
Escalation guard: no change needed. `PrivilegeEscalationGuard.MayGrant` and `MayAssignRoleAsync` work on permission ids,
not a list, so 73 and 74 go through the same subset rule (read; tests below).
SPA: `src/permissions/areaOperations.ts` (new, per-area per-operation permission map and `operationPermitted`,
`deniedOperations`), `components/CrudPanel.vue` (`can()` now also checks the user's permission; a small hint
`crud.restrictedByPermission` shows when some operations are hidden; hidden rather than disabled), `layout/menuTree.ts`
(Models menu gains 73, 74), `permissions/areas.ts` (26, 73, 74 grouped under models; 26 was under cases). The form model
is static and not per user, and the page is DTO-led, so the gate lives in the client map; the server stays the
authority.
Docs: `docs/Configuration/Reprocessing/ReprocessingInstances/index.md` (rewrote "Who may start, stop and delete a
request"), `docs/Configuration/Administration/Permissions/index.md`, `docs/AgentHierarchy/index.md` and
`tool-inventory.md` (permission source and access columns for 12 tools, 25 rows), test catalogue sections appended to
five `docs/Testing/*.md`.
Review: `review/reprocessing-permissions.locale-proposals.json` (en, pl, ar, ka, id, sv, ur, hi, fr, one key), four
lines in `review/SHARED-TODO.md`.

## 4. Tests

Existing tests changed (old versus new):

| Test or fixture                                                                                                                                                       | Old                          | New                                      | Why                                                                                                                         |
|-----------------------------------------------------------------------------------------------------------------------------------------------------------------------|------------------------------|------------------------------------------|-----------------------------------------------------------------------------------------------------------------------------|
| `DatabaseFixture.SeedAsync` `readWriteSpecs` (seeds `UserWithPermission`, which every reprocessing service and PenTest test uses)                                     | ends `69, 70`                | ends `69, 70, 73, 74`                    | the seeded all-permissions role must still hold every reprocessing verb; without it every trigger and delete test would 403 |
| `TreeChildrenServiceTests.NodeTableMatchesTheLegacyRoutesKeysAndPermissionSets` legacy table                                                                          | `("Reprocessing","id",[26])` | `[26, 73, 74]`                           | the node now admits any of the three                                                                                        |
| `DynamicSqlTablePermissionParityTests`                                                                                                                                | n/a                          | unchanged, still passes by construction  | tables and the services' `permissions` field are both the read set                                                          |
| `HierarchyAgentDefinitionsTests.EveryLeafDeclaresThePermissionsItsOwnToolsRequire`, `ToolInventoryDocumentTests.EveryInventoryRowStatesTheAccessTheCatalogueEnforces` | n/a                          | unchanged, depend on the doc edits above | inventory rows must read `26`, `73`, `74` or `26, 73, 74` exactly as the catalogue joins them                               |
| `EntityAnalysisModelReprocessingRule(Instance)ServiceTests`, `PenTestModelReprocessingRule(Instance)`                                                                 | read                         | unchanged                                | run as the seeded all-permission user; the no-permission user still holds none of the three                                 |
| `AdvancedAnalyticsPageWiringTests.TheModelsGroupIsVisible...`                                                                                                         | read                         | unchanged                                | pins the Razor layout list only (see risks)                                                                                 |

Tests added (all unrun):

- Service, `Jube.Tests/Service/ReprocessingPermissions/`: `ReprocessingRulePermissionSplitTests` (edit holder allowed on
  create, validate, update, delete; trigger-only and stop-only refused on every authoring verb with no row written; each
  of the three reads; a role with none refused on reads; declared sets), `ReprocessingInstancePermissionSplitTests`
  (trigger holder starts, validates and creates a plain instance but cannot delete; stop holder deletes but cannot
  start; edit-only cannot start, validate, update or delete; reads for each of the three and refusal for none; declared
  sets), support class `ReprocessingPermissionUsers`.
- Escalation, `Service/Security/ReprocessingPermissionEscalationTests`: an administrator with 36 and 26 cannot grant 73
  or 74 and nothing is written; with 36 and 73 can grant 73 but not 74 (and the mirror); cannot self grant; a user
  administrator without the new permission cannot put a user in a role carrying it; one holding all three can.
- PenTest, `Security/Model/PenTestModelReprocessingPermissionSplit`: a Theory over 13 endpoint verb rows (rule GET list,
  byModel, byId, POST, PUT, DELETE; instance GET list, byRule, byId, POST byExistingUpdateUncompleted, POST, PUT,
  DELETE). For each, users holding only each permission that does not own the verb, and one holding none of the three,
  must get 403; each owner is not refused. Plus a side-effect test (refused trigger starts no run, refused stop leaves
  the run) and a tree reachability test with only 73 or only 74.
- Parity, `AskJooby/ReprocessingPermissionParityTests` (Unit): numbers, every catalogue descriptor against its service
  property per verb, tree node and both SQL tables, hubs include 73 and 74.
- Migration, `Migrations/SplitReprocessingPermissionMigrationTests`: runs `BuildBackfillSql` against the test database.
  A 26 role gains exactly 73 and 74 (Administrator stamp, Version 1) and resolves to the same plus the two; no 26 gains
  nothing; a deleted 26 gains nothing; an inactive 26 stays inactive and effective permissions stay empty; idempotent
  and no duplicates over an existing 73; tenants A and B both backfilled; Administrator role holds all three; catalogue
  names.
- Vitest: `permissions/__tests__/areaOperations.spec.ts`, `layout/__tests__/reprocessingMenu.spec.ts`.

Tests to run (owner): Unit: `ReprocessingPermissionParityTests`, `DynamicSqlTablePermissionParityTests`,
`ToolInventoryDocumentTests`, `AgentHierarchyDocumentTests`, `HierarchyAgentDefinitionsTests`,
`ServiceToolCatalogueTests`; Service: the two `...PermissionSplitTests`, `ReprocessingPermissionEscalationTests`,
`SplitReprocessingPermissionMigrationTests`, `TreeChildrenServiceTests`, `PermissionSpecificationServiceTests`,
`RoleRegistryPermissionServiceTests`, `PrivilegeEscalationServiceTests`, both existing reprocessing service test
classes, `PreservationServiceTests`; PenTest (serial pass): `PenTestModelReprocessingPermissionSplit`,
`PenTestModelReprocessingRule`, `PenTestModelReprocessingRuleInstance`, `PermissionParityTests`;
`npx vitest run src/permissions src/layout`; `vue-tsc`.

## 5. Translations

No new resource keys were needed server side: refusals reuse each service's existing `PermissionDenied`, so no `.resx`
was added or changed in any of the nine locales (en, pl, ar, ka, id, sv, ur, hi, fr) and the parity script had nothing
to compare; printed file list: none. Permission names are plain English strings in the `PermissionSpecification` table
and no resx, locale or description column translates any existing name, so the new ones follow the existing practice
(finding, shared TODO). SPA: one key, `crud.restrictedByPermission`, proposed in all nine locales in
`review/reprocessing-permissions.locale-proposals.json` (machine translated, fr formal register). Shared locale JSON not
edited.

## 6. Docs TODO markers

`docs/Configuration/Reprocessing/ReprocessingInstances/index.md`: four markers (names as shown in Roles screen; 26 can
stop indirectly by editing or deleting the rule; two screenshots, roles card and trigger-only view).
`docs/Configuration/Administration/Permissions/index.md`: one marker (names, link). One marker under each appended test
catalogue section.

## 7. Risks and design questions

- Design question: editing or deleting a rule supersedes it and cancels its uncompleted runs (existing behaviour,
  finding 4 of model-reprocessing), so a 26-only holder can stop a run indirectly. Not closed: requiring 74 for rule
  edit would defeat the point of letting rule editing be delegated alone.
- Instance Update and Validate sit under 73 (reconfiguring or requeueing a run is a trigger). Rule Validate, ParseRule,
  Execute, Build sit under 26 as authoring aids, so a trigger-only role cannot test rules.
- Legacy Razor frame `EntityAnalysisModelReprocessingRule.cshtml.cs` and the Razor Models menu still gate on 26 only;
  the API and SPA honour all three. Shared TODO raised, left alone because `AdvancedAnalyticsPageWiringTests` pins the
  layout string.
- Permission ids 73 and 74 could collide if another agent adds a permission after this read. Migration numbers used:
  only 20263108630001.
- The hint is hidden-with-explanation, not disabled buttons; `can('list')` false still shows the older "no list" text,
  but a user cannot reach the page without a read permission.
- A role whose 26 grant was deleted before the upgrade gets nothing, by design; an inactive 26 produces inactive 73 and
  74, so reactivating 26 later also needs the other two reactivated (the Permissions screen shows them separately).
- Nothing compiled. Highest-risk compile points: `DatabaseFixture` shared edit, the nested `Data.Poco` references in new
  tests (copied from siblings), FluentAssertions overloads in the parity test.
