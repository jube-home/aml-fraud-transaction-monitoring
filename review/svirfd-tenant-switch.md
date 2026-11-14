# Svirfd per-tenant remote lookup switch

Agent mode, 2026-11-08. Nothing built, no test run, no Rider lint (owner builds). Scratch dir unused. Owner decision:
default ON.

## Design

- Column `SvirfdRemoteLookupsEnabled` (smallint NULL DEFAULT 1, Poco `byte?`) on `TenantRegistry` and
  `TenantRegistryVersion`, following the three HttpAdaptation columns. Null means on. DTO `bool`, default true,
  `[NewDefault(true)]`, switch widget, new form group `DataProtection` (order 50), landlord-only because it rides the
  landlord-only tenant service (`EnsureLandlord` on every verb). No validator rule is needed for a boolean.
- No new global key and no `DynamicEnvironment.cs` change: the installation-wide `EnableSvirfd` and
  `EnableSvirfdRemoteEngineLookup` already are the "global" tier. Effective rule: global allows AND tenant allows.
- Engine: resolved at sync in `SyncEntityAnalysisModelSvirfdExtensions` (one `TenantRegistry` read per tenant per sync,
  deleted tenants fall back to on), stored on the model's immutable `SvirfdPlan.RemoteLookupsEnabled`.
  `ExecuteSvirfdAsync` skips the remote call when off and passes the flag down.
- Host: `ISvirfdLookup` gained `LookupAsync/LookupFreshAsync(address, remoteAllowed, token)`; `SvirfdHost` returns null
  on the Remote path before the cache read, circuit check and the executor (so no breaker, pause or cache effect). Old
  overloads delegate with true. Local and Directory paths ignore the flag. `SvirfdNullLookup` implements both.
- No shared state leaks: the flag is passed per call; there is no new cache. The existing remote cache is keyed by IP
  only (public data) and stays shared, but a switched-off tenant returns before it is read, so it never receives another
  tenant's cached enrichment. A tenant id in the key was therefore not needed.
- Interactive paths (they also send IPs): `SvirfdService` (Svirfd page, status, AskJooby `SvirfdLookup` tool) resolves
  the caller's current tenant flag in `CreateAsync` through new `GetTenantSvirfdRemoteLookupsQuery` (fails closed when
  the user has no tenant row); in Remote mode with the switch off, Lookup throws `NotFoundException` with new
  `SvirfdResources.RemoteLookupsDisabledForTenant`. `CaseKeyJournalService.GetTransactionSvirfdAsync` passes the flag to
  the fresh lookup (drift shows no fresh record).
- Status: `SvirfdStatusDto.TenantRemoteLookupsEnabled` (Key 11), set by `SvirfdLookupMapper.ToStatus(lookup, flag)`.

## Files

New: `Jube.Migrations/Branches/TenantSvirfdSwitch/AddTenantSvirfdRemoteLookups.cs` (20263108620001, empty Down),
`Jube.Data/Query/GetTenantSvirfdRemoteLookupsQuery/GetTenantSvirfdRemoteLookupsQuery.cs`,
`Jube.Resources/SvirfdResources.fr.resx`, tests below, `review/svirfd-tenant-switch.locale-proposals.json`.
Edited: `Jube.Data/Poco/TenantRegistry.cs`, `TenantRegistryVersion.cs`,
`Jube.Dto/Repository/TenantRegistry/TenantRegistryDto.cs`,
`Jube.Service/Repository/TenantRegistry/TenantRegistryMapper.cs`, `Jube.Dto/Query/Svirfd/SvirfdStatusDto.cs`,
`Jube.Service/Query/Svirfd/SvirfdService.cs`, `SvirfdLookupMapper.cs`,
`Jube.Service/Query/CaseKeyJournal/CaseKeyJournalService.cs`, `Jube.Svirfd/Interfaces/ISvirfdLookup.cs`,
`SvirfdHost.cs`, `SvirfdNullLookup.cs`, `Jube.Engine/Svirfd/SvirfdPlan.cs`,
`SyncEntityAnalysisModelSvirfdExtensions.cs`, `SvirfdExtensions.cs`, `Jube.Resources/SvirfdResources.cs` and its 8 resx,
docs (below). The repository needed no change: update maps the whole poco, and the version snapshot is AutoMapper so the
new column is copied.

## Migration

20263108620001, FluentMigrator `Execute.Sql` per table: `ADD COLUMN ... smallint NULL DEFAULT 1` plus
`COMMENT ON COLUMN`. Existing rows get 1 from the default.

## Wire generated files

`TenantRegistryDto` (new Key 15) and `SvirfdStatusDto` (new Key 11) change the generated decoders: the owner regenerates
by building (`Jube.App/Vue/src/generated/wire.generated.ts`, `wireTypes.generated.json`, `Jube.App/wire.generated.stamp`,
and check `WireGeneratedDecodersTests`).

## Existing tests that could pin the old shape

Read, none needed an edit: `TenantRegistryServiceTests` HttpAdaptation tests (DTO default true so unchanged; old DTO
shape without the field simply gets true), `PenTestTenantRegistry` PUT/POST bodies without the field (default true),
`EntityVersionHistoryQueryServiceTests` (inserts Pocos, null column reads as on), `SvirfdSupportTests` (settings only).
One deliberate change: `SvirfdServiceTests.RefusalMessagesComeFromTheLocaliser...` culture list now includes `fr` (old:
7 locales, new: 8 plus en). Possible breakage to watch: `WireGeneratedDecodersTests`, form-model golden/translation
baseline for TenantRegistry (`FormModelTranslationBaseline.txt` had no TenantRegistry entries).

## Tests added

| Class                                                   | Kind    | Covers                                                                                                                                                                                                                                                                                               |
|---------------------------------------------------------|---------|------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| `Jube.Tests/Svirfd/SvirfdTenantSwitchTests.cs` (6, new) | Unit    | off sends nothing (plain and fresh); 25 off lookups with a failing handler leave handler calls 0, circuit closed, cache empty, then an allowed lookup still reaches the handler; off tenant does not read cache filled by another; default overloads still remote; Directory unaffected; null lookup |
| `Jube.Tests/Engine/Svirfd/SvirfdPlanTests.cs` (2, new)  | Unit    | plan default true, carries flag                                                                                                                                                                                                                                                                      |
| `SvirfdInvokeTests` (+1)                                | Service | tenant with switch off, sync, local lookup still enriches                                                                                                                                                                                                                                            |
| `SvirfdServiceTests` (+2)                               | Service | Remote mode off: NotFound, handler 0 calls, status false, restored true; Directory mode off still answers                                                                                                                                                                                            |
| `TenantRegistryServiceTests` (+4)                       | Service | new tenant defaults on; null row reads on; round trip and version-table snapshot; non-landlord refused and value unchanged                                                                                                                                                                           |
| `PenTestTenantRegistry` (+1)                            | PenTest | landlord can switch off; WithPermission, TenantB and all-permission users get 403 on PUT against own, other and target tenant, switch unchanged; create refused; read by other tenant leaks nothing                                                                                                  |

`docs/Testing` tables updated (counts and rows; new sections for the two new classes) so `TestDocumentationTests` should
pass.

Tests to run: `SvirfdTenantSwitchTests`, `SvirfdPlanTests`, `SvirfdResilienceTests`, `SvirfdRemoteHostTests`,
`SvirfdPrivacyTests` (Unit); `SvirfdServiceTests`, `SvirfdInvokeTests`, `TenantRegistryServiceTests`,
`EntityVersionHistoryQueryServiceTests` (Service, own DB); `PenTestTenantRegistry`, `PenTestSvirfdLookup` (PenTest
serial); `TestDocumentationTests`, `WireGeneratedDecodersTests`; a build.

## Translations

New server key `RemoteLookupsDisabledForTenant`: neutral, pl, ar, ka, id, sv, ur, hi, and fr (`SvirfdResources.fr.resx`
created, all 7 keys, formal French). Parity verified by script, all 9 files 7 keys identical order. No new
TenantRegistry keys were needed. Printed files: `Jube.Resources/SvirfdResources.resx`, `.pl`, `.ar`, `.ka`, `.id`,
`.sv`, `.ur`, `.hi`, `.fr` `.resx`. Field and group labels come from DTO names (no FormModelResources entries exist for
TenantRegistry fields). SPA proposals (en, pl, ar, ka, id, sv, ur, hi, fr):
`review/svirfd-tenant-switch.locale-proposals.json` (page notice, status label, tenant group title, field label and
hint); no SPA code consumes them yet.

## Docs and TODO markers

`docs/Configuration/Models/Svirfd/index.md` (per-tenant bullet replacing "no per-tenant switch"; 2 TODO markers),
`docs/Concepts/MultiTenancy/EnablingMultiTenancyAndSwitching/index.md` (new section; 3 TODO markers),
`docs/Concepts/EnvironmentVariables/index.md` (1 TODO). `grep -rn "TODO(" ` for lines. States landlord-only and why.

## Risks

Nothing compiled. Switch takes effect at next model sync (interactive paths read it per request). Fresh DB with a user
without tenant row fails closed (remote off). `SvirfdHost` shared breaker is per service host, not per tenant
(unchanged). A landlord PUT omitting the field resets it to true (same as the HttpAdaptation switch): UI always sends
it. Svirfd page does not yet show the new status field or notice (SPA proposals only).

## Recommendation: tenant-admin control

Yes, allow a tenant administrator to switch it OFF only, never back on: it is a privacy control, and a one-way ratchet
cannot weaken the operator's posture. Not built because it is not trivial: it needs a new permission, a tenant-scoped
endpoint and service method writing only this column (the tenant service is landlord-only), audit and version history,
and its own PenTest (cannot switch other tenants, cannot switch on). Until then a tenant asks the operator.

## Follow-up: SPA wiring and form labels

- `Jube.App/Vue/src/svirfd/types.ts`: `SvirfdStatus.tenantRemoteLookupsEnabled`. New `src/svirfd/status.ts`
  (`tenantRemoteOff`, `remoteBlocked`).
- `SvirfdPage.vue`: info alert `t('svirfd.tenantRemoteOff')` when off, a status row
  `t('svirfd.status.tenantRemoteLookups')` with Yes/No, and `SvirfdSearch` gets `remote-blocked` (check button disabled,
  lookup query disabled) when off and mode is Remote. `SvirfdSearch.vue`: new `remoteBlocked` prop.
  `cases/journal/SvirfdPanel.vue`: same alert from the status query.
- Tests: vitest case in `src/svirfd/__tests__/svirfd.spec.ts`; two Playwright cases and the status stub field in
  `e2e/svirfdPage.spec.ts` (not run). prettier and eslint clean, `vue-tsc --build` clean (generated wire not needed to
  typecheck: the status is read as plain JSON via `useApi`), vitest `src/svirfd`, `src/locales`, `svirfdMenu`: the two
  Svirfd keys are now referenced; one unrelated failure in the locales spec: `override.approval.title|close|note`
  referenced but missing in en (another agent's slice).
- Form labels: the neutral `FormModelResources.resx` is empty by design (English comes from the DTO: the field label is
  the humanised property name, the group label the humanised path, the description from `[Description]`), so no neutral
  keys were added. Added to pl, ar, ka, id, sv, ur, hi `FormModelResources.<c>.resx` (113 entries each, was 110):
  `TenantRegistry.svirfdRemoteLookupsEnabled` (label), `TenantRegistry.svirfdRemoteLookupsEnabled.description`
  (SourceHash comment 8c1916e8, computed from the DTO text), `TenantRegistry.group.DataProtection`. Baseline unchanged
  (the new description is translated). `FormModelResources.fr.resx` not created, as instructed. Pre-existing gap
  noticed: the three `HttpAdaptation*` field descriptions and placeholder on `TenantRegistryDto` are neither translated
  nor in `FormModelTranslationBaseline.txt`, so `TheUntranslatedTextsAreExactlyThoseInTheBaseline` will fail for them
  until translated or baselined.
