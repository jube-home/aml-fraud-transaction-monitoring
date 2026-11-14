# Slice review: model-exhaustive

Agent-mode review, 2026-11-08. Steps 0, 2, 3, 4, 5. Nothing was built and no .NET or DB-backed test was run. Front end:
`prettier --check`, `eslint` on the slice files and `vitest run src/exhaustive src/dataset src/upload` (3 files, 75
tests) passed; type-check and Playwright not run. Rider `lint_files` not run on the C# edits.

## 1. Summary and verdict

Verdict: READY AFTER OWNER DECISIONS and one serial test pass. Five real defects fixed (one security, one data, one
flake, one unbounded regex, one soft-delete), one missing feature found (z-score clipping is not in this tree), and one
large gap left (chart and narrative text is English only).

Answers to the brief:

1. **BuildVariablesFromFields exclusion.** Verified in `Jube.Engine/Exhaustive/Utilities/DataUtility.cs:269`. The filter
   is `ProcessingTypeId is 3 or 5 or 7 or 8 or 9` (the memory says 3, 5, 7, 8; 9 is also in). Payload (1) is excluded,
   and only `CategoricalExpansion.Expand` re-admits Payload fields. `LoadVariableSelectionTests` pins it.
2. **Variable count 2x.** Confirmed as a code defect with a plausible mechanism, fixed (finding 2).
   `GetExhaustiveSearchInstancePromotedTrialInstanceVariableQuery` applied the `Deleted` filters only inside
   `tenantRestricted`, and the engine and sync constructors are tenantless. `VariableSelector.SelectVariablesAsync`
   soft-deletes the trial's variables and re-inserts them on each re-selection, so the engine loaded live plus
   soft-deleted rows. I could not reproduce the exact 2x ratio without a run; the owner's serial run of
   `RecallChainTests` and `CategoricalTrainingTests` is the proof.
3. **Z-scores clamped.** NOT true in this tree. `Standardisation.ZScore`, `Normalisation.ZScore`,
   `ExhaustiveSearchInstancePromotedTrialInstanceVariable.ZScore` have no clamp. The work exists only on branch
   `exhaustive-zscore-clipping` (dfad0797, not an ancestor of HEAD); `git apply --check` of that commit fails on 7
   files. Not hand-ported blind (14 files, would touch training, recall, sync and DynamicEnvironment). Owner decision:
   merge or port (BUG line in SHARED-TODO).
4. **Challenger-kind flake.** Cause found and fixed in the tests (finding 3). The memory says it was fixed in the
   weekly-build-flakes worktree; it is not in this branch.
5. **filterSql.** Audited end to end, see section 3a. Sound in depth; one bypass of the first layer found and fixed.
6. **Artefact serialisation.** `TypeNameHandling.Auto` with the default binder on stored network JSON: fixed with an
   allow-list binder (finding 4).

## 2. Files reviewed (first-touch pull-ins)

`--closure`: shell-auth (`useFormat.ts`, `i18n/index.ts`, `en.json`), data-grid (`DataGrid.vue`, `JubeChart.vue`),
rule-authoring (`BuilderStatus.vue`, `ruleText.ts`), wire-and-api (`ProblemAlert.vue`) are claimed by later slices and
were NOT pulled in or reviewed here (inherited).

| File                                                                                                                                                                                                                                          | Read                                                | Notes                                                    |
|-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|-----------------------------------------------------|----------------------------------------------------------|
| Jube.Validations/FilterSqlText.cs                                                                                                                                                                                                             | whole                                               | edited                                                   |
| Jube.Validations/ExhaustiveSearchInstance/ExhaustiveSearchInstanceDtoValidator.cs                                                                                                                                                             | 160-216                                             | filter rules                                             |
| Jube.Data/Reporting/Postgres.cs                                                                                                                                                                                                               | 40-120, 200-325, 700-722                            | filter execution, guard, read-only transaction, timeouts |
| Jube.Data/Reporting/PostgresSqlValidator.cs                                                                                                                                                                                                   | 1-400                                               | AST gate, denied functions and relations                 |
| Jube.Data/Reporting/SampleSource.cs                                                                                                                                                                                                           | whole                                               | fixed FROM and key column                                |
| Jube.Engine/Exhaustive/Utilities/DataUtility.cs, CategoricalExpansion.cs                                                                                                                                                                      | 232-300, 488-541; 100-125                           | edited                                                   |
| Jube.Engine/Helpers/JsonSerializationHelper.cs                                                                                                                                                                                                | whole                                               | edited; new `AnchoredAssemblySerializationBinder.cs`     |
| Jube.Engine/EntityAnalysisModelInvoke/Extraction/BoundedJsonPath.cs and its test                                                                                                                                                              | whole                                               | reused                                                   |
| Jube.Data/Query/GetExhaustiveSearchInstancePromotedTrialInstanceVariableQuery                                                                                                                                                                 | whole                                               | edited                                                   |
| Jube.Data/Repository/ExhaustiveSearchInstancePromotedTrialInstanceRepository.cs                                                                                                                                                               | 40-110                                              | edited                                                   |
| Jube.Data/Repository/ExhaustiveSearchInstanceTrialInstanceVariableRepository.cs                                                                                                                                                               | whole                                               | soft delete                                              |
| Jube.Engine/Exhaustive/Selection/VariableSelector.cs, Promotion/PrescriptionBuilder.cs, Statistics/StatisticsCalculator.cs, PopulationStatisticsWriter.cs (30-140), StatisticsTargetResolver.cs, Stages/StoreStage.cs, BaseStatisticsStage.cs | read                                                | variable bookkeeping                                     |
| Jube.Engine/.../SyncExhaustiveSearchInstancesExtensions.cs                                                                                                                                                                                    | 60-285                                              | model build                                              |
| Jube.Engine/Exhaustive/Algorithms/ExhaustiveTrainingSettings.cs, Supervised.cs (95-130)                                                                                                                                                       | read                                                | challenger probability                                   |
| Jube.Service/Repository/ExhaustiveSearchInstancePromotedTrialInstance/...Service.cs                                                                                                                                                           | 60-229                                              | permission 14, validator, audit scope                    |
| Jube.Analytics/Datasets/DatasetJsonParser.cs (limits), Jube.Service/.../DatasetLimitsFactory.cs                                                                                                                                               | grep                                                | limits                                                   |
| Jube.Service/Charts renderers                                                                                                                                                                                                                 | grep for literals only                              | not read                                                 |
| docs/Configuration/ExhaustiveAdaptation (7 pages)                                                                                                                                                                                             | headings, image lines, TrainingConfiguration 36-100 | edited                                                   |

Not read in depth: `Mock.cs` (1850 lines), `Supervised.cs` beyond the settings, `TopologyEvolver`, `DataOptimiser`,
`ExhaustiveSearchInstanceService.cs` (1219 lines, permissions and test names only),
`ExhaustiveInstanceDatasetService.cs` (766), the endpoint files (not read, covered by PenTest names), the Vue components
beyond grep.

## 3. Findings

| #  | Severity          | Where                                                                     | What                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                 | Status                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                  |
|----|-------------------|---------------------------------------------------------------------------|------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| 1  | Medium (security) | Jube.Validations/FilterSqlText.cs                                         | The text check tracked quotes but not PostgreSQL `E'...'` strings, where `\'` is an escaped quote. `x = E'\'' or pg_sleep(1) is null or x = E'\''` has an even quote count for the checker, hides the middle as "inside quotes", yet PostgreSQL parses it as code. The execution-time AST gate (`AssertSelectOnly`) catches denied functions, so it was not exploitable end to end, but the first layer was bypassable and any harmless-but-forbidden construct (`order`, `limit`, subquery-free CPU burners not on the deny list) passed save time. | Fixed: a single quote preceded by e or E that is not the end of an identifier is refused. Also replaced the `partial` `[GeneratedRegex]` with a static `Regex` with a 2 s timeout (house style, refusal on timeout).                                                                                                                                                                                                                                                                                                                                                                    |
| 2  | High (defect)     | GetExhaustiveSearchInstancePromotedTrialInstanceVariableQuery.cs:44-52    | `Deleted` filters on the trial variable and the instance variable only applied when `tenantRestricted`; the sync, invoke and recall paths use the tenantless constructor, so soft-deleted trial variables (left by every re-selection) were loaded as extra network inputs. Matches the "variable count 2x" symptom.                                                                                                                                                                                                                                 | Fixed: both `Deleted` filters unconditional.                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                            |
| 3  | Medium (flake)    | CategoricalTrainingTests, RecallChainTests                                | Training promotes an AdvancedAnalytics challenger with probability `ExhaustiveChallengerModelBaseProbability` (default 0.1, rising with topology complexity, random). A promoted challenger has no `TopologyNetwork`; the test built the model from `Json` only (KindId null, so network path), got a null network and an NRE at `TopologyNetwork.Compute`, or `InputsCount` mismatch.                                                                                                                                                               | Fixed: both tests pin `ExhaustiveChallengerModelBaseProbability` to 0 in the test environment; the Categorical test also passes the promoted row's `KindId`. No retry reliance.                                                                                                                                                                                                                                                                                                                                                                                                         |
| 4  | High (security)   | Jube.Engine/Helpers/JsonSerializationHelper.cs                            | `TypeNameHandling.Auto` with the default binder on the stored promoted network JSON (`ExhaustiveSearchInstancePromotedTrialInstance.Json`, also carried by preservation import): a `$type` could name any loadable type.                                                                                                                                                                                                                                                                                                                             | Fixed: `AnchoredAssemblySerializationBinder` permits only primitives, string and types from assemblies it was anchored to (see section 12; arrays and generic arguments checked); other types raise a JsonSerializationException that the existing Error handler swallows, so the model loads without that member and does not score (logged by `LogIncompleteModel`). `NetworkRoundTripTests` (every activation function, `errors.Should().BeEmpty()`) is the regression guard against over-restriction. Challenger JSON (`AdvancedAnalyticsModelRegistry.Build`) is not covered here. |
| 5  | Medium (DoS)      | DataUtility.cs:527, CategoricalExpansion.cs:118                           | Plain `SelectToken(path)` on stored value paths: a `=~` filter runs with no regex timeout (the model-references seed).                                                                                                                                                                                                                                                                                                                                                                                                                               | Fixed: both use `BoundedJsonPath.Select` (250 ms, timeout selects nothing). Sites in `Jube.Service/Query/CaseKeyJournal` and `Jube.Data/Query/CaseQuery` belong to cases, not touched.                                                                                                                                                                                                                                                                                                                                                                                                  |
| 6  | Medium (defect)   | ExhaustiveSearchInstancePromotedTrialInstanceRepository.UpdateActiveAsync | The row lookup ignored `Deleted`, so a soft-deleted promoted trial (or one under a deleted instance) could be reactivated and then selected by `GetLastActiveByExhaustiveSearchInstanceIdAsync`. A test pinned this as intended ("StillSucceedsBecause...DoesNotFilterDeleted").                                                                                                                                                                                                                                                                     | Fixed: not found for deleted trial or instance; test renamed and inverted. The `                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                        || !tenantRegistryId.HasValue` escape remains (shared line). |
| 7  | High (missing)    | Jube.Engine/Exhaustive                                                    | Z-score clipping absent (section 1 item 3).                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                          | Left, shared BUG line; merge or port decision.                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                          |
| 8  | Medium (locale)   | Jube.Service/Charts/Exhaustive*Renderer.cs, ExhaustiveNarrativeRules.cs   | Chart titles, series names, tile labels and the narrative verdict are hard-coded English (about 100 strings); renderers take no culture.                                                                                                                                                                                                                                                                                                                                                                                                             | Left, shared TODO (needs a resource class and culture plumbing; too large for this pass).                                                                                                                                                                                                                                                                                                                                                                                                                                                                                               |
| 9  | Low (design)      | Promotion                                                                 | Promotion (permission 14) is not under maker-checker; `ExhaustiveSearchInstance` is. PermissionParityTests `ExhaustivePage_ViewPermissionCannotPromote_ByDesignAsync` documents 14 as deliberate.                                                                                                                                                                                                                                                                                                                                                    | Left, DESIGN: the code does what it was written to do; whether activating a different promoted model should need approval is intent.                                                                                                                                                                                                                                                                                                                                                                                                                                                    |
| 10 | Info              | Tenant escape                                                             | `                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                    |                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                         | !tenantRegistryId.HasValue` in ExhaustiveSearchInstancePromotedTrialInstanceRepository (and siblings). Service always resolves a tenant, so not reachable through the API. | Shared line. |

### 3a. filterSql audit

| Question           | Evidence                                                                                                                                                                                                                                                                                                                                                                                  |
|--------------------|-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| Validated          | `FilterSqlText.IsAcceptable` on every Insert and Update (`ExhaustiveSearchInstanceDtoValidator` 172-181): length 65,536, no `; \ $ NUL` or controls, no `-- /* */`, balanced quotes and parentheses (depth never negative), a deny list of keywords, `pg_*`, `lo_*`, `regclass`, `current_setting`, `version(`, and now E strings. Tokens must be a JSON array and `JsonText`-acceptable. |
| Parsed             | At execution `Postgres.ExecuteReturnOnlyJsonFromArchiveSampleAsync` and `ExecuteCountFromArchiveAsync` wrap the filter in a fixed statement and run `PostgresSqlValidator.AssertSelectOnly` (libpg_query AST: one statement, SELECT only, denied function names and prefixes including quoted names, depth 64, 65,536 characters).                                                        |
| Tables and columns | `from "<Archive                                                                                                                                                                                                                                                                                                                                                                           |MockArchive|dataset row table>" where "<key column>" = @n and (<filter>)`: `SampleSource` is built in code from the instance's model or dataset id, never from user text. A subquery needs `select`, refused twice. |
| Tenant scoping     | The key is the instance's own model id (tenant checked at insert; `PenTestExhaustiveInstanceJson.InstanceCannotReferenceAForeignOrAmbiguousJsonDataset_MassAssignmentAsync` for datasets).                                                                                                                                                                                                |
| Read-only, limits  | Connection options `default_transaction_read_only=on`, `statement_timeout` and `lock_timeout`; each query also opens `BEGIN READ ONLY; SET LOCAL statement_timeout = 30000; SET LOCAL lock_timeout = 5000`.                                                                                                                                                                               |
| Comments, stacked  | Refused by text and by the single-statement AST check. A filter closing the wrapper parenthesis is refused by the depth rule (otherwise `1=1) or (...` is a valid SELECT that drops the model key predicate).                                                                                                                                                                             |
| Gap found          | Finding 1. Before this pass there was NO direct test of `FilterSqlText` at all (only `FilterSqlNotEmpty` and one happy path in the service tests).                                                                                                                                                                                                                                        |

## 4. Fixes made

1. FilterSqlText: E-string refusal, static Regex with timeout (unit `FilterSqlTextTests`, new).
2. Variable query: unconditional `Deleted` filters (`ReselectingTrialVariables_NeverLeavesTheSoftDeletedRows...` in
   `TrainDbRecorderTests`, new).
3. Challenger flake: both tests pin the probability (existing tests edited).
4. `AnchoredAssemblySerializationBinder` and its wiring in both settings (`AnchoredAssemblySerializationBinderTests`,
   new, 3 methods, a hostile gadget type with a static constructor counter).
5. BoundedJsonPath at two sites (`ACatastrophicRegexFilterInAStoredPathIsCutShortAndReadsAsZero` in
   `LoadJsonExtractionTests`).
6. `UpdateActiveAsync` deleted filter (`UpdateOfSoftDeletedRowThrowsNotFoundAndDoesNotReactivateItAsync`).
7. 105 resx, locale proposals, docs (below). `docs/Testing` updated: Exhaustive.md (TrainDbRecorderTests 9,
   LoadJsonExtractionTests 14, new binder section), AppAndInfrastructure.md (new FilterSqlTextTests section),
   ServiceRepositories.md (renamed row).

No code comments added or touched. One class per file. No partial classes added (one removed).

## 5. Test and pen-test matrix

| Area                                             | Evidence read                                                                                                                                                                                                                                                                                                                                                                                                                                             | Status                                                                                                                                                        |
|--------------------------------------------------|-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|---------------------------------------------------------------------------------------------------------------------------------------------------------------|
| Instance service                                 | `ExhaustiveSearchInstanceServiceTests` method list (about 55): CRUD, permission 16, tenant isolation, validators, tamper, locked and picked-up update, maker-checker resubmit and retract, audit, span, change event                                                                                                                                                                                                                                      | Covered; filter hostile gap closed by unit test                                                                                                               |
| Promoted trial service                           | `ExhaustiveSearchInstancePromotedTrialInstanceServiceTests` (about 25)                                                                                                                                                                                                                                                                                                                                                                                    | Covered; deleted behaviour corrected                                                                                                                          |
| Pen tests                                        | `Security/Model/PenTestExhaustiveSearchInstance`, `Security/Identity/PenTestExhaustiveSearchInstancePromotedTrialInstance`, `Security/Exhaustive/PenTestExhaustiveInstanceCsv` (16) and `...Json` (20, method names read: matrix, cross-tenant, mass assignment, hostile bodies, oversize, chunked, traversal labels), `Security/QueryExhaustive/*` (ROC, error histogram, learning curve, variable, promotion history), `PermissionParityTests` line 229 | Covered by name                                                                                                                                               |
| No PenTest posts a hostile `filterSql` over HTTP | grep                                                                                                                                                                                                                                                                                                                                                                                                                                                      | Not added: it would overlap the new unit test of the same validator; the PenTest base (`ModelAreaPenTestBase`) is the owner of a generic hostile-string probe |
| Datasets                                         | `DatasetCsvParserTests` (33), `DatasetJsonParserTests` (57), `DatasetMappingRulesTests` (12), service tests (names)                                                                                                                                                                                                                                                                                                                                       | Covered by name                                                                                                                                               |
| Training                                         | `TrainLoopTests`, `TrainMetricsTests`, `TrainInstanceRunTests`, etc. by count; `LoadSampleSqlTests` read                                                                                                                                                                                                                                                                                                                                                  | Not re-read                                                                                                                                                   |
| Recall and sync                                  | `SyncBuildModelTests`, `ExhaustiveAdaptationsExtensionsTests`, `RecallChainTests`                                                                                                                                                                                                                                                                                                                                                                         | Flake fix only                                                                                                                                                |
| Front end                                        | vitest 3 files 75 tests pass; e2e specs exhaustiveRecord (8), exhaustiveResults (22), datasetUpload (14) not run                                                                                                                                                                                                                                                                                                                                          | `.vue` logic tests not audited for vacuous assertions                                                                                                         |
| Training resource limits, cancellation           | `DbTrialRecorder.IsStopRequestedAsync` and `InstanceStatus.IsStoppingOrStoppedOrCancelled` calls in stages read; the unsupervised, correlation and supervised loops not read for cancellation points                                                                                                                                                                                                                                                      | Partly covered                                                                                                                                                |

Tests needing the owner's serial run: `FilterSqlTextTests`, `AnchoredAssemblySerializationBinderTests`,
`NetworkRoundTripTests`, `LoadJsonExtractionTests`, `TrainDbRecorderTests`,
`ExhaustiveSearchInstancePromotedTrialInstanceServiceTests`, `CategoricalTrainingTests`, `RecallChainTests`,
`SyncBuildModelTests`, then `Category=PenTest` for the Exhaustive classes. If `RecallChainTests` still shows
`NetworkVariablesInOrder` = 2x `InputsCount`, finding 2 was not the whole cause.

## 6. Translations

Server: created `.pl .ar .ka .id .sv .ur .hi` for 15 classes (105 files): ExhaustiveInstanceDataset,
ExhaustiveSearchInstance, ...PromotedTrialInstance, ...PromotedTrialInstanceConfusion, ErrorHistogram, LearningCurve,
PredictedActual, Query, Roc, VariablePrescription, ExhaustiveSearchInstanceTrialInstanceListQuery,
...TrialInstanceVariable, ...TrialInstanceVariableVariance, ExhaustiveSearchInstanceVariable, and
`ExhaustiveSearchInstancePromotionHistoryResources`, which had a `.cs` but no `.resx` at all (a neutral `.resx` was
created from the sibling Roc file plus its seven locales). Generated by a script kept in the slice's scratchpad
subfolder, which printed the 105 files it wrote (all begin `Exhaustive`). Verified for all 112 locale files of the
slice's classes: well-formed, key set equals English, `{0}` kept, no value equal to English, no Eastern or Devanagari
digits. In ar and ur every Latin or numeric run (CSV, JSON, UploadedDataset, 256, {0}) is wrapped in U+2068 and U+2069.
`.fr.resx` untouched. `ExhaustiveSearchInstanceVariableStatisticsResources.*` already existed (not mine, parity OK).

SPA: no gaps in en, pl, ar (219 keys under exhaustive, dataset, upload; audit script: 0 missing, 0 empty, one pl
same-as-English "Monte Carlo", proper noun). Proposals for ka, id, sv, ur, hi:
`review/model-exhaustive.locale-proposals.json` (219 keys each, nested, placeholders verified identical to English,
Western digits). No hard-coded UI strings found by the audit and `FilterSql` is never displayed, so no bidi isolation is
needed for SQL in the UI.

Open (shared): fonts for ka, ur, hi and the Nastaliq and Devanagari line-height work are already on SHARED-TODO
(shell-auth). Server chart text is English only (finding 8). No physical left and right CSS in the slice components
(grep). Chart RTL not examined.

## 7. Docs written and TODO markers

Seven pages under `docs/Configuration/ExhaustiveAdaptation/`: every image reference renamed
`model-exhaustive-<kebab>.png` with a `TODO(screenshot)` above it (old PNGs left on disk), a page-level `TODO(docs)`
under each H1 (Stopping has no H1). New content in TrainingConfiguration: "Class Definition filter: validation and
execution", "Uploaded datasets" (limits 50 MB, 1,000,000 rows, 500 columns, JSON depth 8), "Permissions and security";
in TrainingPromotedModelPerformance: "Promotion rules". Marker lines (47 total, re-grep `TODO(`): Concepts 11, 36, 41,
65; Recall 11, 22; Stopping 12, 17, 31; Testing 11, 14, 19, 24, 29; Performance 11, 14, 19, 34, 39, 46, 51, 58, 65, 70,
75, 78, 88; Configuration 11, 44, 50, 82, 104, 107, 114, 120, 126, 133, 140, 164, 175, 176, 185; VariableStatistics 11,
18, 23, 30, 35. About 30 screenshots. Statements flagged for the owner: the refused-word list and the 30 s and 5 s
timeouts, permission 14 for promotion.

## 8. Shared-code TODOs raised

Five lines under `model-exhaustive` in `review/SHARED-TODO.md`: z-score clipping port (BUG), chart and narrative
English, tenant escape in the promoted trial repository (BUG), VariableStatistics resx provenance, SPA locale proposals.

## 9. Manual test sheet

1. Permissions: 16 alone sees the pages, can create and stop but cannot activate a promoted model (403); 14 alone cannot
   list.
2. Instance form: Class Definition on with no filter (refused); a filter with `;`, `--`, `/* */`, `select`, `E'\''`, an
   unbalanced bracket (each refused with the localised message); a normal Query Builder filter saves.
3. Datasets: upload CSV and JSON, a 0 byte file, a wrong extension, an over-limit file, JSON nested 9 deep, duplicate
   variable names, an included constant column; delete while an adaptation is training (refused).
4. Train on the demo dataset; confirm the promoted model's variable count equals the network input count, repeat three
   times; stop mid-run.
5. Promote: activate another model (siblings deactivate), deactivate the active (next best activates), delete an
   adaptation then try to activate its model (not found).
6. Batch recall CSV with a hostile path and 10,000 rows; sensitivity sliders; charts in each locale (English chart text
   expected).
7. en, pl, ar, ka, id, sv, ur, hi: validator messages (ar and ur isolates), dataset page, status band, narrow width,
   dark theme, keyboard only, one hostile string per field.

## 10. Commit pathspecs

`slice_audit.py model-exhaustive --paths` (slices.json unedited), plus hand-picked: `Jube.Validations/FilterSqlText.cs`,
`Jube.Engine/Helpers/JsonSerializationHelper.cs`, `Jube.Engine/Helpers/AnchoredAssemblySerializationBinder.cs`,
`Jube.Engine/Exhaustive/Utilities/DataUtility.cs` and `CategoricalExpansion.cs`,
`Jube.Data/Query/GetExhaustiveSearchInstancePromotedTrialInstanceVariableQuery/...Query.cs`,
`Jube.Data/Repository/ExhaustiveSearchInstancePromotedTrialInstanceRepository.cs`, the 105 new
`Jube.Resources/Exhaustive*Resources.<locale>.resx` plus `ExhaustiveSearchInstancePromotionHistoryResources.resx`, tests
`Jube.Tests/Validations/FilterSqlTextTests.cs`, `Jube.Tests/Exhaustive/AnchoredAssemblySerializationBinderTests.cs`, and
edits to `TrainDbRecorderTests.cs`, `LoadJsonExtractionTests.cs`, `CategoricalTrainingTests.cs`, `RecallChainTests.cs`,
`ExhaustiveSearchInstancePromotedTrialInstanceServiceTests.cs`, docs `Testing/Exhaustive.md`, `AppAndInfrastructure.md`
(one section), `ServiceRepositories.md` (one row), `review/model-exhaustive*`.

## 11. Not covered

No build, test, Rider lint or Playwright run; every C# edit and new test is unverified by a compiler (notably the
`Jube.Data.Query...Query` fully qualified name in `TrainDbRecorderTests`, the hostile-type string replace in the binder
test which assumes the serialised `$type` is `Accord.Neuro.ActivationFunctions.SigmoidFunction, Accord.Neuro`, and the
`AssertSelectOnly` outcomes for quoted function names). Not read: `Mock.cs`, the stage loops for cancellation, memory
use of dataset and archive sampling at training (`Extraction.SampleLimit` 10,000 read only),
`ExhaustiveInstanceDatasetService`, the endpoint files, challenger JSON deserialisation, Vue components in depth, SVG
chart RTL, the tenant escape in sibling repositories. The doubling mechanism (finding 2) is inferred from code, not
reproduced.

## 12. Follow-up pass (owner policy, bugs fixed, 2026-11-08)

Nothing built or run. All new and changed tests are for the owner's serial pass.

**1. Binder no longer names Accord.** `Jube.Engine/Helpers/AnchoredAssemblySerializationBinder.cs` (replaces
`TopologyNetworkSerializationBinder.cs`, deleted) takes anchor types in its constructor and permits primitives, string,
arrays and generics of them, and any type from the anchors' assemblies. `JsonSerializationHelper` anchors it on
`typeof(ActivationNetwork)` (a type, not a name string; the only Accord mention left is the `using` line, as in the rest
of Jube.Engine). Tests renamed to `AnchoredAssemblySerializationBinderTests` (4 methods; the gadget test derives the
stored `$type` text from the type instead of a literal; new test for a binder with no anchor). Docs wording changed to
"the neural network library". Risk: if a stored network carries a type from a second assembly the model silently fails
to load; `NetworkRoundTripTests` (every activation function, `errors.Should().BeEmpty()`) is the guard.

**2. Z-score clamp ported by hand from dfad0797.** Read with `git show`; nothing checked out, applied or merged for
source files. What was ported unchanged: `Jube.Engine/Exhaustive/ExhaustiveZScoreClipBoundResolver.cs` (new file), the
`ExhaustiveZScoreClipBound` default of 5 in `DynamicEnvironment.cs` (inserted after `ExhaustiveRecallTimeout`),
`Normalisation` (environment parameter, clamp in `NormaliseData`, optional `clipBound` on the static `ZScore`),
`ExhaustiveSearchInstancePromotedTrialInstanceVariable` (`ZScoreClipBound`, `ClampZScore`, clamped `ZScore`),
`ExhaustiveExtensions.BuildScoreInputs` (clamp when standardising, the gap the original commit found), `PrepareStage`
and `AnomalyDetectionStage` (pass `environment`), `Training` (pass `environment` to `PrepareStage` and `NormaliseData`),
`SyncExhaustiveSearchInstancesExtensions.BuildExhaustiveModel` (optional `environment`, bakes the bound into each
variable). What I changed: (a) the resolver also falls back to the default for a non-positive or non-finite value (a
zero or NaN bound would zero or poison every input; the original only checked parseability), (b) the call sites were
adapted to this tree's `using` style and the sync caller passes `context.Services.DynamicEnvironment`, (c)
`InvokeService.cs` passes its `dynamicEnvironment` to `BuildExhaustiveModel` (the original branch had no such caller;
without it the on-demand recall path would use the default and ignore a configured bound). Docs: environment variable
row added to `docs/Concepts/EnvironmentVariables/index.md` (text as in the branch plus the fallback sentence). Tests:
`ExhaustiveZScoreClipBoundResolverTests` (new file from the branch, applied with `git apply` for that file and
`ExhaustiveRecallTests`), plus hand-merged methods in `SyncBuildModelTests` (2) and `TrainingRepairTests` (6, including
training and recall clamping to the same bound); new by me: non-positive and non-finite override falls back (Theory of
4) and an indicator within the bound is not moved. Known limit carried over: `PopulationStatisticsWriter` uses the
default bound for the triangular distribution (shared line). The branch `exhaustive-zscore-clipping` must not be merged
on top of this.

**3. Chart titles and narrative English only: NOT done.** Evidence of size: about 330 string-literal sites across
`ExhaustiveChartRenderer` (49), `ExhaustivePerformanceRenderer` (93), `ExhaustiveStatisticsChartRenderer` (34 plus
public `const` series and title names), `ExhaustiveCalibrationRenderer` (38), `ExhaustiveTopologyRenderer` (23 plus
consts), `ExhaustiveNarrativeRules` (57 sentences built by concatenation with numbers) and `ExhaustiveNarrativeService`
(13), and 209 references to those renderers' public consts and outputs in 10 test files (17 files in
`Jube.Tests/Service/ExhaustiveCharts`, golden-style assertions on English text). The renderers are static and
culture-free; localising means a resource class with seven locales (about 330 keys times 7), threading
`IStringLocalizer` and a culture through every static renderer and narrative rule (sentence word order differs, so the
narrative concatenations need format strings with placeholders and plural handling), updating every test that asserts
English, and checking ar and ur sentence rendering against `direction:ltr` set in Jube.Charting by
model-advanced-analytics (not undone). I judged that a refactor of this size with no compiler or test run available is
more likely to break the 17 chart test files than to help, so it was not started; it remains an open finding on
SHARED-TODO and is the largest remaining item of this slice. Suggested plan: one `ExhaustiveChartResources` class per
renderer, narrative first (the SPA shows it as text), then renderer titles, then tests.

**4. Promotion and maker-checker: BUG, fixed consistently with model-advanced-analytics.** Evidence: the engine loads an
instance via `GetApprovedByEntityAnalysisModelIdOrderByIdAsync` (approved version, `Active`) but picks the promoted
model with `GetLastActiveByExhaustiveSearchInstanceIdAsync`, i.e. the newest row with `Active = 1`, with no approval
check; `UpdateActiveAsync` (permission 14, one request) flips that flag, so one user could change what a live instance
scores for a tenant with no checker. Fix: `ExhaustiveSearchInstanceRepository.IsLiveAsync` (current row Active, or its
approved resolution Active, tenant and not-deleted scoped),
`ExhaustiveSearchInstancePromotedTrialInstanceRepository.GetScopeAsync` (instance id and current state of the visible,
not-deleted promoted row), and the service now: not found if invisible, no-op if the requested state equals the current
one, else `ConflictException` (new, `Code` `InstanceIsLive`, resource key `InstanceIsLive` added to the neutral, fr
untouched, and seven locale resx) when the instance is live, mapped to HTTP 409 by the endpoint. Uses
`ApprovalsRequiredResolver.Default` like the analytics fix (service has no setting access). Trade-off for the owner (as
for Advanced Analytics): no emergency rollback of a live Exhaustive Adaptation without a checker. What is not covered by
this: a training run started from an approved instance still promotes its own best model when it finishes
(`DbTrialRecorder.InsertPromotedAsync`); the approved activation of the instance is the checker's consent to train and
promote, so I treated it as by design. Tests: service
`ChangingThePromotedModelOfALiveInstanceIsRefusedAndChangesNothingAsync`,
`DeactivatingTheActivePromotedModelOfALiveInstanceIsRefusedAndChangesNothingAsync`,
`RequestingTheStateAPromotedModelAlreadyHasIsANoOpEvenOnALiveInstanceAsync`; PenTest
`PromotionSwitchOfALiveInstance_IsRefusedWithConflictAndChangesNothingAsync`. Existing tests that built a live instance
(`Active = 1`) and then switched a promoted model now build an inactive one (`CreatePromotedTrialInstanceAsync` and
`CreatePromotedAsync` take `instanceLive`, default false). The SPA shows the generic failure for the 409 until the
shared key `exhaustive.promoted.instanceLive` is added (SHARED-TODO; ka, id, sv, ur, hi proposals added). Docs section
"Promotion rules" updated with a TODO (docs).

**5. The 2x variable count: proven by reading, not by running.** Path read end to end: `DataOptimiser.cs` runs
`while (true)`; whenever `trialVariables.Count < minVariableCount` (which happens after sensitivity analysis removes
variables, and on the first pass) it calls `EstablishRandomTrialVariablesAsync`, which calls
`VariableSelector.SelectVariablesAsync` for the same trial instance. That method first calls
`DeleteTrialVariablesAsync`, which only soft-deletes (`Deleted = 1`;
`ExhaustiveSearchInstanceTrialInstanceVariableRepository.cs:35-42`), then inserts a fresh row per drawn variable.
`DrawVariableIndices` clamps the draw to the number of variables, so on small datasets (the tests) every re-selection
draws all variables, i.e. exactly the same set again. The promoted trial's variable list is then loaded by
`GetExhaustiveSearchInstancePromotedTrialInstanceVariableQuery.ExecuteByExhaustiveSearchInstanceTrialInstanceIdAsync`
(sync, invoke and recall all use the tenantless constructor), whose `Deleted` filters sat inside `tenantRestricted` and
so did not apply. Each variable therefore appears once per selection round, exactly twice after one re-selection, while
the network was built from the last selection only (`InputsCount` = N). That reproduces "always exactly double, 48 vs
24, 28 vs 14, 38 vs 19" and nothing else in the path can (I checked the join: one `ExhaustiveSearchInstanceVariable` per
trial row, a left join to the categorical field on its id; the 2-rows-per-variable prescription table
`ExhaustiveSearchInstancePromotedTrialInstanceVariable` is not read by this query). Disproved alternatives: duplicate
population-statistics inserts (`StatisticsTargetResolver` selects the writer by exact repository type, one writer per
repository) and the join shape. Residual doubt: the first selection might not always be followed by a second; only a run
of `RecallChainTests` and `CategoricalTrainingTests` after the fix can close it, and
`ReselectingTrialVariables_NeverLeavesTheSoftDeletedRows...` pins the mechanism directly.

**Tests to run** (in addition to section 5): `AnchoredAssemblySerializationBinderTests`, `NetworkRoundTripTests`,
`ExhaustiveZScoreClipBoundResolverTests`, `ExhaustiveRecallTests`, `SyncBuildModelTests`, `TrainingRepairTests`,
`ExhaustiveSearchInstancePromotedTrialInstanceServiceTests`, `PenTestExhaustiveSearchInstancePromotedTrialInstance`,
`EntityAnalysisModel*` unaffected. The Playwright `exhaustiveResults` spec mocks the PUT and was not checked against the
409.

**Files written by scripts:** only the 105 + 8 resx earlier (the eight are the `InstanceIsLive` key appended to the
neutral and seven locale files of `ExhaustiveSearchInstancePromotedTrialInstanceResources`), the locale proposals JSON,
and docs edits; scripts live in the slice scratchpad subfolder `model-exhaustive`.

**Not covered (additions):** Rider lint; `Jube.Data` `Postgres.cs` untouched; whether any stored network in the wild
holds types from a second assembly; the training-time `PopulationStatisticsWriter` bound.
