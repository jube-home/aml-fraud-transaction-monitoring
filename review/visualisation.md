# Slice review: visualisation

Agent-mode review, 2026-11-08 (resumed after a session restart; nothing from the first run had been edited). Nothing was
built, no dotnet test run. `vitest run src/dashboard src/datasource`: 6 files, 50 tests passed. Rider `lint_files` timed
out on every attempt, so ALL changed C# is unlinted. Scratch: `scratchpad/visualisation/` (translation txt files,
English dumps).

## 1. Summary and verdict

Verdict: READY AFTER FIXES ARE BUILT AND THE SERIAL PASS (list in section 5). Two real tenancy/allow-list bypasses were
found in the table-token rewrite and fixed (the main finding), plus a parent-move defect in two repositories, SQL text
leaking to viewers, and an unguarded deny-mode wrapper.

Viewer model (stated precisely): visibility of a registry, parameter or datasource is by role grant (registry role AND
datasource role) within the viewer's tenant. At run time the SQL executes with the allow-list built from the VIEWER's
table permissions (`BuildVisualisationAllowedTables(userName, permissionValidation.Validate)`), tenant-scoped by
templates keyed on the viewer's `UserInTenant`. Save-time validation and preview use the AUTHOR's permissions. So a
viewer cannot read a table they cannot query themselves, and the author cannot grant that. Execution needs 28 or 1;
authoring 31/32/33.

## 2. Files reviewed

Read in full: `Jube.Data/Reporting/PostgresSqlValidator.cs`, `Postgres.cs` (guard, caps, timeouts),
`Query/ExecuteVisualisationRegistryDatasourceCardQuery`, `GetByVisualisationRegistryDatasourceCommandExecutionQuery`,
`IntrospectVisualisationRegistryDatasourceQuery`, `CreateOrUpdate...WithValidationQuery`,
`GetVisualisationRegistrySummaryQuery`, `VisualisationRegistryRepository`, `VisualisationRegistryDatasourceRepository`,
`VisualisationRegistryParameterRepository`, `VisualisationRegistryDatasourceCommandExecutionService`,
`DatasourceCardAssembler`, `DatasourceChartRenderer`, `DatasourceChartText`, `VisualisationRegistryDatasourceMapper`,
`CommandExecution` endpoint; datasource service lines 262-300 and 800-1110 (introspect, preview, chart kinds, update
start); registry service 500-620; `DynamicSqlTableAccess` build methods. Vue: `JubeMarkup.vue`, `chartStylesheet.ts`,
`templateVocabulary.ts`, `dashboard/api.ts`, `state.ts`, `url.ts`, `VisualisationParameters.vue`,
`AdminVisualisationsPage.vue`, `datasource/api.ts`. Inherited, not re-reviewed: DataGrid.vue, JubeChart.vue,
i18n/index.ts, en.json, ProblemAlert.vue and the `--closure` list (case-record, form-model-engine, rule-authoring,
shell-auth, model-core files).

## 3. Findings

| #  | Sev             | Where                                                                              | What                                                                                                                                                                                                                                                                                                                                                                                                                                                                    | Status                                                                                                                  |
|----|-----------------|------------------------------------------------------------------------------------|-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|-------------------------------------------------------------------------------------------------------------------------|
| 1  | High (security) | PostgresSqlValidator.WalkAndSubstituteTables                                       | `WITH "UserRegistry" AS (SELECT 1) SELECT * FROM public."UserRegistry"`: CTE names were kept in the same dictionary as allowed tables with a null template (pass-through), the lookup ignored schemaname, so any schema-qualified reference to a table not on the allow-list (including password-bearing and cross-tenant tables) passed in allow-list mode (blockProtectedRelations is false there). Same in CollectTableReferences (permission collection skipped it) | Fixed: CTE names tracked separately; a qualified name is never a CTE; qualified pass-through only in public             |
| 2  | High (security) | same                                                                               | A CTE named after a table a template reads internally (UserInTenant, TenantRegistry, RoleRegistry, UserRegistry, *Role tables) was visible inside the substituted template subselect and replaced the tenancy/role check (`WITH "UserInTenant" AS (SELECT ...) SELECT * FROM "Archive"`), so tenant scoping was bypassable by the user's own SQL                                                                                                                        | Fixed: template relations are schema-qualified with `public` before substitution (pg_ views skipped)                    |
| 3  | Low             | PostgresSqlValidator.AssertSqlValueFunctionAllowed                                 | `current_role` was not blocked (user, catalog, schema were)                                                                                                                                                                                                                                                                                                                                                                                                             | Fixed                                                                                                                   |
| 4  | Medium          | Postgres.ExecuteLimitedByNamedParametersAsync, IntrospectAsync (deny mode)         | Raw SQL was only validated after being wrapped in `SELECT * FROM (...) b LIMIT n`, so a payload could close the wrapper and append a second FROM-item                                                                                                                                                                                                                                                                                                                   | Fixed: raw SQL asserted stand-alone first (this edit shares the file with an owner diff already present in Postgres.cs) |
| 5  | Medium          | VisualisationRegistryDatasource update (CreateOrUpdate...Query)                    | UpdateAsync wrote the DTO's VisualisationRegistryId: a datasource could be moved under another registry of the same tenant (different role gating)                                                                                                                                                                                                                                                                                                                      | Fixed: pinned to the stored row, introspection uses it                                                                  |
| 6  | Medium          | VisualisationRegistryParameterRepository.UpdateAsync                               | Same move defect                                                                                                                                                                                                                                                                                                                                                                                                                                                        | Fixed                                                                                                                   |
| 7  | Medium          | VisualisationRegistryParameterRepository                                           | `                                                                                                                                                                                                                                                                                                                                                                                                                                                                       |                                                                                                                         | !tenantRegistryId.HasValue` null-tenant escape through an unused tenantless ctor on an `int?` | Fixed: int, escape and ctor removed (no callers; tests use the other ctors) |
| 8  | Medium          | VisualisationRegistryDatasourceService.GetByVisualisationRegistryIdActiveOnlyAsync | Viewers with only 28 or 1 received the datasource `Command` (SQL text, table names, literals); the dashboard SPA never reads it and the card contract says the SQL is never returned                                                                                                                                                                                                                                                                                    | Fixed: Command blanked unless the caller holds 33                                                                       |
| 9  | Design          | Directory and Summary                                                              | Need no permission, only a role grant (pinned by an existing test); summary shows last-run user names to granted viewers                                                                                                                                                                                                                                                                                                                                                | Left, SHARED-TODO design line                                                                                           |
| 10 | Medium          | Postgres.cs                                                                        | (now FIXED, see section 12) Statement timeout 30 s (`default_transaction_read_only`, `statement_timeout`, `lock_timeout` in connection options plus `BEGIN READ ONLY; SET LOCAL`), card cap 5000 rows via `LIMIT n+1`, other paths 100000 rows. No byte cap: giant text values (`format('%500000000s')`, repeated concatenation in CTEs) are bounded only by PG's 1 GB value limit                                                                                      | Fixed: per-cell and per-result byte caps, `format` denied                                                               |
| 11 | Info            | CommandExecutionService.ExecuteAsync (agent tool only)                             | `(int)SelectToken("id")`, duplicate ids and unparseable values throw 500 rather than 400; constant paths so no regex risk; stores `ex.ToString()` in the execution log Error column                                                                                                                                                                                                                                                                                     | Left                                                                                                                    |
| 12 | Info            | `/api/ChartsEndpoints.cs`                                                          | Contains no datasource route; the other chart families in it belong to exhaustive, watcher and analytics                                                                                                                                                                                                                                                                                                                                                                | Not reviewed here                                                                                                       |

Checked and found sound: function deny list and AST walk (FuncCall, SQLValueFunction, IntoClause, LockingClause, any
`*Stmt`), stacked statements (single-statement assertion), comments and unicode escapes (parser decodes identifiers),
LATERAL and sub-selects (generic reflection walk), UNION branches, named-parameter smuggling (`$n` input refused,
descending restore), read-only transaction, per-viewer allow-list built from the viewer, card parameters limited to the
registry's own parameter ids, case context id checked against tenant, no `v-html` anywhere in the slice (JubeMarkup
re-sanitises server markup with a tag and attribute allow-list), no remote asset loads in the slice (only navigational
links elsewhere), no physical left/right CSS classes, no export path.

## 4. Fixes made

`Jube.Data/Reporting/PostgresSqlValidator.cs` (findings 1-3); `Jube.Data/Reporting/Postgres.cs` (4);
`Jube.Data/Query/CreateOrUpdateVisualisationRegistryDatasourceWithValidationQuery/...Query.cs` (5);
`Jube.Data/Repository/VisualisationRegistryParameterRepository.cs` (6, 7);
`Jube.Service/Repository/VisualisationRegistryDatasource/VisualisationRegistryDatasourceService.cs` (8).

## 5. Test and pen-test matrix

Existing coverage cited (read headers and test names): `PenTestVisualisationRegistry*`,
`PenTestVisualisationRegistryDatasource(CommandExecution)` (DynamicSql), `PenTestSqlGate` over `SqlAttackCatalogue`,
`DynamicSqlCommonTableExpressionTests`, `DynamicSqlNamedParameterRewriteTests`, `DynamicSqlTemplateSchemaSweepTests`,
`VisualisationRegistry*ServiceTests` (tenant isolation, permission per verb, audit, events, concurrency),
`VisualisationRegistryDatasourceCardServiceTests`, `ChartSafetyTests`/`InjectionTests` (SVG escaping), Playwright
`visualisationDashboard`, `visualisationPages`, `datasourceBuilder`.
Added (unrun):

- `DynamicSqlCommonTableExpressionTests`:
  `ASchemaQualifiedNameIsNeverACommonTableExpressionSoItCannotShadowAForbiddenTable` (theory, 4 cases),
  `CollectedTableReferencesIncludeASchemaQualifiedNameThatSharesAnExpressionName`,
  `AnInnerExpressionMayReuseAnOuterExpressionName`, `CurrentRoleIsABlockedValueFunction`,
  `AnExpressionNamedAfterATableATemplateReadsCannotCaptureTheTemplateTenancyCheck` (theory, 4 cases).
- `SqlAttackCatalogue.ProtectedReads`: two shadow-CTE entries (run through every variant by `PenTestSqlGate`).
- `VisualisationRegistryDatasourceServiceTests`: `UpdateCannotMoveADatasourceToAnotherRegistryOfTheSameTenantAsync`,
  `GetByVisualisationRegistryIdActiveOnlyWithholdsTheSqlFromACallerWhoCannotAuthorAsync`.
- `VisualisationRegistryParameterServiceTests`: `UpdateCannotMoveAParameterToAnotherRegistryOfTheSameTenantAsync`.
- Changed: `DynamicSqlNamedParameterRewriteTests` expectation now `FROM public."ExampleCustomerCaseManagement" t0`;
  `DatasourceChartLocalisationTests.Cultures` now pl, ar, ka, id, sv, ur, hi.
  Must be run (own Postgres, serial): `DynamicSqlCommonTableExpressionTests`, `DynamicSqlNamedParameterRewriteTests`,
  `DynamicSqlTableAccessTests`, `DynamicSqlTemplateSchemaSweepTests`, `DynamicSqlTablePermissionParityTests`,
  `PostgresSqlValidatorTests`, `VisualisationRegistryDatasourceServiceTests`,
  `VisualisationRegistryParameterServiceTests`, `VisualisationRegistryServiceTests`,
  `VisualisationRegistryDatasourceCardServiceTests`, `VisualisationRegistryDatasourceCommandExecutionServiceTests`,
  `DatasourceChartLocalisationTests`, `Seeded/CaseSql*` tests that assert rewritten text, then `Category=PenTest`
  filtered to `PenTestSqlGate` and `PenTestVisualisation*`. Watch for any other test that asserts exact rewritten
  template text (grep found only the one changed).

## 6. Translations

Server: created 61 resx: the eight `VisualisationRegistry*Resources` classes (pl, ar, ka, id, sv, ur, hi = 56) and
`DatasourceChartResources` ka, id, sv, ur, hi (5; pl and ar existed; SourceHash comments copied from pl so the
stale-hash test holds). Key parity, `{n}` placeholder parity and non-empty values verified by script, 0 problems.
Machine translation, POC quality. SPA: `review/visualisation.locale-proposals.json` holds ka, id, sv, ur, hi for all 107
owned keys (visualisation, datasource, adminVisualisations, chart; placeholders and plural pipes verified). pl and ar
already complete per the audit. Hard-coded English: none in the slice Vue files; server chart SVG text (axis and tile
labels) remains English (SHARED-TODO, large refactor).

## 7. Docs and TODO markers

Edited `docs/Configuration/Visualisations/{Registry,Parameters,SecurityPolicy,Logging}/index.md` (25 `TODO(screenshot)`
markers above every existing image, 13 `TODO(docs)` markers appended), `Visualisations/index.md` (1 TODO (docs)),
`docs/Frontend/DatasourceTemplateDialect.md` (2 TODO (docs) appended), `docs/Testing/AskJooby.md`,
`ServiceRepositories.md`, `ChartingAndDiagrams.md` (test catalogue). Pages GenerateChart, GenerateHTML, GenerateMap and
EmbedSQLGenerateGrid, Recall have no markers and were not read.

## 8. Shared-code TODOs raised

Four lines in `review/SHARED-TODO.md` (validator fix impact, case SQL inheritors, directory design, English chart text).

## 9. Manual test sheet

Administration > Visualisations: create, edit, lock, delete a registry, parameter and datasource; role grants on each.
Datasource builder: valid SELECT, invalid SQL (position shown), `WITH "UserInTenant" AS (...) SELECT * FROM "Archive"`
returns only your tenant's rows, `WITH "UserRegistry" AS (SELECT 1) SELECT * FROM public."UserRegistry"` is refused,
`SELECT current_role` refused; edit a datasource via API with another registry id and confirm it stays. As a user
holding only 28: directory lists granted registries, tiles render, network response for the tile list has no `command`.
Parameter types, required flag, URL query overrides, date and time zones, locale en, pl, ar, ka, id, sv, ur, hi (RTL for
ar, ur; chart SVG forces direction ltr), narrow width, dark theme, hostile titles and labels.

## 10. Commit pathspecs

`slice_audit.py visualisation --paths` plus: `Jube.Data/Reporting/PostgresSqlValidator.cs`, `Postgres.cs` (hunks: the
two AssertSelectOnly additions only; the stream hunk is owner work), the CreateOrUpdate query,
`VisualisationRegistryParameterRepository.cs`, the datasource service, the 61 new resx under `Jube.Resources`, the
changed tests above, docs above, `review/visualisation*`.

## 11. Not covered

Rider lint of all changed C# (timed out). Not read in depth: the datasource service CRUD sections (lines 1-262, 300-800,
1110-end), `VisualisationRegistryService` and `ParameterService` CRUD, the role services and endpoints, the 12 pen-test
bodies (names only), `DatasourceChartKinds`/renderers beyond the card assembler, `DatasourceBuilder.vue` and sibling
bodies, `DashboardTile.vue`, `VisualisationDashboard.vue`, directory and story components, the Playwright specs, the
five docs pages without markers, execution-log storage of exception text. Whether the report connection string maps to a
read-only role was not verified (the read-only transaction and AST gate are the enforced controls; `jube_app` has no
DELETE grant but does hold write grants). Byte-for-byte deparse expectations after schema qualification are unverified
until the tests run.

## 12. Second pass (owner policy: build a larger corpus, close gaps)

### 12.1 Adversarial corpus and the bypasses it found

New `Jube.Tests/AskJooby/PostgresSqlValidatorCorpusTests.cs` (Category Unit, table-driven, about 20,000 cases):
non-SELECT statements and stacked or malformed spellings (about 90), about 95 denied functions x 4 spellings (plain,
upper, schema-qualified, quoted) x 22 positions plus unicode-escape and comment spellings, six identity value functions,
13 protected tables x about 60 positions and spellings (including U&"..." identifiers, TABLE, ONLY, TABLESAMPLE,
LATERAL, UNION/INTERSECT/EXCEPT, every subquery position, CTE shadows) on both the deny path and the allow-list path,
catalogue and information_schema reach in three modes, 55 allow-list rejections and 25 allow-list successes (tenancy
relation stays qualified), dollar-quote and comment cases that hide semicolons, length, NUL, depth, long chains,
positional parameter refusal and named parameter round trip. Read against the code (not run). Bypasses found by reading
while building it:
| # | Sev | What | Fix |
|---|---|---|---|
| A | High | CTE names compared case-insensitively while PostgreSQL compares them exactly:
`WITH userregistry AS (SELECT 1) SELECT * FROM "UserRegistry"` treated the real table as a CTE and skipped the
allow-list (same in CollectTableReferences, so the permission list missed it) | CTE name sets are now Ordinal |
| B | Medium | `FROM "Archive" AS a (x, y)` lost the user's column alias list when the template replaced the table | the
user's alias is cloned onto the subselect |
| C | Medium | Native-parser recursion DoS: a left-nested chain of thousands of operators or UNIONs could recurse in
libpg_query and the walkers; only parentheses were bounded | refused beyond 4000 operator characters or 200 set
operations (outside quotes and comments) |
| D | Low | Memory and CPU time bombs still allowed: `format('%999999999s')`, `crypt` with a large cost, `gen_salt`,
plus metadata leaks `current_query`, `row_security_active`, `obj_description`, `col_description`, `shobj_description`,
`acldefault`, `aclexplode` | added to the denied names (`format` is now refused for every dynamic-SQL caller; Ask Jooby
analyses that used it must use `to_char` or concatenation) |
No other bypass was found for: data-modifying CTEs (INSERT, UPDATE, DELETE, MERGE, MATERIALIZED, nested, recursive),
SELECT INTO, TABLE, VALUES, COPY (including PROGRAM), EXPLAIN ANALYZE, every FOR UPDATE and FOR SHARE form,
PREPARE/EXECUTE, DO, CALL, SET ROLE, SET SESSION AUTHORIZATION, RESET, SHOW, set-returning and system functions,
schema-qualified and quoted identifiers, unicode-escaped identifiers (the parser decodes them), dollar strings and
comments, stacked statements, views (the schema has no views), aliases equal to template relation names (template
relations are qualified), case folding, parameter smuggling. Trailing-newline `$` anchors are not in these files
(FilterSqlText is exhaustive's, covered by `FilterSqlTextTests`). The corpus is a refusal corpus: a case that the parser
itself rejects also counts as refused.

### 12.2 Database-level read-only (confirmed, plus new settings)

Confirmed in `Postgres.cs`: every execution path calls `BeginGuardAsync`
(`BEGIN READ ONLY; SET LOCAL statement_timeout; SET LOCAL lock_timeout`), the connection string carries
`default_transaction_read_only=on`, `statement_timeout` and `lock_timeout`, and the card path caps rows with
`LIMIT n+1`. What is NOT done and cannot be done in code: a read-only database role. The report connection string
defaults to the application's own role (`ReportConnectionString ?? ConnectionString`), which holds write grants; the
read-only transaction and the AST gate are the enforced controls. Recommendation for the owner: point
`ReportConnectionString` at a SELECT-only role in production (documented in `docs/Concepts/EnvironmentVariables`). New
DynamicEnvironment settings (defaults in `DynamicEnvironment.cs`, rows in `docs/Concepts/EnvironmentVariables/index.md`,
parity test `EnvironmentVariablesDocumentationTests`): `ReportStatementTimeoutSeconds` (30), `ReportMaximumCellBytes`
(1048576), `ReportMaximumResultBytes` (33554432). They reach `Postgres` through the new `ReportLimits` record
(`Jube.Data/Reporting/ReportLimits.cs`) and an optional constructor argument; the datasource card, preview and
introspect paths pass them.

### 12.3 Byte caps

`Postgres.ExecuteLimitedByNamedParametersAsync` sums an approximate size per cell (UTF-8 bytes for text, length for byte
arrays, recursion for arrays, 16 for scalars) and refuses the card when a cell exceeds `ReportMaximumCellBytes` or the
running total exceeds `ReportMaximumResultBytes`. The refusal is a coded `SqlValidationFailedException`
(`DatasourceMessages.CellTooLarge`, `ResultTooLarge`), localised in all eight locales (resx keys `Message.CellTooLarge`
and `Message.ResultTooLarge` added to the seven DatasourceChartResources files with SourceHash comments);
`PostgresErrorDescriber` now passes a coded exception through instead of rewrapping it. Tests: `ReportLimitsTests` (4),
`VisualisationRegistryDatasourceCardServiceTests.AValueOverTheCellByteLimitIsAnErrorCardThatNamesTheColumnAsync` and
`AResultOverTheTotalByteLimitIsAnErrorCardAsync`. The other `Postgres` execute methods (Ask Jooby, case SQL) keep only
their row caps; they are not visualisation-owned.

### 12.4 Gaps closed

Read this pass: the three role repositories and `VisualisationRegistryDatasourceRoleDtoValidator` (both ends of a grant
resolved in the caller's tenant, delete tenant-filtered; sound), series repository,
`VisualisationRegistryParameterRepository` (fixed earlier), the datasource `PenTest` list (create-only mass assignment;
update-move is covered by the new service tests), dashboard `VisualisationDashboard.vue` script (no defect: `contextId`
is validated server-side against the tenant). Not fixed because not defective: nothing further found. Still only
skimmed: `DatasourceBuilder.vue`, `DatasourceDisplayPicker.vue`, `DashboardTile.vue`, directory and story components,
registry and parameter service CRUD bodies beyond what their service tests pin.

### 12.5 Directory and summary: design decision (exact exposure)

`GetByShowInDirectoryAsync` and `GetDirectorySummaryAsync` check no permission, only the role grant (pinned by
`ListByShowInDirectoryNeedsNoPermissionOnlyTheRoleGrantAsync`). Role grants can only be created through
`VisualisationRegistryRoleService.Insert`, which requires permission 31, resolves the registry and the role in the
caller's tenant, and is audited, so the grant cannot be obtained without a permission check: this is a design question,
not a bug. Exposure for a user whose role is granted a registry: its name, column count, column width, row height and
context kind; per registry the count of active tiles and parameters, the display kinds drawn, the number of executions
and failures and the average response time over 30 days (aggregated over ALL users of the tenant, not just that viewer),
the date of the last execution and the user name of whoever ran it last. No SQL, no row data. Owner to decide whether
the last-run user name and the tenant-wide counts should require 28 or 1.

### 12.6 Every test class that exercises the shared validator (run all in the serial pass)

Own: `AskJooby/PostgresSqlValidatorCorpusTests`, `AskJooby/DynamicSqlCommonTableExpressionTests`,
`AskJooby/DynamicSqlNamedParameterRewriteTests`, `Reporting/ReportLimitsTests`,
`Charting/Datasource/DatasourceChartLocalisationTests`,
`Service/Repository/VisualisationRegistry{,Datasource,Parameter}ServiceTests`,
`Service/Query/VisualisationRegistryDatasourceCommandExecution/*`,
`Security/DynamicSql/PenTestVisualisationRegistryDatasource*`.
Other slices: `AskJooby/AskJoobySqlQueryClientTests`, `DynamicSqlRoleFilteringTests`, `DynamicSqlTableAccessTests`,
`DynamicSqlTablePermissionParityTests`, `DynamicSqlTemplateSchemaSweepTests` (checks every template EXPLAINs after the
new public qualification); `Reporting/PostgresSqlValidatorTests`; `Security/DynamicSql/PenTestPostgresGuard`,
`PenTestSqlGate` (+ `SqlAttackCatalogue`); `Service/ExhaustiveInstanceDataset/ExhaustiveInstanceJsonServiceTests`;
`Validations/FilterSqlTextTests`; case-workflow-config: `Service/Query/CaseChecklist/*` (EnableFlag, Reconciler,
ReconcileTrigger), `Service/Repository/CaseWorkflowChecklistItem/SeededCaseChecklistTests`,
`CaseWorkflowFormValidator/FormValidatorEngineTests`, `CaseWorkflowDisplayExecutionServiceTests`,
`CaseWorkflowMacroExecutionServiceTests`, `CaseWorkflowSqlAuthoringTests`; completions:
`Service/Query/Completions/CompletionSqlPathEscapingTests`; model samples: `EntityAnalysisModelSampleServiceTests`;
`DynamicEnvironment/EnvironmentVariablesDocumentationTests`. Watch for tests that assert exact rewritten template text
(now `public."X"`), tests whose SQL uses `format(` (now refused), and any statement with more than 4000 operator
characters or 200 UNIONs after template expansion.

### 12.7 Files changed in the second pass

`Jube.Data/Reporting/PostgresSqlValidator.cs`, `Postgres.cs`, `PostgresErrorDescriber.cs`, `DatasourceMessages.cs`, new
`ReportLimits.cs`; `ExecuteVisualisationRegistryDatasourceCardQuery.cs`,
`IntrospectVisualisationRegistryDatasourceQuery.cs`; the two datasource services;
`Jube.DynamicEnvironment/DynamicEnvironment.cs`; `docs/Concepts/EnvironmentVariables/index.md`; seven
`DatasourceChartResources.*.resx`; new tests `PostgresSqlValidatorCorpusTests`, `ReportLimitsTests`, two card service
tests; test catalogue pages. Rider lint still not run (timed out); unlinted: all of these.

## 13. Third pass (reporting role, front end read, denied functions in shipped SQL)

### 13.1 Report connection warning and the SELECT-only role

`ReportConnectionString` defaults to null and every consumer falls back to the application login, so user SQL ran as the
application role with only the read-only transaction and the validator. New
`Jube.Data/Reporting/ReportConnectionAdvice.cs` (pure: `Assess` returns Unset, SameLogin or Separate by comparing the
Postgres user names with `NpgsqlConnectionStringBuilder`; `Warning` returns text with no host, user or password) and
`ReportConnectionAssessment.cs`. `Startup.ConfigureServices` calls `WarnOnReportConnection` once at start, at Warning
level. Note the quickstart `docker-compose.yml` sets the report string to the same superuser, so it will warn; the
cluster composes use `jube_reporting` and will not. Docs: new section "The reporting database role" in
`docs/Configuration/Visualisations/SecurityPolicy/index.md` (house convention `jube_reporting` from
`Jube.Cluster/database.sh` and `docs/GettingStarted/CreateUsers.sql`, plus an exact narrower `CREATE ROLE` and
`GRANT SELECT` list of the 190 tables the allow-list can reach, column-level `SELECT ("Name","RoleRegistryGuid")` on
`UserRegistry` so password hashes stay unreadable, no password and no migration), a pointer in the
`ReportConnectionString` row of `docs/Concepts/EnvironmentVariables/index.md`, and a paragraph in
`docs/GettingStarted/DeployingWithDocker/index.md`. The narrow role is a recommendation to verify: the same connection
string also serves backtest, reprocessing, sample and case-search reads (TODO (docs) marker left). Tests:
`ReportConnectionAdviceTests` (assessment theory, warning has no detail, and a test that parses the GRANT list in the
docs and compares it with `BuildVisualisationAllowedTables` plus `CollectTableReferences` of every template, so the list
cannot drift). During this pass a shell heredoc with unescaped backticks executed `Jube.Cluster/database.sh` once; it
aborted at "Could not determine Patroni leader" before any change (no cluster was running), and nothing was modified.

### 13.2 Front end, read in full

Read: `DatasourceBuilder.vue`, `DatasourceDisplayPicker.vue` (script and key template parts), `DatasourceSqlEditor.vue`,
`DatasourceTemplateEditor.vue`, `DatasourceSqlStatus.vue`, `DatasourceColumnStrip.vue`, `DatasourceCardView.vue`,
`VisualisationDirectory.vue`, `VisualisationDirectoryDetail.vue`, `VisualisationStory.vue` (script),
`DashboardTile.vue`, `VisualisationDashboard.vue`, `VisualisationParameters.vue`. No `v-html`, no stored XSS path
(markup goes through `JubeMarkup`'s allow-list, SVG through `JubeChart`), no hard-coded English. Fixed:

- The SQL and template editors inherited the page direction, so SQL rendered right to left in Arabic and Urdu; both
  hosts are now `dir="ltr"`.
- Editors accepted unbounded paste; both now refuse a document over 65536 characters (server limit) through
  `datasource/editorLimits.ts` with `editorLimits.spec.ts` (4 tests).
- `DatasourceCardView` hard-coded `ar` as the only right-to-left grid direction (Urdu was left-to-right); it now uses
  `directionFor`.
- Bidi isolation (`dir="auto"` or `dir="ltr"`): server error messages and the SQL status, card message and tile title,
  tile and parameter names in the directory detail, the tile failure heading and the parameter labels; column names and
  types in the column strip are `dir="ltr"`.
  Not changed: `By position: {columns}` interpolates identifiers into a translated sentence (cannot isolate inside the
  string without markup), the tiles of a dashboard all request their card at once (count is bounded by what an author
  configured), and `runBy` user names in the directory grid are not isolated (the grid component belongs to data-grid).
  Prettier, eslint and `vitest run src/datasource src/dashboard` (7 files, 54 tests) pass;
  `vue-tsc -p tsconfig.app.json` passes.

### 13.3 Denied functions in shipped SQL

Searched the whole repository (code, markdown, JSON, SQL, resx; excluding bin, obj, node_modules and the tests that
attack the validator) for `format(`, `crypt(`, `gen_salt(` and `current_query(`. Hits are only: DDL inside migrations
(`EXECUTE format(...)` in DO blocks, not validator-gated),
`Jube.Migrations/Scripts/MigrateCaseKeyJournalIndexesToArchiveKey.sql` (a manual DBA script), and C# methods named
`Format`. The Ask Jooby prompts, tool descriptions, RAG column and table comments, docs and seeded datasources contain
no SQL using them, so nothing shipped fails. The Ask Jooby `RunSqlQuery` tool description was left unchanged (a model
that tries `format()` gets the generic refusal).

### 13.4 Tests to run for this pass

`ReportConnectionAdviceTests`, `ReportLimitsTests`, the section 12.6 list, `EnvironmentVariablesDocumentationTests`;
front end `npx vitest run src/datasource src/dashboard`. Unlinted C# added: `ReportConnectionAdvice.cs`,
`ReportConnectionAssessment.cs`, `Startup.cs` hunk, `ReportConnectionAdviceTests.cs`.
