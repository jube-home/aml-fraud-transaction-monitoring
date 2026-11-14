# tools-bench review

## Summary and verdict

Decision slice. No bench is a user feature, none is deleted (deletion is the owner's call), and none can be gated with a
bare `import.meta.env.DEV`: CI Playwright runs against a production build (`playwright.config.ts`:
`command: CI ? 'npm run preview' : 'npm run dev'`, baseURL 4173; `.github/workflows/jube-vue.yml` runs `build-only` then
`test:e2e` with `CI=true`), and about 40 spec files `goto('/tools...')` or `/scaffold...`. So all /tools and /scaffold
routes are gated by a build-time flag `benchesEnabled` (`src/benches.ts`: `DEV || VITE_ENABLE_BENCHES === 'true'`).
Verdict: gated, shipped build exposes neither the routes nor the menu entry. One design question left
(EnableMockEndpoints default).

## Files reviewed

Read: `src/pages/ToolsPage.vue`, `HomePage.vue`, `ApiPage.vue`, `components/ScaffoldLayout.vue`, `src/router/index.ts`,
`src/layout/menuTree.ts`, `src/layout/menu.ts` (partly), `playwright.config.ts`, `package.json`, `vite.config.ts`,
`.env.development`, `.github/workflows/jube-vue.yml`, `Jube.App/Startup.cs` (mock mapping), `Jube.DynamicEnvironment.cs`
(default), `MockRsaMfaEndpoints.cs`, `PenTestMockEndpointsGate.cs`, `docs/Frontend/ToolsInventory.md`. Greps only (not
line by line): the 32 bench components, `GridPage.vue`, `ThemePage.vue`, `src/sample`, e2e specs. No first-touch
pull-ins.

## Inventory

- `/tools?tool=<id>` (ToolsPage, auth required, no permission; menu entry `tools` visible to every signed-in user): 38
  ids, 32 bench components. Each bench hosts a product component and calls the ordinary product endpoints (each enforces
  its own server permission; owning slices audit those). Benches with direct `useApi` calls: DatasourceBuilder,
  ExhaustiveResults (read endpoints). No bench or scaffold page calls `/api/MockHttpAdaptation/*` or `/api/Mfa` (grep of
  src and e2e: none).
- `/scaffold`, `/grid`, `/api-demo` (calls `/api/AntiforgeryToken`), `/theme`: no auth guard at all (outside the
  `requiresAuth` parent), pure scaffolding.
- e2e dependents (per id): builderCoder, roleAllocation, permissionAllocation, approvalManager, datasourceBuilder,
  visualisationDashboard, tools, valueGrid, filterBuilders, evidenceTree, gridCsvExport, gridStatistics, modelStory,
  graphRecall, story, crud, exhaustiveRecord, hierarchy, performance, grid, integrity (also model-picker, engine-state
  etc. aliases), downloadUploadBench, sanctionsLoader, datasetUpload, askJoobyAnalytics, exhaustiveResults, modelSample,
  ruleTools, watcher, sanction, override; scaffolding, theme, api specs for /scaffold. Vitest: `app.spec`, `shell.spec`,
  `signIn.spec`, `returnAddress.spec`.
- No e2e: `graph` bench (GraphBench; also `/dev/graph` DEV-only route).

## Findings

| # | Sev                   | Where                                     | What                                                                                                                                                                                                                                                                                                                                                                                                                       | Status                                                                      |
|---|-----------------------|-------------------------------------------|----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|-----------------------------------------------------------------------------|
| 1 | High (attack surface) | router, menuTree                          | `/tools` and `/scaffold` mapped in production; `/scaffold` fully unauthenticated; Tools menu shown to all users                                                                                                                                                                                                                                                                                                            | Fixed: flag gate, 404 (notFound) in plain production build                  |
| 2 | Process               | e2e vs build                              | A DEV-only gate would break CI e2e (serves a production build)                                                                                                                                                                                                                                                                                                                                                             | Avoided: `.env.bench`, `npm run build-bench`, CI runs it after `build-only` |
| 3 | Low                   | build output                              | Plain production build still emits orphan chunks (ToolsPage, ThemePage etc., 206 assets; not referenced from `index-*.js`, verified no `/scaffold` string there). Unreachable, no data                                                                                                                                                                                                                                     | Left; not a leak (AGPL source)                                              |
| 4 | Design                | Startup.cs:612, DynamicEnvironment.cs:312 | `EnableMockEndpoints` defaults True: a default production deployment maps anonymous mock HTTP adaptation and mock RSA MFA (hard-coded client-key and OTP `12345678`). Docs and migration (seeded example model targets the mock) and test `TheFlagDefaultsOn_SoADeploymentKeepsTheDocumentedFixturesAsync` pin True; model-http-adaptation slice left it too. I could not run dotnet tests to change and pin a new default | Left as owner decision with evidence                                        |
| 5 | Info                  | slices.json                               | `src/sample` is production (`ModelSample.vue`), not a bench                                                                                                                                                                                                                                                                                                                                                                | SHARED-TODO                                                                 |
| 6 | Info (design)         | components                                | ModelPicker, RuleTools, GridLevel, HierarchyStack have no production host, only benches/tests                                                                                                                                                                                                                                                                                                                              | SHARED-TODO                                                                 |
| 7 | Info                  | i18n                                      | ToolsPage tool names and sources are hard-coded English; scaffold pages and benches use `t()` keys                                                                                                                                                                                                                                                                                                                         | Decision: DEV/bench-only, stay English, no RTL work                         |
| 8 | Info                  | remote assets                             | No remote loads in benches/scaffold (only `xmlns` and test literal URLs). Watcher bench map style is the watcher slice                                                                                                                                                                                                                                                                                                     | none                                                                        |

## Decision per bench

All 32 benches and 4 scaffold pages: gate behind `benchesEnabled` (not DEV alone). Deletion candidates, by evidence of
redundancy with real hosts (owner decides, files untouched): `/scaffold` pages (superseded by the real shell;
`app.spec`, `scaffolding.spec`, `theme.spec`, `api.spec` would go with them, theme.spec may be the only axe contrast
check of the theme: check first), `GraphBench` (no e2e; real `GraphPage` exists), and benches whose component now has a
real host with its own e2e (ApprovalManager, CaseFilterBuilder, ExhaustiveResults, Override, SanctionSearch,
SanctionsLoader, Watcher, DatasetUpload, Performance, DatasourceBuilder, VisualisationDashboard) once their specs move
to the real pages. Keep as test hosts until then: RoleAllocation, PermissionAllocation,
ModelPicker/RuleTools/Integrity/ModelSample (no production host).

## Fixes made

- New `Jube.App/Vue/src/benches.ts`, `Jube.App/Vue/.env.bench`; edited `src/router/index.ts` (both route groups spread under the
  flag), `src/layout/menuTree.ts` (Tools entry), `package.json` (`build-bench`, added to `ci`),
  `.github/workflows/jube-vue.yml` (`build-bench` before e2e).
- Verified by production build (`vite build` to scratch): `/scaffold` route absent from index chunk.

## Test and pen-test matrix

| Layer                               | Evidence                                                                                                                                                                                                                                                                   |
|-------------------------------------|----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| vitest (new)                        | `src/__tests__/benches.spec.ts`, 4 tests: on in dev; on in production with flag; off in plain production (no routes, no menu entry, `/tools`, `/tools?tool=crud`, `/scaffold`, `/scaffold/api-demo` resolve to `notFound`); flag value other than `true` is off. Ran: pass |
| vitest existing                     | `app.spec`, `shell.spec`, `signIn.spec`, `menu.spec`, `svirfdMenu.spec`: all 363 tests in `src/__tests__` and `src/layout` pass                                                                                                                                            |
| type-check, eslint on changed files | clean; `prettier --check` flags 4 files in `src/cases` (not mine)                                                                                                                                                                                                          |
| e2e                                 | Not run (needs dev server). Dev mode unchanged so should be unaffected; CI bench build is the one to watch                                                                                                                                                                 |
| Pen test                            | `PenTestMockEndpointsGate` (read, not run) pins both directions of the flag. No new pen test: no server change. A server-side test that a plain production SPA bundle 404s `/tools` is not possible here (SPA is a static host)                                            |

## Translations

None needed (DEV/bench only). No proposed keys.

## Docs

`docs/Frontend/ToolsInventory.md`: new section "Availability of /tools and /scaffold" with the build table; one
`TODO(docs)` marker (does a developer-only page belong in the published guide). No screenshots.

## Shared-code TODOs raised

Five lines appended to `review/SHARED-TODO.md` under `tools-bench` (slices.json corrections,
package.json/workflow/router/menuTree surgical edits, unhosted components, EnableMockEndpoints default).

## Manual test sheet

1. `npm run dev`: sign in, Tools menu present, `/tools` and `/scaffold` open. 2.
   `npm run build-only && npm run preview`: no Tools menu entry; `/tools`, `/scaffold`, `/scaffold/theme` show not found
   (signed in and out). 3. `npm run build-bench && npm run preview` with `CI=true npm run test:e2e`: specs pass. 4.
   Production deployment: confirm the image build uses `build-only`. 5. Decide EnableMockEndpoints default.

## Commit pathspecs

`Jube.App/Vue/src/benches.ts`, `src/__tests__/benches.spec.ts`, `.env.bench`, `src/router/index.ts` (hunks),
`src/layout/menuTree.ts` (hunk), `package.json` (two lines), `.github/workflows/jube-vue.yml`,
`docs/Frontend/ToolsInventory.md`, `review/tools-bench.md`.

## Not covered

Playwright not run; the Dockerfile/release build command that produces the shipped SPA was not located (Jube.App csproj
shows no `vite build` step), so item 4 of the manual sheet is unverified; Watcher map tile loading; per-endpoint
permission of each bench's backend (owned by other slices); `/api/AntiforgeryToken` anonymity not checked; dotnet tests
to run: `PenTestMockEndpointsGate` and `MockEndpointsGoldenTests` only if the default is changed.

## Addendum: type-check fix

`Jube.App/Vue/env.d.ts` now declares `ImportMetaEnv.VITE_ENABLE_BENCHES?: string` (no cast). `npm run type-check`, eslint,
prettier on the changed files and `benches.spec.ts` (4 tests) are clean.

## Removed (2026-11-09)

Owner instruction: drop the Temp Tools menu entry and all staging tools now that the app has full coverage. Removed: the
`/tools` route, `ToolsPage.vue`, all 32 `*Bench.vue` files, `BenchValueCard.vue`, `graphFixtures.ts`, the DEV-only
`/dev/graph` route, `nav.tools` and the bench-only locale keys in all nine locales, `benchesEnabled`,
`VITE_ENABLE_BENCHES` and `src/__tests__/benches.spec.ts`. The `/scaffold` pages stay (login and app specs redirect to
them) behind the renamed flag `scaffoldEnabled` / `VITE_ENABLE_SCAFFOLD` (`src/scaffold.ts`, `.env.scaffold`,
`npm run build-scaffold`). The e2e specs were ported to real pages or deleted where already covered. Components with no
real host were kept: `HierarchyStack`, `GridLevel`, `RuleTools` (with the Backtest* children) and `ModelPicker`. The
findings above are history and are left as written.

## Removed (2026-11-09, scaffold pages)

Owner decision: the `/scaffold` pages are deleted entirely. Removed: the `/scaffold` route group, `ScaffoldLayout.vue`,
`HomePage.vue`, `GridPage.vue`, `ApiPage.vue`, `ThemePage.vue`, the scaffold-only `FeatureCard.vue`, `NestedGroup.vue`
and `pages/sampleCases.ts`, `src/scaffold.ts` (`scaffoldEnabled`), `VITE_ENABLE_SCAFFOLD` (`env.d.ts`, `.env.scaffold`),
`npm run build-scaffold` and its CI step, `e2e/scaffolding.spec.ts`, `e2e/api.spec.ts`, and the scaffold-only locale
keys (`home.*`, `grid.*` except pager and rows, `api.*`, `theme.*`, `nav.home/grid/api/theme`, and the `rule.*` keys
used only by the theme page) in all nine locales. Specs that redirected to `/scaffold/*` now use `/svirfd` and
`/sanctions`; the theme, right-to-left and wordmark specs run on the sign-in page. The findings above are history and
are left as written.
