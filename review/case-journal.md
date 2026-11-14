# case-journal review

## 1. Summary and verdict

Slice: Case Key Journal, event journal, replay (saved journal views and the Track map), activation cards and drawers.
Verdict: sound; tenant isolation, ArchiveKey as the only key index, keyset paging, hostile input and replay semantics
verified in code. Four defects fixed (SelectToken without regex timeout, a conditional-format regex without timeout, a
null-tenant escape, a payload dictionary crash), the `journal.merge.op.*` vitest failure fixed, 49 resx files created,
RTL bidi hardening applied. Open: server chart text is hard-coded English (shared TODO), 344 SPA keys await integration.
No build or test was run.

## 2. Files reviewed

Read in full or in the relevant parts: Jube.App/Endpoints/Query/CaseKeyJournalEndpoints.cs;
Jube.Service/Query/CaseKeyJournal/{CaseKeyJournalService, CaseKeyJournalPinService, CaseKeyJournalReplayService,
JournalMatcher, JournalLensReader, JournalTransactionReader, ArchiveFilter}.cs;
Jube.Data/Query/{GetCaseKeyJournalRowsQuery, GetCaseKeyJournalTransactionQuery, GetCaseJournalQuery, ArchiveKeyLookup/
*}; Jube.Data/Repository/{ArchiveKeyRepository, CaseEventRepository, CaseKeyJournalPinRepository}.cs;
Jube.Service/Query/ArchiveKeyFields/ArchiveKeyFieldResolver.cs;
Jube.Migrations/Scripts/MigrateCaseKeyJournalIndexesToArchiveKey.sql; Jube.Service/DynamicSql/DynamicSqlTableAccess.cs (
ArchiveKey, SessionCaseJournal entries); Jube.Data/Reporting/PostgresSqlValidator.cs (denied relations); Vue:
CaseKeyJournal, ActivationCard, ActivationsDrawer, ModelFacts, AdaptationPanel, TransactionDrawer, SvirfdPanel,
JournalCharts, JournalChartCard, JournalScope (operators), lanes.ts, api.ts. Not read line by line:
JournalFilterBuilder, JournalMap, JournalMapOptions, JournalSavedViews, JournalTag*, columns.ts, layout.ts, state.ts
(existing specs cover state, columns, layout, paint, saved, tags, filter).
First-touch pull-in: Jube.Data/Helpers/BoundedJsonPath.cs (new).

## 3. Findings

| #  | Severity        | File                                                                                                                                           | What                                                                                                                               | Status                                                                                                                                              |
|----|-----------------|------------------------------------------------------------------------------------------------------------------------------------------------|------------------------------------------------------------------------------------------------------------------------------------|-----------------------------------------------------------------------------------------------------------------------------------------------------|
| 1  | Medium (DoS)    | JournalTransactionReader:108, JournalLensReader:263,361, CaseKeyJournalPeerAccumulator:138, CaseKeyJournalService:476, GetCaseJournalQuery:151 | Plain SelectToken on tenant-configured XPath: `=~` filter with no regex timeout                                                    | Fixed with new Jube.Data.Helpers.BoundedJsonPath (250 ms, timeout selects nothing); ProcessCaseQuery:62 is cases-workspace's, raised on SHARED-TODO |
| 2  | Medium (DoS)    | GetCaseJournalQuery:177                                                                                                                        | Conditional-format `new Regex(pattern)` with no timeout (sibling JournalLensReader.Matches had 100 ms)                             | Fixed                                                                                                                                               |
| 3  | Medium          | CaseEventRepository.GetAsync/GetByIdAsync                                                                                                      | `                                                                                                                                  |                                                                                                                                                     | !tenantRegistryId.HasValue` made every case event visible to an unknown user, and linq2db null compare could match null-tenant models | Fixed (explicit non-null guard); methods have no callers today |
| 4  | Low             | CaseKeyJournalService.GetTransactionPayloadAsync                                                                                               | `ToDictionary` over XPath names threw on duplicate names, giving a 400                                                             | Fixed (distinct names)                                                                                                                              |
| 5  | Low (test gate) | JournalScope.vue                                                                                                                               | Template-literal key `journal.merge.op.${choice}` is invisible to locales.spec "uses every key"                                    | Fixed: literal key map. Keys are live, not stale                                                                                                    |
| 6  | Low (RTL)       | ActivationCard, ActivationsDrawer, ModelFacts, AdaptationPanel, TransactionDrawer, SvirfdPanel                                                 | Payload values, rule names, ids, hashes had no dir attribute                                                                       | Fixed: dir="auto" for user content, dir="ltr" for hashes, versions, numbers, dates                                                                  |
| 7  | Info            | Charts                                                                                                                                         | Server chart titles, axes, subtitles hard-coded English (~35 strings)                                                              | Left per owner: precise plan on SHARED-TODO, no refactor                                                                                            |
| 11 | Low             | CaseKeyJournalTrackService, CaseKeyJournalTagJobService                                                                                        | Filter built before workflow access check                                                                                          | Fixed (reordered)                                                                                                                                   |
| 8  | Info            | CaseKeyJournalKeyIndexRepository, poco                                                                                                         | Retired job, only referenced by a migration test                                                                                   | Left, owner deletes with the table                                                                                                                  |
| 9  | Info            | Per-row BoundedJsonPath and 100 ms regex                                                                                                       | Worst case 5000 rows x paths x timeout if an admin stores a hostile path                                                           | Design: admin-configured, bounded by limit 5000                                                                                                     |
| 10 | Design          | All journal services                                                                                                                           | Permission 1 (Case Management) gates reads and writes (tags, pins, saved views, tag jobs); consistent with the other case services | Owner decision, left                                                                                                                                |

Verified, no defect: every journal query joins UserInTenant to the model (Scope, TransactionQuery); pins/replays/tags
resolve the case through CaseSearchService.EverythingAsync (workflow accessible to caller); ArchiveFilter binds values
and takes SQL only from server-loaded fields; archive-id list is parsed to longs (max 500); limit max 5000, window max
3660 days; JournalMatcher pages by Id keyset; Svirfd lookup takes the IP from the stored archive, not the request;
replay here is saved views and a read-only Track, with no hold or dispatch stamping; no v-html in journal Vue; style
bindings use CSSOM properties. Backfill script reviewed: idempotent, resumable, advisory lock, dedupe before unique
index, validated sample; integer values with decimals are counted as CastFailed and skipped (note). Stale index paths:
IX_Archive_Journal_* and CaseKeyJournalIndexJob are gone from code; only the retired table repository remains. Dynamic
SQL: ArchiveKey, ArchiveKeyVersion and SessionCaseJournal are tenant-joined in DynamicSqlTableAccess; other journal
tables are not in the closed map.

## 4. Fixes made

Follow-up pass: BoundedJsonPath consolidated on Jube.Data.Helpers (Jube.Engine already references Jube.Data; the two
classes were behaviourally identical). Callers repointed with the fully qualified name in RequestFieldExtraction,
RequestJsonAccessor, InvocationContextBuilder, CategoricalExpansion and DataUtility (unused Extraction using dropped
from the last two), EntityAnalysisModelSampleService and the journal files. The Engine class and its test could not be
deleted (permission classifier refused rm); the same assertions were copied to
Jube.Tests/Data/Helpers/BoundedJsonPathTests.cs (namespace Jube.Test.DataHelpers). Owner deletes the two old files;
until then both compile and pass. Tag, TagJob and Track services read: Track and TagJob Preview/Submit built the journal
filter (workflow-to-model lookup, field loading, lane resolution) before the workflow access check, so a foreign
workflow guid produced validation errors that differed from not found, an oracle on another tenant's Archive Key fields.
Fixed by checking access first (Track, TagJob Preview and Submit). Tag service: model, lock, bounds and tag-name checks
verified, no defect. Remaining Vue files (saved views, filter builder, map, tag bar, controls, strip, peers) grepped for
v-html, eval and navigation, none; e2e cases.spec journal stubs read by grep only.

BoundedJsonPath (Jube.Data/Helpers) and the five call sites; Regex timeout; CaseEventRepository guard; payload
dictionary; JournalScope literal keys (prettier applied); dir attributes in six Vue files.

## 5. Test and pen-test matrix

Existing, read by name: PenTestCaseKeyJournal (Reads_AreAuthenticatedAndTenantIsolated, Charts_DrawAHostileName...,
Parameters_HostileValues..., Limits_AndWindowsAreBounded, Merge_AndFilterAreParsedDefensively,
CaseScopedReads_RefuseAForeignCase..., Writes_NeedAnAntiforgeryCheck, Activations_..., TransactionActivations_...),
PenTestCaseKeyJournalWrites (tags, pins, tag jobs, replays, saved layout, locked case, parallel writes),
PenTestCaseJournal, PenTestSessionCaseJournal; service tests under Jube.Tests/Service/Query/CaseKeyJournal (not re-read
in full); vitest in src/cases/journal/ __tests__ (activations, columns, facts, filter, jobs, layout, paint, peers,
saved, state, tags).
Added: Jube.Tests/Service/Query/CaseKeyJournal/JournalBoundedJsonPathTests.cs (4 tests, Unit); src/cases/journal/
__tests__/lanes.spec.ts (6) and api.spec.ts (3). The Engine BoundedJsonPathTests were not duplicated beyond the Data
helper.
To run: JournalBoundedJsonPathTests, Jube.Test.DataHelpers.BoundedJsonPathTests, the Engine request extraction,
exhaustive DataUtility and sample service tests, CaseKeyJournalTrackService and tag job service tests (order change:
foreign workflow now 404 before validation, check any test expecting 400 on a foreign workflow); vitest on
src/cases/journal, src/locales and src/story; PenTest category (CaseKeyJournal, CaseJournal) in the serial pass;
MigrateCaseKeyJournalIndexesToArchiveKeyScriptTests.

## 6. Translations

Server: created 49 .resx (pl, ar, ka, id, sv, ur, hi) for CaseByCaseKeyValue, CaseEventByCaseKeyValue, CaseJournal,
CaseKeyJournal, CaseNoteByCaseKeyValue, CaseWorkflowFormEntryByCaseKeyValue, SessionCaseJournal; key, placeholder, digit
and XML parity verified by script (two digit-check false positives, a thousands separator and a literal 365/7
formatting). CaseKeyJournalResources had no .fr.resx (untouched). SPA: en, pl, ar already match for journal.*; 344
journal keys proposed for ka, id, sv, ur, hi in review/case-journal.locale-proposals.json (keys relative to `journal.`
prefixed). Hard-coded English: chart text only.

## 7. Docs

docs/Configuration/CaseManagement/WorkingCaseKeyJournal/index.md lines 10 to 20: 5 TODO (docs) and 6 TODO (screenshot)
markers. The other slice docs (WorkingCaseJournal, WorkingCaseEventJournal, WorkingActivations,
Frontend/CaseKeyJournalDesign.md, CaseJournalActivations.md) were not edited: TODO for the docs pass.

## 8. Shared-code TODOs raised

See the case-journal lines at the end of review/SHARED-TODO.md (ProcessCaseQuery BUG, duplicate helper, charts, SPA
keys, dead repository, resx list, slices.json overlap).

## 9. Manual test sheet

Open a case, check the journal loads each lens; pivot via a key and return; add up to three lanes incl. at least and at
most on a numeric key; apply an Archive filter and see the count; toggle activations only and the window; select rows
and tag, then undo; tag all matching and cancel/undo the job; pin and pinned only; save, load and delete a view; open
the transaction drawer, Show payload (audited), Around this, What fired, Model facts, Svirfd compare; map view;
locked-to-another case refuses writes; user without permission 1 gets refusal; Arabic and Urdu layout of drawers;
hostile text in key value and tag names; narrow width and dark theme.

## 10. Commit pathspecs

Jube.App/Vue/src/cases/journal (incl. new __tests__), Jube.Data/Helpers/BoundedJsonPath.cs,
Jube.Data/Repository/CaseEventRepository.cs, Jube.Data/Query/GetCaseJournalQuery, Jube.Service/Query/CaseKeyJournal,
Jube.Tests/Service/Query/CaseKeyJournal/JournalBoundedJsonPathTests.cs, the 49 Jube.Resources resx, docs page, review
files.

## 11. Not covered

Rider re-lint (errors only) clean on JournalTransactionReader, PeerAccumulator, JournalLensReader,
RequestFieldExtraction, RequestJsonAccessor, DataUtility; timed out on InvocationContextBuilder, CategoricalExpansion,
EntityAnalysisModelSampleService, the new Data test and not re-run on the reordered Track and TagJob services. Earlier
Rider lint run on BoundedJsonPath, CaseEventRepository, GetCaseJournalQuery, the new test, JournalTransactionReader,
PeerAccumulator, JournalLensReader (only style hints; redundant qualifier fixed after the run, not re-linted);
CaseKeyJournalService.cs, JournalFilterBuilder, JournalMap and the vue files unlinted by Rider (eslint and prettier
clean). Not read: JournalLanes/Merge, Tag-job runner, Vue bodies only grepped (JournalFilterBuilder, JournalMap,
JournalMapOptions, tag bar), e2e cases.spec journey and Playwright, EXPLAIN plans, Tag/Pin concurrency, chart renderer
RTL (Jube.Charting). No build or test run.
