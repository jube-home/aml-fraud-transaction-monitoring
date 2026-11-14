# Slice review: sanctions

Agent-mode review, 2026-11-08 (resumed after a session restart; nothing had been edited before it). Steps 0, 2, 3, 4, 5.
No dotnet build or test was run. npm eslint, prettier, vue-tsc and vitest were run on the slice's Vue files.

## 1. Summary and verdict

Verdict: READY AFTER OWNER DECISIONS and one serial dotnet pass. The slice has no URL-editing UI and no tenant manual
entries: it is a landlord-only CSV upload (SPA), an automatic loader (HTTP or directory, env-gated, off by default) and
a search. Real defects were found and fixed: the search endpoint had no server-side permission check, rate limit or
result cap; the loader's HTTP download used a default HttpClient (redirects to anywhere, no size or time cap, no SSRF
policy); a truncated download or read fault caused the run to delist every entry missing from the partial file; an
upload that failed was still reported as "loaded"; Unicode folding missed compatibility forms and invisible characters.
Follow-up pass (owner policy: bugs are fixed): homoglyph folding and entry token caching are now built (section 12).
Remaining decision: full transliteration between scripts (labelled design, recommendation in section 12).

## 2. Files reviewed (first-touch pull-ins)

| File                                                                                                                                                                                             | Read           | Notes                                                                                   |
|--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|----------------|-----------------------------------------------------------------------------------------|
| Jube.App/Vue/src/sanction/{api,location,state,types}.ts, sanctions/api.ts                                                                                                                            | whole          | types.ts reviewed here as asked                                                         |
| Jube.App/Vue/src/pages/SanctionsPage.vue, AdminSanctionsLoaderPage.vue, components/FileUpload.vue                                                                                                    | whole          | landlord gate is client-side for the loader page, server enforces                       |
| Jube.App/Vue/src/components/SanctionSearch.vue, SanctionsLoader.vue                                                                                                                                  | whole          | not claimed by any slice in slices.json (orphans); reviewed here, SanctionSearch edited |
| Jube.App/Endpoints/Repository/SanctionEntrySourceEndpoints.cs                                                                                                                                    | whole          | edited (size limit)                                                                     |
| Jube.Service/Repository/SanctionEntrySource/SanctionEntrySourceService.cs                                                                                                                        | whole          | edited; landlord-only (`RequiresLandlord`, `EnsureLandlord`-style check on every verb)  |
| Jube.Engine/Sanctions/{SanctionEntryFileImporter,LevenshteinDistance}.cs, Models/SanctionEntryFileImportResult.cs                                                                                | whole          | edited                                                                                  |
| Jube.Engine/BackgroundTasks/TaskStarters/SanctionsTaskStarter.cs                                                                                                                                 | whole          | edited (download, reconcile gating)                                                     |
| Jube.Data/Repository/SanctionEntryRepository.cs                                                                                                                                                  | whole          | no tenant column: shared landlord data                                                  |
| Jube.Service/Invoke/InvokeService.cs (Sanction method), Jube.App/Endpoints/InvokeEndpoints.cs (Sanction), Jube.Validations/Invoke/SanctionSearchRequestDtoValidator.cs, SanctionSearchRequestDto | relevant parts | unclaimed first-touch; edited                                                           |
| Jube.Case/Outbound/{GuardedHttpEndpoint,OutboundHostPolicy,PolicyCode}.cs                                                                                                                        | whole          | inherited; one public method added (see 4)                                              |
| Jube.Dto/Repository/SanctionEntrySource/SanctionEntrySourceDto.cs, Jube.Resources/SanctionEntrySourceResources(.cs/.resx)                                                                        | whole          | edited                                                                                  |
| Jube.Data/Security/PermissionValidation.cs                                                                                                                                                       | whole          | landlord holds every permission                                                         |
| New files                                                                                                                                                                                        |                | Jube.Engine/Sanctions/SanctionTextFolding.cs, SanctionListDownloader.cs                 |

Not claimed by me: EntityAnalysisModelSanction* (model-references). Engine hit handling (SanctionsExtensions) was read
only through its tests (`SanctionsExtensionsTests`, 12) and not edited.

## 3. Findings

| #  | Severity                     | Where                              | What                                                                                                                                               | Status                                                                                                                                                           |
|----|------------------------------|------------------------------------|----------------------------------------------------------------------------------------------------------------------------------------------------|------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| 1  | High (bug, authz)            | InvokeService.Sanction             | Any signed-in user could search; permission 29 was checked only by the SPA.                                                                        | Fixed: 403 without an active user holding 29                                                                                                                     |
| 2  | High (bug, SSRF)             | SanctionsTaskStarter               | `static HttpClient` default handler: redirects followed anywhere, proxy, no size, no timeout, no host policy.                                      | Fixed: GuardedHttpEndpoint policy, resolved-address check at connect, manual redirects (max 5, each re-checked), 128 MiB and 5 minute caps, buffered then parsed |
| 3  | High (bug, poisoned list)    | ImportAsync / reconcile            | A read fault or truncated body was swallowed and the partial hash set drove removal, delisting everything missing.                                 | Fixed: result carries `Completed`; reconcile skipped, import recorded Successful 0                                                                               |
| 4  | High (design guard)          | ReconcileRemovedAsync              | A complete but wrong or tiny file could delist the source.                                                                                         | Fixed with a guard: with 100 or more active entries a run removing more than 50% is skipped and warned (constants, not settings)                                 |
| 5  | Medium (bug)                 | Import service                     | A failed import returned 200 and the SPA said "Loaded".                                                                                            | Fixed: `ImportFailed` validation error (400)                                                                                                                     |
| 6  | Medium (bug)                 | Import endpoint                    | No request size limit.                                                                                                                             | Fixed: 64 MiB service refusal (`FilesTooLarge`) plus RequestSizeLimit metadata                                                                                   |
| 7  | Medium (bug)                 | InvokeService.Sanction             | No rate limit; unbounded entries (distance 50, ratio 1 returns most of a list).                                                                    | Fixed: ParseRateLimiter reuse (429, 300 per 60 s per user), entries capped at 1000 closest first, aggregations still over all                                    |
| 8  | Medium (bug, evasion)        | Tokenize and NormalizeElementValue | FormD only: full-width Latin, ligatures, Arabic presentation forms not folded; zero-width, soft hyphen and bidi controls kept inside tokens.       | Fixed: `SanctionTextFolding.Fold` (NFKD, strip Mn, Cf, controls, whitespace to space, lower case) on both sides                                                  |
| 9  | Low                          | SanctionSearch.vue                 | RTL names inside a LTR grid not isolated.                                                                                                          | Fixed locally with U+2068/U+2069; grid fix in SHARED-TODO                                                                                                        |
| 10 | Info                         | CSV parsing                        | TextFieldParser only: no XML, archive, JSON, so XXE, billion laughs, zip-slip and decompression bombs do not apply; row cap 5,000,000 added.       | Fixed/documented                                                                                                                                                 |
| 11 | Info                         | Tenant isolation                   | SanctionEntry and SanctionEntrySource have no tenant; there is no `                                                                                |                                                                                                                                                                  | !TenantRegistryId.HasValue` pattern in these repositories. Shared landlord data is the convention (all lookups are global, writes landlord-only). No tenant manual-entry feature exists. | Documented, owner to confirm |
| 12 | Info                         | Sources                            | HttpLocation editable only in the database (no endpoint), so never tenant controlled. DTO exposes Id and Name only.                                | Left                                                                                                                                                             |
| 13 | Info                         | SelectToken, BoundedJsonPath       | no `SelectToken` in the slice files (grep).                                                                                                        | none                                                                                                                                                             |
| 14 | Info                         | importer logging                   | three INFO lines per row on 19,000-row files.                                                                                                      | Left (tests assert log levels); noted                                                                                                                            |
| 15 | Medium (bug)                 | search cost                        | `CheckMultipartString` re-tokenised and normalised each entry on every search.                                                                     | Fixed in section 12 (tokens cached on the entry)                                                                                                                 |
| 16 | High (bug, screening bypass) | homoglyphs                         | Latin a and Cyrillic a were different letters, so a lookalike spelling evaded the list.                                                            | Fixed in section 12. Transliteration stays a design decision                                                                                                     |
| 17 | Low                          | DataGrid, FileUpload               | no cell dir isolation; no client size limit.                                                                                                       | SHARED-TODO (not my files)                                                                                                                                       |
| 18 | Info                         | change propagation                 | an upload is seen by the engine through the change poll (default 60 s); with `EnableSanctionLoaderChangePoll` False it waits `SanctionLoaderWait`. | documented                                                                                                                                                       |

## 4. Fixes made

Jube.Service/Invoke/InvokeService.cs (SanctionAsync, permission, limiter, cap), Jube.App/Endpoints/InvokeEndpoints.cs
(async endpoint), Jube.DynamicEnvironment (three keys, surgical), Jube.Case/Outbound/GuardedHttpEndpoint.cs
(`OpenGetAsync(Uri)`, surgical, no running agent in it), Jube.Engine/Sanctions (SanctionTextFolding,
SanctionListDownloader, importer Completed, row cap, removal guard, Levenshtein), SanctionsTaskStarter (downloader,
reconcile gating, per-file fault tracking, files kept when not completed), SanctionEntrySourceService and endpoint
(size, ImportFailed), SanctionEntrySourceResources (+FileTooLarge, ImportFailed), SanctionSearch.vue (isolation). Rider
lint on SanctionsTaskStarter found a real int-to-byte error, fixed in both places.

## 5. Test and pen-test matrix

| Layer          | Evidence read                                                                                                                                                                                                                                       | Status                                                                                                                      |
|----------------|-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|-----------------------------------------------------------------------------------------------------------------------------|
| Source service | `SanctionEntrySourceServiceTests` (16 names read): forbidden per verb, not authenticated, import empty, no file, unknown source, no delimiter skip, success row, audit and event                                                                    | +1 oversize refusal (new)                                                                                                   |
| Pen test       | `PenTestSanctionEntrySource` (4): Import refused for every non-landlord and writes nothing; hostile input refused; list. `PenTestUploadAntiforgery` (5, names), `PenTestInvokeController` route list includes Sanction (401)                        | Existing; no overlap added                                                                                                  |
| Invoke search  | `InvokeEndpointTests` Sanction_* (names read, 13): shape, validation, bounds, hostile strings, gates, 401, 405, change poll                                                                                                                         | +1 `Sanction_ByAUserWithoutTheViewSanctionPermission_Returns403Async`; `InitializeAsync` now grants 29 to the scaffold role |
| Engine         | `LevenshteinDistanceTests` 38, `SanctionAggregationCalculatorTests` 37, `SanctionEntryFileImporterTests` 22, `...ReconciliationTests` 7, `...SdnIntegrationTests` 12, `SanctionsChangePoll*` 10, `SanctionsExtensionsTests` 12 (names/headers only) | New: `SanctionTextFoldingTests` (7), `SanctionEntryFileImporterIntegrityTests` (2), +2 reconciliation guard tests           |
| Repository     | `Jube.Tests/Repository/Sanctions/*` (5 files, names)                                                                                                                                                                                                | covered                                                                                                                     |
| Front end      | vitest `location.spec`, `state.spec`; new `sanction/__tests__/api.spec.ts` (4) covering both API modules; playwright `sanction`, `sanctionsPage`, `sanctionsLoader` (22 tests, names/assert lines read)                                             | vitest 16 green; no playwright run                                                                                          |

Not tested: the downloader (needs a loopback listener; add a Unit test alongside
`EntityAnalysisModelHttpAdaptationTests` pattern), the 429 path and entry cap (need >1000 entries or a loop),
no-permission user through Archive of other Invoke tests.

Dotnet tests to run: `SanctionTextFoldingTests`, `SanctionEntryFileImporterIntegrityTests`,
`SanctionEntryFileImporterTests`, `LevenshteinDistanceTests` (Unit); `SanctionEntryFileImporterReconciliationTests`,
`SanctionEntrySourceServiceTests`, `InvokeEndpointTests`, `SanctionsChangePoll*Tests`, SDN integration (Service);
`PenTestSanctionEntrySource`, `PenTestUploadAntiforgery`, `PenTestInvokeController` (PenTest). Check that no other test
calls `/api/Invoke/Sanction` as a scaffold user without permission 29 (grep found only InvokeEndpointTests and the
pen-test route list).

## 6. Translations

SPA: pl and ar complete for all 51 owned keys (script check). ka, id, sv, ur, hi have no files in the repo; machine
proposals for all 51 keys (placeholders and plural pipes verified by script) are in
`review/sanctions.locale-proposals.json`. No new SPA keys added. Server: created
`SanctionEntrySourceResources.{pl,ar,ka,id,sv,ur,hi}.resx` (6 keys each, key parity with the neutral file and `{0}` in
FileTooLarge verified by parsing); `.fr.resx` untouched and lacks ImportFailed and FileTooLarge.
`EntityAnalysisModelSanctionResources` locales were created by model-references. Hard-coded English: none in slice Vue
(audit 0); `SanctionEntrySourceDto` has descriptions only, no titles. Remote assets: none. Fonts are shared platform.

## 7. Docs written and TODO markers

`docs/Configuration/Sanctions/SanctionsLoader/index.md` (new section "Download policy and safety limits"),
`SanctionSearching/index.md` (new "Access, limits and Unicode handling"), image links re-pointed to `sanctions-*.png` in
all three pages (19 TODO (screenshot) markers: 2, 9, 8). TODO (docs) markers in the loader page (4) and the search page
(3). `docs/Concepts/EnvironmentVariables` gets three rows; `docs/Testing/*.md` updated for the new tests. Grep `TODO(`
under docs/Configuration/Sanctions for lines.

## 8. Shared-code TODOs raised

Eight lines under `sanctions` in `review/SHARED-TODO.md` (InvokeService, DynamicEnvironment, GuardedHttpEndpoint,
DataGrid isolation, upload size, search pre-tokenisation, transliteration, fr.resx).

## 9. Manual test sheet

1. Landlord: Administration > Sanctions loader lists sources; upload a small CSV: "loaded"; upload an empty file and a
   .txt: refusals; a file over 64 MiB: refused. Non-landlord: refusal text, direct POST 403.
2. Search page with permission 29: Robert Mugabe at distance 0, 1; full-width "Ｒｏｂｅｒｔ Ｍｕｇａｂｅ" matches; text with
   zero-width spaces matches. Without 29: page refused and API 403 (also via curl).
3. Hammer the search over 300 times a minute: 429. Short string with distance 5 and ratio 1: at most 1000 rows, count
   aggregation true.
4. Enable the loader (`EnableSanctionLoader=True`) against a source: SDN downloads through the redirect; point a source
   at http://127.0.0.1 or the metadata address: refused and logged, import Successful 0; kill the connection
   mid-download: nothing delisted.
5. Re-upload a file with far fewer rows than a large source: removal skipped, warning logged.
6. Locales en, pl, ar, ka, id, sv, ur, hi on both pages; Arabic and Urdu names in the grid with Latin references; narrow
   width; dark theme; keyboard only.

## 10. Commit pathspecs

`slice_audit.py sanctions --paths` plus: Jube.App/Vue/src/components/SanctionSearch.vue, SanctionsLoader.vue,
Jube.App/Vue/src/sanction/ __tests__/api.spec.ts, Jube.Engine/Sanctions/ *,
Jube.Engine/BackgroundTasks/TaskStarters/SanctionsTaskStarter.cs, Jube.Service/Repository/SanctionEntrySource/*,
Jube.Resources/SanctionEntrySourceResources*, the new tests, the three hunks (InvokeService, InvokeEndpoints,
DynamicEnvironment, GuardedHttpEndpoint, EnvironmentVariables docs), docs/Configuration/Sanctions, docs/Testing hunks.
`slices.json` unedited: SanctionSearch.vue and SanctionsLoader.vue should be added to this slice.

## 11. Not covered

No build, no dotnet test, no playwright. Rider lint completed only for Jube.Engine/Sanctions/*, SanctionsTaskStarter
(after fix not re-linted), GuardedHttpEndpoint, the integrity test; timed out and remain unlinted: InvokeService.cs,
InvokeEndpoints.cs, SanctionEntrySourceService.cs (after the byte cast), SanctionEntrySourceEndpoints.cs,
SanctionTextFoldingTests, ReconciliationTests, InvokeEndpointTests, SanctionEntrySourceServiceTests. Not read: bodies of
the SDN integration and repository tests, `SanctionsExtensions` (model hit handling, cache keying) and
`SyncEntityAnalysisModelSanctionsExtensions`, version history, the agent tool catalogue entries, migrations. Engine
model-rule view of a hit and cache invalidation for rules was not audited beyond the existing tests' names. Unicode
handling for Devanagari: marks (virama) are stripped on both sides, which conflates some words; not assessed for false
positives. Maker-checker does not apply (no manual entry CRUD exists).

## 12. Follow-up pass (owner policy: bugs are fixed, not parked)

### 12.1 Homoglyph folding (bug, fixed)

`Jube.Engine/Sanctions/SanctionTextFolding.cs` gained an embedded confusables table (about 60 pairs: Cyrillic a c e o p
x y i j s h d q w l and capitals A B C E H I J K M O P S T X Y; Greek alpha omicron nu rho iota kappa upsilon chi and
capitals A B E Z H I K M N O P T Y X; dotless i, Latin alpha, script g). No remote data; the Unicode confusables file is
not in the repo. Rule per word: if every letter is Latin or in the table, table letters become Latin; a word with any
letter that has no Latin lookalike (Cyrillic g d l, Greek final sigma, any Arabic, Urdu, Georgian or Devanagari letter)
is left untouched, so legitimate names in those scripts are not corrupted. The same `Fold` is used when the list is
indexed (`NormalizeElementValue`) and when searches and engine lookups tokenise, so a Cyrillic lookalike entry in a list
is also found by the Latin query. `IsMixedScript(string)` is public for flagging; matching folds rather than rejects.
Digit lookalikes (0 and O, 1 and l) are intentionally not folded. Tests (new, Unit, in `SanctionTextFoldingTests`, now
22 cases): lookalike words fold, legitimate Cyrillic, Greek, Georgian, Arabic, Urdu and Devanagari words are unchanged,
mixed-script flag, a Cyrillic-lookalike spelling of "robert mugabe" is found at distance 0, a lookalike entry is found
by the Latin query, folding is idempotent.

Transliteration (full Arabic, Cyrillic, Georgian or Devanagari to Latin) stays a labelled design decision. Evidence: no
transliteration source exists in the repo (grep for ICU, Transliterator, ISO 9, BGN or similar found nothing).
Recommendation: treat a maintained transliteration source (ICU Any-Latin or per-list alias fields, since OFAC and UN
lists already publish Latin aka spellings and some native-script names) as a separate feature, with its own accuracy
tests per script; the lookalike fold does not substitute for it.

### 12.2 Per-search re-tokenisation (bug, fixed)

`SanctionEntry` now caches `Tokens` (folded, split, length greater than 1, distinct), computed lazily and reset when
`SanctionElementValue` is assigned. The load path replaces an entry object when it changes (`SanctionsTaskStarter` line
288 creates a new entry; removed ids leave the dictionary), so list updates invalidate the cache; there is one cache
path because both the Invoke search and the model rule lookup go through `LevenshteinDistance.CheckMultipartString`.
Stop tokens are applied per search against the cached array (only when the stop-token set is non-empty), so stop-token
changes need no invalidation. Results are unchanged:
`CachedEntryTokensGiveTheSameMatchesAsFreshEntriesAndAreRefreshedWhenTheValueChanges` compares cached against fresh
entries over six entries and five queries and checks refresh on reassignment. Only the cost of the per-entry fold was
removed; the per-search loop over entries remains (a length index is still a future option).

### 12.3 Permission 29 agreement (checked)

Menu: `menuTree.ts` Sanctions item `permissions: [29]`. Page: `SanctionsPage.vue` `hasPermission(me, 29)` shows
`sanctions-refused` otherwise; there is no router guard (the page does the check, as for other pages). e2e:
`sanctionsPage.spec.ts` stub defaults `permissions: [29]` and has a no-permission case (line 186); `sanction.spec.ts`
mocks the API only. Server: now 403 without 29. Seeded grants: the default administrator is a landlord and landlord
holds every permission (`PermissionValidation` selects every PermissionSpecification), so default installs work. Tenant
users who could use the API without 29 (never the SPA, which hides the menu and refuses the page) are now refused: this
is the intended behaviour change. Dotnet test fixtures: only `InvokeEndpointTests` calls the endpoint as a scaffold user
(the scaffold role has no permissions, so I grant 29 in `InitializeAsync`); `PenTestInvokeController` hits it for 401
only. Docs: TODO added to list View Sanction (29) in the roles documentation. No mismatch found beyond the test fixture.

### 12.4 Manual tenant entries (plain gap check)

The slice header says "manual entries". Evidence: the endpoint set is list sources and import file only; the SPA offers
only the CSV upload; `docs/Configuration/Sanctions/SanctionsLoader/index.md` promises "Manual upload" (a file), and
`docs/FAQ/SanctionsScreening.md` line 54 says "manual CSV upload"; the root `ManualSanctions.png` is a screenshot of the
old manual search page (viewed). Nothing promises a create-one-entry feature or tenant-owned entries. So no promised
feature is missing; if the owner wants tenant manual entries that is a new feature (tenant column, isolation in search,
approval), not a bug.

### 12.5 Lint

Linted clean at error level, one file at a time: SanctionEntrySourceService.cs, InvokeService.cs, InvokeEndpoints.cs,
SanctionsTaskStarter.cs, SanctionEntrySourceEndpoints.cs, SanctionTextFolding.cs, SanctionEntry.cs,
LevenshteinDistance.cs, SanctionTextFoldingTests.cs, GuardedHttpEndpoint.cs, SanctionEntryFileImporterIntegrityTests.cs,
plus the earlier Engine/Sanctions files. Timed out and remain unlinted: InvokeEndpointTests.cs,
SanctionEntryFileImporterReconciliationTests.cs, SanctionEntrySourceServiceTests.cs.

### 12.6 Dotnet tests to run (cumulative)

Unit: `SanctionTextFoldingTests`, `SanctionEntryFileImporterIntegrityTests`, `LevenshteinDistanceTests`,
`SanctionAggregationCalculatorTests`, `SanctionEntryFileImporterTests`, `SanctionsExtensionsTests`. Service:
`SanctionEntryFileImporterReconciliationTests`, `SanctionEntrySourceServiceTests`, `InvokeEndpointTests` (Sanction_*),
`SanctionsChangePoll*Tests`, `SanctionEntryFileImporterSdnIntegrationTests`. PenTest: `PenTestSanctionEntrySource`,
`PenTestUploadAntiforgery`, `PenTestInvokeController`. Watch the SDN integration and the existing Levenshtein tests for
any result changes from the NFKD and lookalike fold.
