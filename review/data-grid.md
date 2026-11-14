# Slice review: data-grid

Agent-mode review, 2026-11-08. Steps 0, 2, 3, 4, 5. Nothing .NET was built or run (no infrastructure assigned). Georgian
(ka) late scope added and covered.

## 1. Summary and verdict

Verdict: READY AFTER OWNER ACTIONS. The grid is thin, server-described and defensive: sort columns are whitelists, take
is clamped, CSV and XLSX formula injection is neutralised and tested, the histogram endpoint is bounded and escaped and
tested. No injection or tenancy defect found in what I read. Real gaps: the server-side grid titles and headings have no
pl, ar or ka translation at all (plumbing exists, keys do not), the server-drawn chart text is English, JubeChart's SVG
sanitiser was incomplete (fixed), the expander glyph did not mirror under RTL (fixed), Georgian has no font in the
stack, the docs path in slices.json is the wrong page.

Before commit: owner manual test (section 9); owner applies shared TODOs; owner supplies screenshots; someone runs the
new .resx build (12 files, trivial) in CI.

## 2. Files reviewed (first-touch pull-ins)

| File                                                                                                                        | Read                                                                            | Notes                                                                                                              |
|-----------------------------------------------------------------------------------------------------------------------------|---------------------------------------------------------------------------------|--------------------------------------------------------------------------------------------------------------------|
| Jube.App/Vue/src/components/DataGrid.vue                                                                                        | whole                                                                           | Tabulator wrapper, pager, expander, local CSV and XLSX                                                             |
| PayloadGrid.vue, GridExportButtons.vue, PayloadFilterSelect.vue, StatisticsChartCard.vue, chartTheme.ts, dataGridColumns.ts | whole                                                                           | the last five are unowned files I own                                                                              |
| JubeChart.vue, ServerChart.vue                                                                                              | whole                                                                           | SVG mount, sanitiser                                                                                               |
| src/export/xlsx.ts, src/gridModel/{api,state,types}.ts                                                                      | whole                                                                           |                                                                                                                    |
| ValueGrid.vue, GridLevel.vue, GridStatistics.vue, DayBars.vue, valueGrid/*, statistics/*                                    | grepped for hard-coded text, physical left/right, v-html; not read line by line | see Not covered                                                                                                    |
| Jube.App/Endpoints/Query/GridModelEndpoints.cs                                                                              | whole                                                                           | two GETs, RequireAuthorization                                                                                     |
| Jube.Service/Query/GridModel/GridModelRegistry.cs, GridModelLocaliser.cs                                                    | whole                                                                           | attribute-driven; failures list; cached per (area, culture)                                                        |
| Jube.Dto/Query/GridModel/GridColumnDto.cs, Jube.Dto/Forms/GridEndpointAttribute.cs                                          | whole                                                                           | Take default 2000                                                                                                  |
| Jube.App/Endpoints/ChartsEndpoints.cs                                                                                       | PayloadStatistics group and handler (173-267)                                   | auth only; stateless render of client data                                                                         |
| Jube.Service/Charts/StatisticsHistogramChartRenderer.cs                                                                     | 25-100                                                                          | 200 bucket cap, 120 char title, finite check                                                                       |
| Jube.Service/HttpProcessingCounter/HttpProcessingCounterService.cs; Jube.Data/Repository/DockerHostMetricRepository.cs      | grep and ApplySort                                                              | sample of the 45 list services: `Math.Clamp(take,1,100000)`, sort is a `switch` whitelist, no string concatenation |
| Jube.Tests: GridModelRegistryTests, StatisticsHistogramChartRendererTests, MonitoringListTestBase users (names only)        | test names                                                                      |                                                                                                                    |

`--closure`: none. `--orphans` not needed. The 45 per-area list services and repositories belong to their own slices;
only the two samples were read. Migrations: none for this slice.

## 3. Findings

| #  | Severity          | Where                                     | What                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                              | Status                                                            |
|----|-------------------|-------------------------------------------|-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|-------------------------------------------------------------------|
| 1  | Medium            | JubeChart.vue `clean()`                   | Sanitiser removed script, foreignObject, iframe, object, embed and on* attributes but kept `href` and `xlink:href` (javascript: links in `<a>`) and SMIL `<animate>`/`<set>` that can set href. The SVG is server rendered and the renderer escapes text (test `TheThemeIsChosenByTheCallerAndAHostileTitleIsEscaped`), so defence in depth only.                                                                                                                                                                                                                 | Fixed, test added                                                 |
| 2  | Low (RTL)         | DataGrid.vue expander mark                | Glyph `›` did not mirror under rtl while pager chevrons do (`rtl:rotate-180`).                                                                                                                                                                                                                                                                                                                                                                                                                                                                                    | Fixed (`rtl:-scale-x-100` closed state; open rotate-90 unchanged) |
| 3  | Medium (locale)   | FormModelResources.pl/.ar                 | 110 data rows each, none is a grid key (`{area}.title`, `{area}.column.{field}`, filter keys per FormModelKeys). All 45 `[GridEndpoint]` areas show English titles, column headings, filter labels in pl and ar (and ka). Plumbing works (`GridModelLocaliser`, test `TheGridTitlesColumnsAndFiltersAreTranslated...`).                                                                                                                                                                                                                                           | Shared TODO                                                       |
| 4  | Medium (locale)   | StatisticsHistogramChartRenderer.cs 47-48 | "Value" and "Rows" axis titles hard-coded English in the server SVG; no culture parameter. Arabic/Georgian text anchors and font in SVG unverified.                                                                                                                                                                                                                                                                                                                                                                                                               | Shared TODO                                                       |
| 5  | Medium (Georgian) | assets/main.css 99-100                    | IBM Plex Sans and Mono have no Mkhedruli; Georgian falls to system-ui, which renders on common OSes but the mono grid cells (dates formatted with the ka locale) fall to ui-monospace which may lack Georgian. Georgian text is typically 10 to 30 percent longer: grid headers have no truncation class (no `truncate`/`ellipsis` in the slice files, Tabulator wraps headers), the pager `nav` is `flex-wrap` and the tab strip in GridLevel is `whitespace-nowrap shrink-0` (scrolls, not clips). No truncation defect found by reading; not visually checked. | Shared TODO (font), visual check in manual test                   |
| 6  | Low (RTL)         | dataGridColumns.ts 129                    | `mono` cells (IDs, IPs, dates, numbers) carry no bidi isolation; in Arabic rows Latin tokens and signs can reorder. A fix (`unicode-bidi: plaintext` class) would break the assertion at dataGridColumns.spec.ts:59 (`cssClass: 'font-mono tabular-nums'`).                                                                                                                                                                                                                                                                                                       | Left, owner decision (carried from login-audit TODO)              |
| 7  | Info              | Numeric alignment                         | `align: 'end'` goes through `resolveAlign`, which gives physical `right`/`left` by direction; pinned (`frozen`) columns rely on Tabulator's `textDirection`. Direction change rebuilds the table (watch on `direction`). Looks right; not seen running.                                                                                                                                                                                                                                                                                                           | Manual test                                                       |
| 8  | Info              | GridModelEndpoints.cs                     | Both GETs only `RequireAuthorization`; no permission. Schema names only.                                                                                                                                                                                                                                                                                                                                                                                                                                                                                          | Shared TODO / owner                                               |
| 9  | Info              | Export                                    | CSV and XLSX are browser-side from loaded (capped) rows; no server export endpoint exists, so no server tenant surface. Formula start `= + - @ \t \r` neutralised by `csvSafe` (plain numbers exempt) and XLSX writes text cells. Both tested. `+44 7700...` phone numbers get an apostrophe in CSV, by design.                                                                                                                                                                                                                                                   | None                                                              |
| 10 | Info              | Sort and take                             | UI sort is applied locally on capped rows and also sent to the server (re-query on sort). Server whitelists sort fields (DockerHostMetricRepository ApplySort switch). Take clamped to 100000 in HttpProcessingCounterService.                                                                                                                                                                                                                                                                                                                                    | None                                                              |
| 11 | Low (inventory)   | slices.json                               | `docs: docs/Navigation/ReportTable` is the Archive Key page. i18n keys `grid.*` and `csvExport.*` are mostly bench/demo keys. `chart.*` keys used by ServerChart are not in the slice's prefixes.                                                                                                                                                                                                                                                                                                                                                                 | Shared TODO                                                       |

No `.only`, `.skip`, `xit` in the four e2e specs or the slice vitest dirs (grep). No code comments added or touched.

## 4. Fixes made

1. `Jube.App/Vue/src/components/JubeChart.vue`: blocked `animate`, `set`, `animatetransform`, `animatemotion`; `href` and
   `*:href` attributes removed unless they start with `#`.
2. `Jube.App/Vue/src/components/__tests__/JubeChart.spec.ts`: test
   `strips external and javascript links and animation elements from the SVG` (vitest, 7 of 7 pass).
3. `Jube.App/Vue/src/components/DataGrid.vue`: expander glyph mirrors in rtl (constants `markClosed`, `markOpen`).
   DataGrid.spec 8 of 8 pass.
4. Created 12 `.resx`:
   `{PostgresIndexStatistics,PostgresStatementStatistics,PostgresTableStatistics,ExhaustiveSearchInstanceVariableStatistics}Resources.{pl,ar,ka}.resx`,
   copied from `.fr.resx` (same two keys `PermissionDenied`, `NotAuthenticated`), machine translation. These are the
   resource classes of the slice's "Statistics" services; the grid itself uses shared `FormModelResources`. Other grid
   list services (45) have their own resource classes owned by their slices.
5. Docs: `docs/Navigation/DataGrid/index.md` new, marker appended to `docs/Navigation/ReportTable/index.md`.
6. `review/SHARED-TODO.md`: 6 lines appended.

## 5. Test and pen-test matrix

| Layer                     | Evidence read                                                                                                                                                                                                                                          | Status                                                                                                                     |
|---------------------------|--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|----------------------------------------------------------------------------------------------------------------------------|
| Registry and DTO plumbing | GridModelRegistryTests: `EveryDecoratedGridRegistersWithoutAFailure`, `EveryGridEndpointIsARouteThatTakesTheParametersTheGridWillSend`, `EveryGridColumnHasATitleAndNoTwoColumnsShareAField`, localisation test, archiver, search-only, snapshot tests | Covered                                                                                                                    |
| Histogram renderer        | StatisticsHistogramChartRendererTests (5): bounds refusal (`AnEmptyOversizedOrNonFiniteHistogramIsRefused...`), hostile title escaped, colour refusal                                                                                                  | Covered                                                                                                                    |
| Histogram endpoint auth   | `RequireAuthorization` on group; no dedicated PenTest by name found                                                                                                                                                                                    | Gap, low (stateless, auth only); not added: overlaps PenTestAuthenticationSurface style anonymous checks, owner decision   |
| List sort, take, tenant   | Monitoring pen tests (MonitoringListTestBase users, PenTestObservabilityLists, PenTestPlatformMonitoring) by file names and grep for sortField/tenant; PenTestUserLogout read in login-audit                                                           | Covered by neighbours, not re-read here                                                                                    |
| GridModel endpoints       | No test calls `/api/GridModel` on the server (grep of Jube.Tests found only the registry host registration). Unauthenticated 401 and unknown area 404 uncovered                                                                                        | Gap, added to Not covered; one small PenTest recommended, not written because it needs the PenTest host which I cannot run |
| Export formula injection  | dataGridColumns.spec `csvSafe` (neutralises and exempts plain numbers; accessorDownload wired, line 283); xlsx.spec `buildWorkbook` asserts `=HYPERLINK("x")` is cell type 3 (string)                                                                  | Covered                                                                                                                    |
| Vitest                    | 111 passing across gridModel, valueGrid, statistics, export, components (run by me)                                                                                                                                                                    | Covered                                                                                                                    |
| Playwright                | grid 4, gridCsvExport 2, gridStatistics 6, valueGrid 13 tests counted; not run                                                                                                                                                                         | Counted only                                                                                                               |

## 6. Translations

SPA audit (script): owned prefixes 110 keys, pl and ar missing 0, empty 0; same-as-English only `statistics.bar` (a
placeholder pair, correct). All 74 `t()` keys exist in en.json. No hard-coded template strings; aria-labels all via
`t()` or the server model. Strings from the server model (area title, column titles, filter labels, search label default
"Search" in GridModelRegistry.cs line 232) are English fallbacks. Plurals: `grid.rows` uses `|` forms, Arabic/Polish
have more than three plural forms; `payloadGrid.rows` takes counts without plural, fine.

No `ka.json` exists. Proposed ka keys for the production keys (bench/demo keys `grid.*` demo text, `valueGrid.bench.*`,
`statistics.bench.*`, `csvExport.*` omitted; owner may decide to delete the bench pages), machine quality:

```
grid.pager.navigation=გვერდების ნავიგაცია
grid.pager.first=პირველი გვერდი
grid.pager.previous=წინა გვერდი
grid.pager.next=შემდეგი გვერდი
grid.pager.last=ბოლო გვერდი
grid.pager.pageSize=სტრიქონი გვერდზე
grid.pager.summary={from}–{to} / {total}
grid.pager.page=გვერდი {page} / {pages}
grid.empty=ფილტრს არც ერთი სტრიქონი არ შეესაბამება.
export.group=ექსპორტი
export.excel=Excel
export.csv=CSV
payloadGrid.from=დან
payloadGrid.to=მდე
payloadGrid.all=ყველა
payloadGrid.apply=გამოყენება
payloadGrid.clear=გასუფთავება
payloadGrid.rows={count} / {total} სტრიქონი
payloadGrid.capped=ნაჩვენებია {count} / {total} სტრიქონი; დანარჩენის სანახავად შეავიწროვეთ პერიოდი ან ფილტრები
payloadGrid.empty=ამ პერიოდში სტრიქონები არ არის.
payloadGrid.failed=სტრიქონების ჩატვირთვა ვერ მოხერხდა.
payloadGrid.retry=ხელახლა ცდა
payloadGrid.statistics=სტატისტიკა
payloadGrid.refresh=განახლება
payloadGrid.filters=ფილტრები
payloadGrid.loading=სტრიქონები იტვირთება
statistics.title=სვეტების სტატისტიკა
statistics.empty=ამ არჩევანისთვის სტატისტიკა არ არის.
statistics.noValues=მნიშვნელობები არ არის
statistics.chartOpen=სრული დიაგრამის ჩვენება
statistics.histogramLabel={min}-დან {max}-მდე განაწილება {bins} ინტერვალით, ყველაზე მეტი მნიშვნელობა {peak}-ის გარშემო ({count})
statistics.bar={bin}: {count}
statistics.col.column=სვეტი
statistics.col.distribution=განაწილება
statistics.col.min=მინ.
statistics.col.max=მაქს.
statistics.col.mean=საშუალო
statistics.col.median=მედიანა
statistics.col.standardDeviation=სტ. გადახრა
valueGrid.title=მნიშვნელობები
valueGrid.search=მნიშვნელობის პოვნა
valueGrid.add=მნიშვნელობის დამატება
valueGrid.approveAll=ყველა მოლოდინში მყოფის დამტკიცება
valueGrid.loading=მნიშვნელობები იტვირთება
valueGrid.loadFailed=მნიშვნელობების ჩატვირთვა ვერ მოხერხდა. განაახლეთ გვერდი.
valueGrid.empty=მიმდინარე ფილტრს მნიშვნელობები არ შეესაბამება.
valueGrid.statusUnavailable=დამტკიცების სტატუსი მიუწვდომელია. განაახლეთ გვერდი.
valueGrid.notePlaceholder=შენიშვნა (არასავალდებულო)
valueGrid.edit=რედაქტირება
valueGrid.delete=წაშლა
valueGrid.save=შენახვა
valueGrid.cancel=გაუქმება
valueGrid.approve=დამტკიცება
valueGrid.reject=უარყოფა
valueGrid.resubmit=ხელახლა გაგზავნა
valueGrid.retract=გამოთხოვა
valueGrid.saved=მნიშვნელობა შენახულია.
valueGrid.deleted=მნიშვნელობა წაშლილია, ელოდება დამტკიცებას.
valueGrid.approved=დამტკიცებულია.
valueGrid.rejected=უარყოფილია.
valueGrid.resubmitted=ხელახლა გაიგზავნა.
valueGrid.retracted=გამოთხოვილია.
valueGrid.refused=მოთხოვნა უარყოფილია.
valueGrid.notNumber={field} უნდა იყოს რიცხვი.
valueGrid.confirmTitle=წაიშალოს ეს მნიშვნელობა?
valueGrid.confirmBody=ის სიაში დარჩება წაშლის მოლოდინში, სანამ შემმოწმებელი არ დაამტკიცებს.
valueGrid.state.approved=დამტკიცებულია
valueGrid.state.awaiting=ელოდება დამტკიცებას
valueGrid.state.deletionAwaiting=წაშლა ელოდება დამტკიცებას
valueGrid.state.rejected=უარყოფილია
valueGrid.state.deletionRejected=წაშლა უარყოფილია
valueGrid.state.unavailable=სტატუსი მიუწვდომელია
valueGrid.col.expiry=წაშლის ვადა (UTC)
valueGrid.col.state=მდგომარეობა
valueGrid.col.approvals=დამტკიცებები
valueGrid.col.madeBy=ავტორი
valueGrid.col.checkerNote=შემმოწმებლის შენიშვნა
valueGrid.col.returnNote=დაბრუნების შენიშვნა
valueGrid.col.actions=მოქმედებები
chart.loading=დიაგრამა იხატება
chart.failed=დიაგრამის დახატვა ვერ მოხერხდა.
chart.retry=ხელახლა ცდა
chart.empty=დასახატი ჯერ არაფერია.
```

Server: 12 `.resx` created (section 4). Open: FormModelResources grid keys for pl, ar, ka (finding 3, shared);
`GridModelRegistry` fallbacks "Search" (line 232) go through the localiser only when a filter key exists. RTL audit of
slice files: grep found no physical `ml/mr/pl/pr/left/right` utilities in the slice's Vue files; positioning uses
`start-0`, `ms-auto`; pager chevrons carry `rtl:rotate-180`; expander fixed; export icons symmetric; `JubeChart` SVG is
server drawn (direction and anchors unverified). Numeric and ID cells: finding 6.

## 7. Docs written and TODO markers

New page `docs/Navigation/DataGrid/index.md` (nav_order 8). Markers (re-grep
`grep -n "TODO(" docs/Navigation/DataGrid/index.md`): screenshots `data-grid-payload-grid.png`,
`data-grid-statistics.png`, `data-grid-export.png`; TODO (docs) for whole-page read, window defaults, row cap wording,
mirrored arrows, file names and disabled buttons, per-language headings, bidi cells, server chart text, ValueGrid
ownership, ReportTable ownership. Also one TODO (docs) appended at the end of `docs/Navigation/ReportTable/index.md`
(Archive Key page, 7 Razor-era screenshots, no screenshot markers added there).

## 8. Shared-code TODOs raised

Six lines in `review/SHARED-TODO.md`: slices.json docs and keywords; FormModelResources grid keys pl/ar/ka; server chart
text; Georgian font in main.css; ka.json; GridModel endpoints permission.

## 9. Manual test sheet

1. Any payload grid (for example performance, sign-ins): load, From/To default window, each select filter, lookup
   filter, search, Apply, Clear, Refresh, Enter submits.
2. Row cap: area with more than `take` rows shows the capped message.
3. Sort each header, desc then asc, pager 10/25/50/100, first/previous/next/last disabled at ends, summary text.
4. Statistics panel: open, distribution cell, "Show the full chart", dark theme chart.
5. Export Excel and CSV: open both; a row with a name starting `=cmd`, `+1`, `-1+1`, `@x`, tab. CSV shows leading
   apostrophe, XLSX shows text. Buttons disabled at zero rows.
6. Permissions: user without the area's permission gets refusal not data, from the area endpoint; unauthenticated GET
   `/api/GridModel/X` is 401.
7. Tenancy: tenant A sees only A rows in each area you touch; landlord all.
8. Hostile: search `'; drop table`, 20000 characters, emoji, RLM/RLO, `<img onerror>` in a name; no execution, no 500;
   hand-craft an SVG response with javascript: link (devtools override) to see it stripped.
9. Locales en, pl, ar, ka: headings (server, expect English in pl/ar/ka until finding 3 is applied), pager, charts;
   Arabic: column alignment, sticky or frozen columns, sort icons, pager arrows and expander mirrored, IDs, IPs, dates,
   numbers inside Arabic rows, chart axes and legends (English text, check anchors). Georgian: glyphs in headers,
   buttons, mono date cells, chart text; look for clipped headers and wrapped pager.
10. Narrow width: horizontal scroll inside the grid, no page scroll; dark theme; keyboard only (expander button, pager,
    filters, export); refresh and deep link.
11. Two tabs: not applicable (read-only), except ValueGrid approvals (separate widget).

## 10. Commit pathspecs

`--paths` output: Jube.App/Vue/src/gridModel, src/valueGrid, src/statistics, src/export, components/DataGrid.vue,
PayloadGrid.vue, ValueGrid.vue, GridExportButtons.vue, GridLevel.vue, GridStatistics.vue, JubeChart.vue,
ServerChart.vue, DayBars.vue, Jube.App/Endpoints/Query/GridModelEndpoints.cs, docs/Navigation/ReportTable, the four e2e
specs.

Hand-picked: Jube.App/Vue/src/components/{PayloadFilterSelect.vue, StatisticsChartCard.vue, chartTheme.ts,
dataGridColumns.ts, dataGridTypes.ts} and their `__tests__` (DataGrid.spec, dataGridColumns.spec, JubeChart.spec,
chartTheme.spec, useSvgViewport.spec to be checked), `docs/Navigation/DataGrid/`, 12
`Jube.Resources/*Statistics*Resources.{pl,ar,ka}.resx`, GridModel registry, localiser, DTOs and tests (platform,
presumably committed with the platform).

## 11. Not covered

- No .NET build or test run; the 12 `.resx` are unverified by build (structure copied from `.fr.resx`, same key set).
- No Playwright run, no type-check run, no Rider lint.
- ValueGrid.vue, GridLevel.vue, GridStatistics.vue, DayBars.vue, valueGrid/ *, statistics/*: grep-level only (hard-coded
  text, physical direction, v-html, truncation classes). Not read line by line; `ValueGrid`'s backend
  (`EntityApproval/*`) belongs to another slice and was not read.
- Of the 45 `[GridEndpoint]` list services only HttpProcessingCounterService (take clamp) and DockerHostMetricRepository
  (sort whitelist) were read; I did not verify every repository's sort switch or tenant scope, nor that each list
  endpoint is covered by a pen test.
- No server test hits `/api/GridModel` (401, unknown area); not added because it needs the PenTest host.
- Server-drawn SVG text direction, Arabic shaping and Georgian font rendering in charts not checked. Visual Georgian
  truncation not checked, only by class inspection.
- `Jube.Charting` and the rest of `ChartsEndpoints.cs` (Exhaustive, Analytics, Backtest) are not mine and not read.
- The `docs/Navigation/ReportTable` content was not edited beyond one marker.

## 12. Late scope: Indonesian (id) and Swedish (sv)

Required set is now en, pl, ar, ka, id, sv. Server: 8 more `.resx` created
(`{PostgresIndexStatistics,PostgresStatementStatistics,PostgresTableStatistics,ExhaustiveSearchInstanceVariableStatistics}Resources.{id,sv}.resx`,
copied from `.fr.resx`, same two keys), so each of the four classes now has pl, ar, ka, id, sv (20 files in total).
Still open and shared: `FormModelResources` grid keys for all five new-or-old locales (finding 3), server chart axis
text (finding 4).

`id` collision check in the slice: the word "id" appears only as nested key segments (`grid.columns.id`,
`csvExport.col.id`, `valueGrid.bench.parentId`) and as the row key default `rowKey ?? 'id'` in DataGrid.vue; none
derives a locale from a key, file or property name. The risk sits in shared locale globbing and culture lists
(shell-auth, wire-and-api): a glob such as `*.id.*` or a lookup of a property named `id` against a locale list must be
checked there. Not verified by me.

Swedish truncation: grid and pager text is not clipped by any class (no `truncate` or ellipsis in the slice files;
Tabulator wraps headers; pager `nav` is `flex-wrap`; GridLevel tabs scroll). Long compounds likely in Swedish (for
example "Standardavvikelse", "Godkännandeförfrågningar", "Borttagningsförfrågan") are expected to wrap or widen, not
clip; the pager buttons are fixed icon buttons with the label only in aria-label. `minWidth` 120 (180 for dates) on
payload columns could make headers wrap on narrow widths. Not visually checked; add to the manual test (step 9) for id
and sv.

Proposed `id.json` keys (machine quality):

```
grid.pager.navigation=Penomoran halaman
grid.pager.first=Halaman pertama
grid.pager.previous=Halaman sebelumnya
grid.pager.next=Halaman berikutnya
grid.pager.last=Halaman terakhir
grid.pager.pageSize=Baris per halaman
grid.pager.summary={from}–{to} dari {total}
grid.pager.page=Halaman {page} dari {pages}
grid.empty=Tidak ada baris yang cocok dengan filter.
export.group=Ekspor
export.excel=Excel
export.csv=CSV
payloadGrid.from=Dari
payloadGrid.to=Sampai
payloadGrid.all=Semua
payloadGrid.apply=Terapkan
payloadGrid.clear=Bersihkan
payloadGrid.rows={count} dari {total} baris
payloadGrid.capped={count} dari {total} baris ditampilkan; persempit jangka waktu atau filter untuk melihat sisanya
payloadGrid.empty=Tidak ada baris dalam jangka waktu ini.
payloadGrid.failed=Baris tidak dapat dimuat.
payloadGrid.retry=Coba lagi
payloadGrid.statistics=Statistik
payloadGrid.refresh=Muat ulang
payloadGrid.filters=Filter
payloadGrid.loading=Memuat baris
statistics.title=Statistik kolom
statistics.empty=Tidak ada statistik untuk pilihan ini.
statistics.noValues=Tidak ada nilai
statistics.chartOpen=Tampilkan bagan lengkap
statistics.histogramLabel=Distribusi {bins} interval dari {min} sampai {max}, sebagian besar nilai mendekati {peak} ({count})
statistics.bar={bin}: {count}
statistics.col.column=Kolom
statistics.col.distribution=Distribusi
statistics.col.min=Min
statistics.col.max=Maks
statistics.col.mean=Rata-rata
statistics.col.median=Median
statistics.col.standardDeviation=Simpangan baku
valueGrid.title=Nilai
valueGrid.search=Cari nilai
valueGrid.add=Tambah nilai
valueGrid.approveAll=Setujui semua yang tertunda
valueGrid.loading=Memuat nilai
valueGrid.loadFailed=Nilai tidak dapat dimuat. Muat ulang halaman untuk mencoba lagi.
valueGrid.empty=Tidak ada nilai yang cocok dengan filter saat ini.
valueGrid.statusUnavailable=Status persetujuan tidak tersedia. Muat ulang untuk memeriksa.
valueGrid.notePlaceholder=Catatan (opsional)
valueGrid.edit=Ubah
valueGrid.delete=Hapus
valueGrid.save=Simpan
valueGrid.cancel=Batal
valueGrid.approve=Setujui
valueGrid.reject=Tolak
valueGrid.resubmit=Kirim ulang
valueGrid.retract=Tarik kembali
valueGrid.saved=Nilai disimpan.
valueGrid.deleted=Nilai dihapus, menunggu persetujuan.
valueGrid.approved=Disetujui.
valueGrid.rejected=Ditolak.
valueGrid.resubmitted=Dikirim ulang.
valueGrid.retracted=Ditarik kembali.
valueGrid.refused=Permintaan ditolak.
valueGrid.notNumber={field} harus berupa angka.
valueGrid.confirmTitle=Hapus nilai ini?
valueGrid.confirmBody=Nilai tetap terdaftar sebagai penghapusan tertunda sampai pemeriksa menyetujuinya.
valueGrid.state.approved=Disetujui
valueGrid.state.awaiting=Menunggu persetujuan
valueGrid.state.deletionAwaiting=Penghapusan menunggu persetujuan
valueGrid.state.rejected=Ditolak
valueGrid.state.deletionRejected=Penghapusan ditolak
valueGrid.state.unavailable=Status tidak tersedia
valueGrid.col.expiry=Kedaluwarsa penghapusan (UTC)
valueGrid.col.state=Status
valueGrid.col.approvals=Persetujuan
valueGrid.col.madeBy=Dibuat oleh
valueGrid.col.checkerNote=Catatan pemeriksa
valueGrid.col.returnNote=Catatan pengembalian
valueGrid.col.actions=Tindakan
chart.loading=Menggambar bagan
chart.failed=Bagan tidak dapat digambar.
chart.retry=Coba lagi
chart.empty=Belum ada yang dapat digambar.
```

Proposed `sv.json` keys (machine quality):

```
grid.pager.navigation=Sidnavigering
grid.pager.first=Första sidan
grid.pager.previous=Föregående sida
grid.pager.next=Nästa sida
grid.pager.last=Sista sidan
grid.pager.pageSize=Rader per sida
grid.pager.summary={from}–{to} av {total}
grid.pager.page=Sida {page} av {pages}
grid.empty=Inga rader matchar filtret.
export.group=Exportera
export.excel=Excel
export.csv=CSV
payloadGrid.from=Från
payloadGrid.to=Till
payloadGrid.all=Alla
payloadGrid.apply=Tillämpa
payloadGrid.clear=Rensa
payloadGrid.rows={count} av {total} rader
payloadGrid.capped={count} av {total} rader visas; avgränsa tidsfönstret eller filtren för att se resten
payloadGrid.empty=Inga rader i det här tidsfönstret.
payloadGrid.failed=Raderna kunde inte läsas in.
payloadGrid.retry=Försök igen
payloadGrid.statistics=Statistik
payloadGrid.refresh=Uppdatera
payloadGrid.filters=Filter
payloadGrid.loading=Läser in rader
statistics.title=Kolumnstatistik
statistics.empty=Ingen statistik för det här urvalet.
statistics.noValues=Inga värden
statistics.chartOpen=Visa hela diagrammet
statistics.histogramLabel=Fördelning över {bins} intervall från {min} till {max}, flest värden nära {peak} ({count})
statistics.bar={bin}: {count}
statistics.col.column=Kolumn
statistics.col.distribution=Fördelning
statistics.col.min=Min
statistics.col.max=Max
statistics.col.mean=Medelvärde
statistics.col.median=Median
statistics.col.standardDeviation=Standardavvikelse
valueGrid.title=Värden
valueGrid.search=Hitta ett värde
valueGrid.add=Lägg till värde
valueGrid.approveAll=Godkänn alla väntande
valueGrid.loading=Läser in värden
valueGrid.loadFailed=Värdena kunde inte läsas in. Uppdatera sidan och försök igen.
valueGrid.empty=Inga värden matchar det aktuella filtret.
valueGrid.statusUnavailable=Godkännandestatus är inte tillgänglig. Ladda om för att kontrollera.
valueGrid.notePlaceholder=Anteckning (valfritt)
valueGrid.edit=Redigera
valueGrid.delete=Ta bort
valueGrid.save=Spara
valueGrid.cancel=Avbryt
valueGrid.approve=Godkänn
valueGrid.reject=Avslå
valueGrid.resubmit=Skicka in igen
valueGrid.retract=Dra tillbaka
valueGrid.saved=Värdet har sparats.
valueGrid.deleted=Värdet har tagits bort, väntar på godkännande.
valueGrid.approved=Godkänt.
valueGrid.rejected=Avslaget.
valueGrid.resubmitted=Inskickat igen.
valueGrid.retracted=Tillbakadraget.
valueGrid.refused=Begäran nekades.
valueGrid.notNumber={field} måste vara ett tal.
valueGrid.confirmTitle=Ta bort det här värdet?
valueGrid.confirmBody=Det finns kvar som en väntande borttagning tills en granskare godkänner den.
valueGrid.state.approved=Godkänt
valueGrid.state.awaiting=Väntar på godkännande
valueGrid.state.deletionAwaiting=Borttagning väntar på godkännande
valueGrid.state.rejected=Avslaget
valueGrid.state.deletionRejected=Borttagning avslagen
valueGrid.state.unavailable=Status inte tillgänglig
valueGrid.col.expiry=Borttagning upphör (UTC)
valueGrid.col.state=Status
valueGrid.col.approvals=Godkännanden
valueGrid.col.madeBy=Skapad av
valueGrid.col.checkerNote=Granskarens anteckning
valueGrid.col.returnNote=Returanteckning
valueGrid.col.actions=Åtgärder
chart.loading=Ritar diagrammet
chart.failed=Diagrammet kunde inte ritas.
chart.retry=Försök igen
chart.empty=Det finns inget att rita ännu.
```

## 13. Late scope: Urdu (ur, RTL) and Hindi (hi, LTR Devanagari)

Required set is now en, pl, ar, ka, id, sv, ur, hi. Server: 8 more `.resx` created
(`{PostgresIndexStatistics,PostgresStatementStatistics,PostgresTableStatistics,ExhaustiveSearchInstanceVariableStatistics}Resources.{ur,hi}.resx`),
so each of the four classes has pl, ar, ka, id, sv, ur, hi (28 files). FormModelResources grid keys remain the open
shared gap for all seven non-English locales (finding 3).

RTL audit with ur: `directionFor` (i18n/index.ts 55) is shared; the grid takes `direction` from it, so ur gets the same
mirroring as ar (pager chevrons `rtl:rotate-180`, expander fix, `start-0`/`ms-auto`). Only registration of `ur` in the
right-to-left set is outside the slice; I did not verify that `ur` is in `rightToLeft`. Nastaliq clipping, by reading
classes (not visually checked): fixed-height controls in the grid are the risk: `btn btn-xs` action buttons and
`input-xs`/`select-xs` in dataGridColumns.ts and DataGrid.vue pager (`select-xs`, `btn-xs btn-square`), `badge badge-sm`
chips in dataGridColumns.ts, `input-sm`/`select-sm` filters in PayloadGrid.vue. daisyUI sets fixed heights on these, so
tall Nastaliq ascenders and descenders can clip, as can Devanagari shirorekha and stacked conjuncts at the xs size. No
explicit `line-height` or `leading-*` override exists in the slice files, so the fix is a shared one: a Nastaliq-capable
font (for example Noto Nastaliq Urdu) with `[dir=rtl]:lang(ur)` line-height and min-height overrides on `.btn-xs`,
`.badge-sm`, `.input-xs`, `.select-xs`, `.tabulator-cell`, `.tabulator-col`. Tabulator rows autosize to cell content, so
rows and headers are lower risk than the fixed-height controls. Added to SHARED-TODO.

Digits: grid numbers go through `useFormat` (`number`, `fixed`, `dateTime`) which uses Intl for the active locale, while
placeholders in `t()` strings (`{from}–{to} of {total}`, `{page}`) receive raw JS numbers rendered as Western digits in
DataGrid.vue's pager. If a locale's Intl default numbering (ar, ur) is Arabic-Indic, one value could mix systems (row
count via `number()` against page numbers raw). I did not check `useFormat` (shell-auth). Pin `numberingSystem: 'latn'`
there, which also covers hi (Western digits are the Indian business norm). Timestamps and IPs in mono cells are Latin
and should stay left-to-right (finding 6).

Devanagari: IBM Plex Sans and Mono have no Devanagari glyphs (same as Georgian, finding 5): system-ui fallback only; add
Noto Sans Devanagari to the stack and the SVG chart font. Chips and buttons: see the clipping note above.

Proposed `ur.json` keys (machine quality):

```
grid.pager.navigation=صفحہ بندی
grid.pager.first=پہلا صفحہ
grid.pager.previous=پچھلا صفحہ
grid.pager.next=اگلا صفحہ
grid.pager.last=آخری صفحہ
grid.pager.pageSize=فی صفحہ قطاریں
grid.pager.summary={total} میں سے {from}–{to}
grid.pager.page=صفحہ {page} از {pages}
grid.empty=فلٹر سے کوئی قطار مطابقت نہیں رکھتی۔
export.group=برآمد
export.excel=Excel
export.csv=CSV
payloadGrid.from=از
payloadGrid.to=تک
payloadGrid.all=سب
payloadGrid.apply=لاگو کریں
payloadGrid.clear=صاف کریں
payloadGrid.rows={total} میں سے {count} قطاریں
payloadGrid.capped={total} میں سے {count} قطاریں دکھائی گئیں؛ باقی دیکھنے کے لیے مدت یا فلٹر محدود کریں
payloadGrid.empty=اس مدت میں کوئی قطار نہیں۔
payloadGrid.failed=قطاریں لوڈ نہیں ہو سکیں۔
payloadGrid.retry=دوبارہ کوشش کریں
payloadGrid.statistics=اعداد و شمار
payloadGrid.refresh=تازہ کریں
payloadGrid.filters=فلٹر
payloadGrid.loading=قطاریں لوڈ ہو رہی ہیں
statistics.title=کالم کے اعداد و شمار
statistics.empty=اس انتخاب کے لیے کوئی اعداد و شمار نہیں۔
statistics.noValues=کوئی قدر نہیں
statistics.chartOpen=مکمل چارٹ دکھائیں
statistics.histogramLabel={min} سے {max} تک {bins} خانوں میں تقسیم، زیادہ تر قدریں {peak} کے قریب ({count})
statistics.bar={bin}: {count}
statistics.col.column=کالم
statistics.col.distribution=تقسیم
statistics.col.min=کم از کم
statistics.col.max=زیادہ سے زیادہ
statistics.col.mean=اوسط
statistics.col.median=وسطانیہ
statistics.col.standardDeviation=معیاری انحراف
valueGrid.title=قدریں
valueGrid.search=قدر تلاش کریں
valueGrid.add=قدر شامل کریں
valueGrid.approveAll=تمام زیر التوا منظور کریں
valueGrid.loading=قدریں لوڈ ہو رہی ہیں
valueGrid.loadFailed=قدریں لوڈ نہیں ہو سکیں۔ دوبارہ کوشش کے لیے صفحہ ریفریش کریں۔
valueGrid.empty=موجودہ فلٹر سے کوئی قدر مطابقت نہیں رکھتی۔
valueGrid.statusUnavailable=منظوری کی حیثیت دستیاب نہیں۔ جانچنے کے لیے دوبارہ لوڈ کریں۔
valueGrid.notePlaceholder=نوٹ (اختیاری)
valueGrid.edit=ترمیم
valueGrid.delete=حذف کریں
valueGrid.save=محفوظ کریں
valueGrid.cancel=منسوخ کریں
valueGrid.approve=منظور کریں
valueGrid.reject=مسترد کریں
valueGrid.resubmit=دوبارہ جمع کریں
valueGrid.retract=واپس لیں
valueGrid.saved=قدر محفوظ ہو گئی۔
valueGrid.deleted=قدر حذف ہو گئی، منظوری کی منتظر۔
valueGrid.approved=منظور ہو گئی۔
valueGrid.rejected=مسترد ہو گئی۔
valueGrid.resubmitted=دوبارہ جمع کر دی گئی۔
valueGrid.retracted=واپس لے لی گئی۔
valueGrid.refused=درخواست مسترد کر دی گئی۔
valueGrid.notNumber={field} کا عدد ہونا ضروری ہے۔
valueGrid.confirmTitle=یہ قدر حذف کریں؟
valueGrid.confirmBody=جب تک جانچنے والا منظور نہ کرے یہ زیر التوا حذف کے طور پر درج رہے گی۔
valueGrid.state.approved=منظور شدہ
valueGrid.state.awaiting=منظوری کی منتظر
valueGrid.state.deletionAwaiting=حذف منظوری کا منتظر
valueGrid.state.rejected=مسترد
valueGrid.state.deletionRejected=حذف مسترد
valueGrid.state.unavailable=حیثیت دستیاب نہیں
valueGrid.col.expiry=حذف کی میعاد (UTC)
valueGrid.col.state=حالت
valueGrid.col.approvals=منظوریاں
valueGrid.col.madeBy=بنانے والا
valueGrid.col.checkerNote=جانچنے والے کا نوٹ
valueGrid.col.returnNote=واپسی کا نوٹ
valueGrid.col.actions=کارروائیاں
chart.loading=چارٹ بنایا جا رہا ہے
chart.failed=چارٹ نہیں بن سکا۔
chart.retry=دوبارہ کوشش کریں
chart.empty=ابھی چارٹ کرنے کے لیے کچھ نہیں۔
```

Proposed `hi.json` keys (machine quality):

```
grid.pager.navigation=पृष्ठ नेविगेशन
grid.pager.first=पहला पृष्ठ
grid.pager.previous=पिछला पृष्ठ
grid.pager.next=अगला पृष्ठ
grid.pager.last=अंतिम पृष्ठ
grid.pager.pageSize=प्रति पृष्ठ पंक्तियाँ
grid.pager.summary={total} में से {from}–{to}
grid.pager.page=पृष्ठ {page} / {pages}
grid.empty=फ़िल्टर से कोई पंक्ति मेल नहीं खाती।
export.group=निर्यात
export.excel=Excel
export.csv=CSV
payloadGrid.from=से
payloadGrid.to=तक
payloadGrid.all=सभी
payloadGrid.apply=लागू करें
payloadGrid.clear=साफ़ करें
payloadGrid.rows={total} में से {count} पंक्तियाँ
payloadGrid.capped={total} में से {count} पंक्तियाँ दिखाई गईं; शेष देखने के लिए समय-सीमा या फ़िल्टर सीमित करें
payloadGrid.empty=इस समय-सीमा में कोई पंक्ति नहीं।
payloadGrid.failed=पंक्तियाँ लोड नहीं हो सकीं।
payloadGrid.retry=पुनः प्रयास करें
payloadGrid.statistics=आँकड़े
payloadGrid.refresh=ताज़ा करें
payloadGrid.filters=फ़िल्टर
payloadGrid.loading=पंक्तियाँ लोड हो रही हैं
statistics.title=कॉलम के आँकड़े
statistics.empty=इस चयन के लिए कोई आँकड़े नहीं।
statistics.noValues=कोई मान नहीं
statistics.chartOpen=पूरा चार्ट दिखाएँ
statistics.histogramLabel={min} से {max} तक {bins} बिन में वितरण, अधिकांश मान {peak} के आसपास ({count})
statistics.bar={bin}: {count}
statistics.col.column=कॉलम
statistics.col.distribution=वितरण
statistics.col.min=न्यूनतम
statistics.col.max=अधिकतम
statistics.col.mean=औसत
statistics.col.median=माध्यिका
statistics.col.standardDeviation=मानक विचलन
valueGrid.title=मान
valueGrid.search=मान खोजें
valueGrid.add=मान जोड़ें
valueGrid.approveAll=सभी लंबित स्वीकृत करें
valueGrid.loading=मान लोड हो रहे हैं
valueGrid.loadFailed=मान लोड नहीं हो सके। पुनः प्रयास के लिए पृष्ठ रीफ़्रेश करें।
valueGrid.empty=वर्तमान फ़िल्टर से कोई मान मेल नहीं खाता।
valueGrid.statusUnavailable=स्वीकृति की स्थिति उपलब्ध नहीं है। जाँचने के लिए पुनः लोड करें।
valueGrid.notePlaceholder=टिप्पणी (वैकल्पिक)
valueGrid.edit=संपादित करें
valueGrid.delete=हटाएँ
valueGrid.save=सहेजें
valueGrid.cancel=रद्द करें
valueGrid.approve=स्वीकृत करें
valueGrid.reject=अस्वीकार करें
valueGrid.resubmit=पुनः जमा करें
valueGrid.retract=वापस लें
valueGrid.saved=मान सहेजा गया।
valueGrid.deleted=मान हटाया गया, स्वीकृति लंबित।
valueGrid.approved=स्वीकृत।
valueGrid.rejected=अस्वीकृत।
valueGrid.resubmitted=पुनः जमा किया गया।
valueGrid.retracted=वापस लिया गया।
valueGrid.refused=अनुरोध अस्वीकार कर दिया गया।
valueGrid.notNumber={field} एक संख्या होनी चाहिए।
valueGrid.confirmTitle=यह मान हटाएँ?
valueGrid.confirmBody=जाँचकर्ता के स्वीकृत करने तक यह लंबित हटाव के रूप में सूचीबद्ध रहेगा।
valueGrid.state.approved=स्वीकृत
valueGrid.state.awaiting=स्वीकृति की प्रतीक्षा में
valueGrid.state.deletionAwaiting=हटाव स्वीकृति की प्रतीक्षा में
valueGrid.state.rejected=अस्वीकृत
valueGrid.state.deletionRejected=हटाव अस्वीकृत
valueGrid.state.unavailable=स्थिति उपलब्ध नहीं
valueGrid.col.expiry=हटाव की समाप्ति (UTC)
valueGrid.col.state=स्थिति
valueGrid.col.approvals=स्वीकृतियाँ
valueGrid.col.madeBy=बनाने वाला
valueGrid.col.checkerNote=जाँचकर्ता की टिप्पणी
valueGrid.col.returnNote=वापसी की टिप्पणी
valueGrid.col.actions=कार्रवाइयाँ
chart.loading=चार्ट बनाया जा रहा है
chart.failed=चार्ट नहीं बन सका।
chart.retry=पुनः प्रयास करें
chart.empty=अभी चार्ट करने के लिए कुछ नहीं है।
```
