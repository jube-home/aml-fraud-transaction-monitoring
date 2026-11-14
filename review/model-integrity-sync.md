# Slice review: model-integrity-sync

Agent-mode review, 2026-11-08. Steps 0, 2, 3, 4, 5. Nothing was built and no .NET, DB-backed test or Playwright was run.
npm prettier, eslint, vue-tsc and vitest (25 tests, integrity and sample) are clean.

## 1. Summary and verdict

READY AFTER a serial .NET test pass. Tenant isolation, permission verbs (integrity 60, sample 40, sync 5) and
hostile-input coverage are sound. Eight defects fixed, no design questions left. The lazy boundary holds: ModelStory
statically imports only `integrity/model` (pure TS, type imports), `integrity/api`, `sample/*`, ModelSample,
ModelSynchronisation; everything heavy (IntegrityCanvas, DataGrid detail components, diagram) is in the lazy chunk.

## 2. Files reviewed

Read: IntegrityEndpoints,
EntityAnalysisModelIntegrity/EngineState/DependencyGraph/Sample/NodeStatusEntries/SynchronisationSchedule endpoints
(route and authorisation sections), the five services, InProcessEngineStateSource, ModelIntegrityRules,
GetModelIntegrityInputsQuery, node status, schedule-by-instance and snapshot repositories, schedule validator, mapper
and DTOs, ModelSyncTaskStarter (sync section), ConfirmSync/Heartbeat/PersistEngineSnapshot extensions, Postgres sample
methods, all slice Vue files by grep plus IntegrityCanvas, ModelSample, ModelSynchronisation, DownloadButton in full.
Not read in depth: ModelGraphBuilder, ModelDependencyGraphBuilder, Detail/Findings components. First-touch pull-ins:
ModelSynchronisation.vue, ModelSample.vue, ModelDependencyResources (all unclaimed).

## 3. Findings

| #  | Sev             | Where                                                              | What                                                                                                                                                                                                                                                                                 | Status                                                                                                                                                                                                                                                                                                                                                                                                                                                   |
|----|-----------------|--------------------------------------------------------------------|--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| 1  | Medium          | EntityAnalysisModelSampleService.BuildCsv                          | `SelectToken(ValueJsonPath)` without regex timeout (path built from user field names)                                                                                                                                                                                                | Fixed with `BoundedJsonPath.Select`                                                                                                                                                                                                                                                                                                                                                                                                                      |
| 2  | Medium          | ConfirmSync, NodeStatusEntryRepository, schedule-by-instance query | Synchronised date stamped at confirmation, after the sync; a row scheduled during a long sync was marked honoured and never run                                                                                                                                                      | Fixed: query returns `ObservedDate`, confirmation stamps it                                                                                                                                                                                                                                                                                                                                                                                              |
| 3  | Low             | EntityAnalysisModelIntegrityService.RequireModelAsync              | GetByIdAsync has no Deleted filter, so a soft-deleted model could be checked, graphed and its engine state read                                                                                                                                                                      | Fixed                                                                                                                                                                                                                                                                                                                                                                                                                                                    |
| 4  | Low             | ModelIntegrityRules.EngineChecks                                   | Nodes silent past the tolerance or retention also got "has not loaded the model; synchronise it" (contradicting the retention comment in the test)                                                                                                                                   | Fixed: only reporting instances                                                                                                                                                                                                                                                                                                                                                                                                                          |
| 5  | Low             | integrity/types.ts, ModelSynchronisation.vue                       | `SyncNodeStatus.id` and `row-key="id"`: server sends no id                                                                                                                                                                                                                           | Fixed (row-key instance)                                                                                                                                                                                                                                                                                                                                                                                                                                 |
| 6  | Medium (defect) | Postgres sample query and BuildCsv                                 | Real memory risk: up to 100,000 archive rows (the whole payload and every derived value, commonly several KB of JSON each, so hundreds of MB of JSON, several times that as parsed JObjects) were held at once, plus the CSV and a byte copy, per request, times concurrent requests | Fixed: rows are streamed one at a time (`StreamJsonFromArchiveSampleAsync`), the CSV is written to a MemoryStream and refused past `SampleMaxBytes` (default 32 MiB, so about 64 MiB peak with the final copy per request) with a localised `SampleTooLarge` 400, never truncated silently; the 100,000 row SQL limit stays                                                                                                                              |
| 7  | Medium (defect) | schedule POST                                                      | No limit: every row makes every engine node re-sync                                                                                                                                                                                                                                  | Fixed: per-tenant (100) and per-model (30) counts of rows created in a window (10 minutes), all DynamicEnvironment settings, refusal is HTTP 429 with localised `ScheduleRateLimited`; counted from the database so it holds across app nodes. Unlike the rule-authoring backtest (`BacktestOnlineMaxSeconds`, a time bound read the same way) this bounds a count; both use settings with defaults in DynamicEnvironment, the service reading the value |
| 8  | Medium (defect) | Integrity titles and messages                                      | English-only server text                                                                                                                                                                                                                                                             | Fixed: `IntegrityCheck` carries `MessageKey` and `Arguments`; the service localises `Title_<Code>` and `Message_<Key>` from EntityAnalysisModelIntegrityResources (43 new keys, 8 locales, fr untouched), falling back to the English text; the SPA labels are fixed too, see 9                                                                                                                                                                          |
| 9  | Info            | schedule Insert                                                    | returns Poco, hard-coded DTO titles                                                                                                                                                                                                                                                  | SHARED-TODO                                                                                                                                                                                                                                                                                                                                                                                                                                              |
| 10 | Info            | EngineState                                                        | tenant sees engine hostnames (per-tenant rows)                                                                                                                                                                                                                                       | Left                                                                                                                                                                                                                                                                                                                                                                                                                                                     |
| 11 | Info            | node heartbeat insert                                              | first heartbeat stamps SynchronisedDate; unreachable in practice because a pending sync always precedes it                                                                                                                                                                           | Left                                                                                                                                                                                                                                                                                                                                                                                                                                                     |

| 9 | Low (defect) | SPA integrity labels | Category, entity kind, 'Engine instance'/'Model' and approval kind names
were English literals and `findingRows` compared and displayed them | Fixed: new `src/integrity/labels.ts` maps stable
codes (categories, ModelEntityKind names, pseudo kinds, approval kind numbers) to i18n keys; grids render through
`format`, link rows through `entityText`; `findingRows` now uses the code `EngineInstance`; labels fall back to the code
when a locale lacks the key (`te`). 46 keys in all eight locales are in
review/model-integrity-sync.locale-proposals.json (SPA JSON is shared). Vitest `labels.spec.ts` reads the C#
`ModelEntityKind` enum and the rule categories from source and fails if any lacks a key, plus approval kinds, key
uniqueness and fallback behaviour; `model.spec.ts` updated for the new fields. prettier, eslint, vue-tsc and vitest (20
integrity tests) pass. Not covered: e2e (the page shows codes until the keys are merged) |

## 4. Fixes made

1-8 above. Follow-up tests (unrun): `InsertBeyondTheTenantRateLimitIsRefusedAndWritesNothingAsync`,
`ASampleLargerThanTheByteLimitIsRefusedWithNoFileAndAnErrorAuditRowAsync`,
`TitlesAndMessagesAreLocalisedWithTheirArgumentsAsync`,
`EveryFindingCarriesAMessageKeyWhoseResourceTakesExactlyItsArguments` (reads the neutral resx and checks placeholder
count equals argument count for every finding the rules can produce). Existing
`RapidScheduling_IsBounded_AndEveryResponseIsPromptAsync` already accepts 429. New settings in DynamicEnvironment and
docs/Concepts/EnvironmentVariables: SampleMaxBytes, SynchronisationScheduleWindowMinutes,
SynchronisationScheduleMaxPerTenant, SynchronisationScheduleMaxPerModel. Files changed: Postgres.cs (stream),
EntityAnalysisModelSampleService, EntityAnalysisModelSynchronisationScheduleService (new overload taking
DynamicEnvironment, endpoint passes it), ScheduleRepository.CountCreatedSinceAsync, new RateLimitedException,
ModelIntegrityRules and the integrity service, three resource classes (8 resx each updated, three new keys plus 43).
Remaining `SelectToken` sites in cases, journal, exhaustive stay with their owners (SHARED-TODO line exists).

## 5. Test and pen-test matrix

Read: EntityAnalysisModelSampleServiceTests (31 names, CSV, tenant, audit, cancel), PenTestEntityAnalysisModelSample
(matrix, other tenant, CSV formula, boundaries, replay), EntityAnalysisModelIntegrityServiceTests,
ModelIntegrityRulesTests, PenTestModelIntegrityQueries (role matrix, other tenant, tampering; trait inherited from
PenTestBase), PenTestEntityAnalysisModelSynchronisationNodeStatusEntries,
PenTestEntityAnalysisModelSynchronisationSchedule (matrix, tenant scope with forged fields, date bounds, rapid
scheduling, shape, injection, type confusion), schedule service tests (names), schedule-by-instance query tests. Vitest
model specs and e2e spec names read.
Added (unrun): sample `AFieldWhoseJsonPathIsACatastrophicRegexFilterIsCutShortAndTheCellIsTheDefaultAsync`; integrity
`ASoftDeletedModelIsNotFoundByEveryOperationAsync`; query tests
`ConfirmingWithTheObservedDateLeavesARowScheduledDuringTheSyncPendingAsync`,
`ConfirmingWithTheObservedDateHonoursEveryRowThatWasPendingAsync`; rules
`ALiveInstanceWithNoStateForTheModelHasNotLoadedIt`; changed `TheEnginesLoadedEntitiesAreComparedWithTheActiveOnes`
(stopped nodes no longer report not-loaded). Catalogue rows updated in docs/Testing/ServiceQueries.md and Engine.md.
Tests to run: those classes plus PenTest for sample, schedule, node status and integrity; Rider lint was not run on the
C# edits.

## 6. Translations

42 files created (plus the new keys above added to the Sample, Schedule and Integrity classes in all eight locales,
parity script-checked): pl, ar, ka, id, sv, ur, hi for EntityAnalysisModelSample, Integrity,
SynchronisationNodeStatusEntries, SynchronisationSchedule, Dependency and ModelDependency Resources (key parity checked
with an XML script on three classes; placeholders kept, FSI/PDI isolation around {0} {1} {2} in ar and ur where ids or
names go). `.fr.resx` untouched. SPA pl and ar complete (162 keys); ka, id, sv, ur, hi proposals in
review/model-integrity-sync.locale-proposals.json (162 keys each, placeholder and plural parity verified). No physical
left/right CSS and no v-html in slice Vue files. Not audited visually: font clipping for ka, ur, hi.

## 7. Docs

Updated docs/Configuration/Models/ModelIntegrity/index.md (sections, stale 10-minute and Warning claims corrected, new
Synchronisation and Permissions sections, 12 TODO markers) and Sampling/index.md (limits section, 5 markers). Lines:
grep `TODO(` in those two files.

## 8. Shared-code TODOs

Five lines appended to review/SHARED-TODO.md.

## 9. Manual test sheet

Integrity: each section, check a model with a broken rule, stop an engine node, soft-delete a model and reload. Sync:
Synchronise now for a model and the tenant, schedule 2 years ahead (refused), no permission 5 (no form), two nodes.
Sample: window, rate 0 and 1, wrong tenant, no permission 40, field named with a regex filter. Locales en, pl, ar;
narrow width; dark; keyboard.

## 10. Commit pathspecs

`slice_audit.py model-integrity-sync --paths` plus: Jube.Data (query Dto and query, node status repository), Jube.Engine
(ConfirmSync, ModelSyncTaskStarter hunk, ModelIntegrityRules), Jube.Service sample and integrity services, the five test
files, 42 resx, ModelSynchronisation.vue, docs.

## Not covered

ModelGraphBuilder and dependency graph builder internals, SVG renderer escaping beyond its test names, Playwright,
accessibility, font clipping, the engine-side sync of every entity type, DB behaviour of the new tests.
