# Slice review: case-workflow-config

Agent-mode review, 2026-11-08. Steps 0, 2, 3, 4 and the agent-side part of 5. Nothing was built, no .NET or DB-backed
test was run, Playwright not run. Scratch files lived in `scratchpad/case-workflow-config/` (`gen.py`, `unique.json`,
seven `.txt` translation files); `gen.py` printed the 168 files it wrote and all are
`CaseWorkflow*Resources.{pl,ar,ka,id,sv,ur,hi}.resx` of this slice.

## 1. Summary and verdict

Verdict: READY AFTER ONE SERIAL TEST PASS (list in section 5). The configuration surfaces are in good shape: the case
SQL, the template dialect, the hooks and the role-assignment children were written defensively (allow-listed tags and
attributes, HTML-encoded output, hard limits, read-only guarded SQL with a table allow-list and timeout, tenant reached
through the parent model on every repository, guid references validated against the caller's tenant). Fixes made: eight
real defects (children could be moved to another parent by an update; a display and a macro could be run against a case
of a different workflow; a null-tenant escape in one query; a partial class with unbounded regexes; unvalidated regular
expression in conditional formatting). One genuine SSRF was found in the dispatch path (payload mode HTTP endpoints
bypass the outbound host policy) in files claimed by cases-workspace, so it is a BUG line, not fixed here. No permissive
path was reintroduced, no grant or role code was touched (the privilege-escalation rule does not apply: the workflow
Role assignment services only attach Roles of the caller's tenant to workflow children and do not grant permissions).

## 2. Files reviewed

First-touch back-end (slices.json claims only endpoints, one Vue file, docs and the e2e key, so the services,
repositories, validators and resources are first-touched here by the by-hand rule):
`Jube.Data/Repository/CaseWorkflow*Repository.cs` (read: CaseWorkflow, Status, Form, Action, Display, Macro, Filter,
XPath, FormValidator, ReleaseHook, Checklist and ChecklistItem exists/update/role parts, StatusRole whole; the other
seven Role repositories by pattern and the shared Role test base), `Jube.Validations/Repository/CaseWorkflow*/` (read:
CaseWorkflow, Status, Filter, XPath, ReleaseHook, FormValidator SQL and Compiles parts, StatusRole, grep of the
reference rules in all 21), `Jube.Service/Repository/CaseWorkflow*` (read: Mapper for CaseWorkflow, Status, XPath,
ChecklistItem SQL authoring, FormValidatorAuthoring, FormValidatorEngine, grep of the SQL guard use in Status, Form,
Display, Macro, Action, ChecklistItem, FormValidator services), `Jube.Service/Query/CaseWorkflowDisplayExecution` (whole
service and `CaseWorkflowTemplateRoots`), `CaseWorkflowMacroExecutionService` (Execute and render parts), endpoints
`CaseWorkflowDisplayExecutionEndpoints` (Execute, others same shape) and the CaseWorkflow endpoint files by the sampled
pattern (about 8k lines, not read line by line), `Jube.App/Vue/src/components/CaseSqlEditor.vue` and `caseSqlApi.ts`.
Read as dependencies (owned elsewhere): `Jube.Service/CaseDispatch/CaseSqlRunner.cs`, `CaseSqlGuard.cs`,
`HttpTarget.cs`, `CaseDispatcher.cs` (lines 160-260), `Jube.Case/WebhookTarget.cs`, `SendHttpEndpoint.cs`,
`Jube.Case/Outbound/OutboundHostPolicy.cs`, `Jube.Data/Reporting/Postgres.ExecuteLimitedByNamedParametersAsync`,
`Jube.Service/Charts/Datasource/Template/TemplateVocabulary.cs`, `TemplateLimits.cs`, `TemplateParser.cs` attribute
section, `TemplateRenderer.cs` attribute and text encoding, `Jube.Engine/CaseRisk/CaseValueReader.cs`,
`BoundedJsonPath.cs` and its tests (`BoundedJsonPathTests`), `CaseNoteService` action/workflow check.
Pull-ins from `--closure`: `DataGrid.vue` only (claimed by data-grid, inherited, not re-reviewed).

## 3. Findings

| #  | Sev    | Where                                                                               | What                                                                                                                                                                                                                                                                                                                                         | Status                                                                                                                                                               |
|----|--------|-------------------------------------------------------------------------------------|----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|----------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| 1  | Medium | `CaseWorkflow{Status,Form,Action,Display,Macro,Filter,XPath}Repository.UpdateAsync` | `dbContext.UpdateAsync(model)` wrote the DTO's `CaseWorkflowId`: an update could move a status, form, action, display, macro, filter or XPath under another workflow of the same tenant (validator only checks tenant visibility), orphaning cases and role grants that reference the child's guid. Same class as model-activation finding 3 | Fixed: pinned to the existing row; `InheritedId` pinned too (and `ImportId` for Action, which was nulled on every update)                                            |
| 2  | Medium | `CaseWorkflowRepository.UpdateAsync`                                                | A workflow's `EntityAnalysisModelId` could be changed by an update (and `ImportId`, `TenantRegistryId` were nulled)                                                                                                                                                                                                                          | Fixed: all three pinned                                                                                                                                              |
| 3  | Medium | `CaseWorkflowDisplayExecutionService.ExecuteAsync`                                  | Display found by id only, not tied to the case's workflow (the form path uses `GetByIdActiveForCaseWorkflowGuidAsync`): a display of workflow B, whose SQL and template belong to another workflow, rendered against a case of workflow A when the caller held both                                                                          | Fixed: new `CaseWorkflowDisplayRepository.GetByIdActiveForCaseWorkflowGuidAsync`                                                                                     |
| 4  | Medium | `CaseWorkflowMacroExecutionService.ExecuteAsync`                                    | Same for macros, and a macro fires outbound email and HTTP                                                                                                                                                                                                                                                                                   | Fixed: new `CaseWorkflowMacroRepository.GetByIdActiveForCaseWorkflowGuidAsync`                                                                                       |
| 5  | Medium | `CaseWorkflowXPathDtoValidator`                                                     | `RegularExpression` for conditional formatting was only length checked, but it is evaluated on the server against case values (ProcessCaseQuery 1 s timeout, JournalLensReader, GetCaseJournalQuery none) and in the browser                                                                                                                 | Fixed: must compile for the NonBacktracking engine (back references and look-around refused), new key `RegularExpressionInvalid`; `ColorPattern` `$` changed to `\z` |
| 6  | Low    | `CaseWorkflowReleaseHookDtoValidator`                                               | `sealed partial` only for two `[GeneratedRegex]`, house rule forbids partial; the token regex had no timeout; `SourceFieldPattern` `$` accepted a trailing newline                                                                                                                                                                           | Fixed: plain class, static `Regex` fields with 1 s timeout, `\z`, camelCase field names (Rider)                                                                      |
| 7  | Medium | `GetEntityAnalysisModelRequestXpathByCaseWorkflowQuery`                             | Null-tenant escape `                                                                                                                                                                                                                                                                                                                         |                                                                                                                                                                      | !tenantRegistryId.HasValue` on an `int?` ctor parameter | Fixed: tenant is `int`, escape removed, unused `using LinqToDB` removed (Rider); single caller passes the resolved tenant |
| 8  | High   | `CaseDispatcher.LegacyCallAsync`, `CaseStatusNotifier` (Engine), `SendHttpEndpoint` | SSRF: Status, Action, Macro `HttpEndpoint` with no SQL is posted through a bare `HttpClient`, bypassing `GuardedHttpEndpoint` and `OutboundHostPolicy`; the five authoring validators accept any non-empty string. The macro tests rely on a loopback listener in this mode, confirming it is unguarded                                      | Not fixed: files claimed by cases-workspace, a fix changes behaviour the existing tests depend on. BUG line with the fix on SHARED-TODO                              |
| 9  | Low    | `CaseWorkflowFormEntryRepository.cs:56,64` (case-record)                            | `                                                                                                                                                                                                                                                                                                                                            |                                                                                                                                                                      | !tenantRegistryId.HasValue` pattern via an unused tenantless ctor; `GetAsync` has no deleted or role filter | BUG line on SHARED-TODO |
| 10 | Low    | `GetCaseJournalQuery.cs:177` (case-journal)                                         | `new Regex(xPath.RegularExpression)` with no timeout                                                                                                                                                                                                                                                                                         | BUG line                                                                                                                                                             |
| 11 | Low    | `CaseSqlRunner.cs`, `CaseDispatcher.cs`                                             | Hard-coded English messages stored in the dispatch audit                                                                                                                                                                                                                                                                                     | BUG line                                                                                                                                                             |
| 12 | Low    | 12 `Jube.Dto/Repository/CaseWorkflow*` DTOs                                         | Hard-coded English `ListColumn` titles                                                                                                                                                                                                                                                                                                       | BUG line (form-model-engine owns the keys)                                                                                                                           |
| 13 | Design | `CaseSqlRunner`, `FormSqlRunner`                                                    | SQL is vetted against the author's table permissions (permission 67) but runs for every viewer with `BuildVisualisationAllowedTables(userName, _ => true)`: tenant scope, not the viewer's table permissions. This is documented in `DatasourceTemplateDialect.md` as intended, so it is a design question, not a defect                     | Left, on SHARED-TODO as `design`                                                                                                                                     |
| 14 | Info   | `CaseWorkflowDto.ValueField`                                                        | No maximum length (a payload field name read by `CaseValueReader`)                                                                                                                                                                                                                                                                           | Left, no key added; harmless (dictionary lookup, not a path)                                                                                                         |
| 15 | Info   | Locked                                                                              | One-way Locked in every child repository, already the model-core BUG line                                                                                                                                                                                                                                                                    | Not repeated                                                                                                                                                         |

Checked and found sound: SQL authoring (permission 67 `MayAuthor` gate on insert and update of Status, Form, Display,
Macro, Action, parameters limited to `@Id`/`@CaseKeyValue`, 8000 characters, `PostgresSqlValidator` rewrite to the table
allow-list, `AssertSelectOnly`, read-only guard transaction, 200 row and 8 s limits); form validator SQL (parameters
limited to field names plus `value`, `CaseId`, `CaseKeyValue`, 3 s, 2 rows, Pattern kind NonBacktracking with 250 ms);
template dialect (tag and attribute allow-list excludes `script`, `style`, `a`, `img`, `href`, `style`, event handlers;
`form` has no `action`; all text and attribute values `WebUtility.HtmlEncode`; limits 65,536 / depth 24 / 100,000
steps / 1,000,000 output); `HttpTarget` and `WebhookTarget` (literal authority, tokens only in path and query,
URL-encoded, host/port/scheme drift refused, userinfo refused); release hook (URL checked by
`OutboundHostPolicy.CheckTemplate` at save, call via `GuardedHttpEndpoint`, credential variable restricted to
`JUBE_HOOK_`); guid cross-references (Status escalation target same workflow and stronger priority; XPath `SourceGuid`
resolved from the workflow's model sources; Checklist status guid and ChecklistItem checklist guid via tenant-scoped
`Visible*()`; FormValidator form guid likewise; workflow Visualisation and Graph guids tenant-checked; every Role child
checks both parent guid and Role guid against the caller's tenant); `SelectToken`: this slice owns none (XPath rows hold
no user JSONPath, `XPath` is derived from the source), the case-code sites remain on the model-references BUG line for
their owners; maker-checker: not applicable (these children are versioned by `Version` tables with
`OptimisticConcurrency`, no approval kind covers them).

## 4. Fixes made

Files: `Jube.Data/Repository/CaseWorkflow{,Status,Form,Action,Display,Macro,Filter,XPath}Repository.cs` (pinning, plus
two new `...ForCaseWorkflowGuidAsync` methods in Display and Macro);
`Jube.Service/Query/CaseWorkflowDisplayExecution/CaseWorkflowDisplayExecutionService.cs` and
`.../CaseWorkflowMacroExecution/CaseWorkflowMacroExecutionService.cs` (one call each);
`Jube.Validations/Repository/CaseWorkflowXPath/CaseWorkflowXPathDtoValidator.cs`;
`Jube.Validations/Repository/CaseWorkflowReleaseHook/CaseWorkflowReleaseHookDtoValidator.cs`;
`Jube.Data/Query/GetEntityAnalysisModelRequestXpathByCaseWorkflowQuery/...Query.cs`;
`Jube.Resources/CaseWorkflowXPathResources.cs` and `.resx` (one key). No code comments added, one class per file kept,
no partial classes remain in the slice (grep of all slice directories).

## 5. Test and pen-test matrix

Read: `CaseWorkflowServiceTests` (58 -> 59), Status, Form, Action, Display, Macro, Filter, XPath service tests (headers,
helpers and the Update tests), `CaseWorkflowDisplayExecutionServiceTests` (scenario seeding and 40 tests -> 41),
`CaseWorkflowMacroExecutionServiceTests` (seeding and Execute tests, 23 -> 24), `PenTestCaseWorkflowCrudBase` and
`CaseWorkflowChildTestBase` test lists, `PenTestCaseWorkflowRoleBase` list, `PenTestCaseWorkflowReleaseHook` (credential
variable and release URL tests), `BoundedJsonPathTests` (not touched).
Existing coverage cited, so nothing duplicated: permission refusal per verb, cross-tenant read, update and delete,
referential check on foreign parents (`UpdateWithCaseWorkflowIdFromAnotherTenantThrowsValidationFailedAsync` in Form,
Macro, Filter, Display), `UpdatePreservesImportIdAsync`, validators, audit and change events, concurrency; pen: matrix
per principal, IDOR on every route, mass assignment of server-owned fields, hostile and oversized text, type confusion,
concurrency, replay of deletes; Role children via `PenTestCaseWorkflowRoleBase`; display and macro execution pen tests
(`PenTestCaseWorkflowDisplayExecution`, `PenTestCaseWorkflowMacroExecution`).
Added (unrun):

- Service, one per class: `UpdateCannotMoveAWorkflowToAnotherModelOfTheSameTenantAsync` (CaseWorkflow),
  `UpdateCannotMoveAStatusToAnotherWorkflowOfTheSameTenantAsync`, `...AFormTo...`, `...AnActionTo...`,
  `...ADisplayTo...`, `...AMacroTo...`, `...AFilterTo...` (each asserts the returned DTO and the stored row keep the
  original parent). No XPath move test: the validator also resolves `SourceGuid` against the target workflow, so a move
  to a workflow of another model is refused by validation already; the pin is exercised by the shared code path.
- Service: `DisplayOfAnotherWorkflowIsNotFoundForTheCaseEvenWhenTheCallerHoldsEveryRoleAsync` (display execution),
  `ExecuteMacroOfAnotherWorkflowThrowsNotFoundAndCallsNothingAsync` (macro execution).
- Service theory: `ConditionalRegularExpressionThatBacktracksOrDoesNotCompileIsRefusedAsync` (XPath; `(a)\1`, `(?=a)a`,
  `[`).
- Vitest: `Jube.App/Vue/src/components/__tests__/caseSqlApi.spec.ts` (2 tests; prettier and eslint clean, vitest not run).
  Gaps: no PenTest class for FormValidator, Checklist, ChecklistItem (the audit matrix shows `---`): they have service
  tests with tenant isolation, validators, permission (`CaseWorkflowFormValidatorServiceTests` 15,
  `CaseWorkflowChecklistServiceTests` 13, `CaseWorkflowChecklistItemServiceTests` 27, `CaseWorkflowSqlAuthoringTests`,
  `FormValidatorEngineTests` 18) but no HTTP-level IDOR or antiforgery test. Writing them needs new
  `PenTestCaseWorkflowWorld` and seeder members (about 300 lines) that cannot be checked without running; left as a gap,
  not a design question. No Playwright journey beyond `caseSuppression`; the SQL editor is covered by `crud.spec.ts`
  only through the generated form.
  Must be run (owner serial pass, own Postgres): `CaseWorkflowServiceTests`, `CaseWorkflowStatusServiceTests`,
  `CaseWorkflowFormServiceTests`, `CaseWorkflowActionServiceTests`, `CaseWorkflowDisplayServiceTests`,
  `CaseWorkflowMacroServiceTests`, `CaseWorkflowFilterServiceTests`, `CaseWorkflowXPathServiceTests`,
  `CaseWorkflowDisplayExecutionServiceTests`, `CaseWorkflowMacroExecutionServiceTests`,
  `EntityAnalysisModelRequestXPathServiceTests`, then `Category=PenTest` filtered to `PenTestCaseWorkflow*` and
  `PenTestCaseWorkflowDisplayExecution`, `PenTestCaseWorkflowMacroExecution`, plus a build (the new tests rely on
  `CreateWorkflowAsync` and `ValidDto` helpers present in each class) and vitest `src/components`.

## 6. Translations

Created 168 resx (24 classes x pl, ar, ka, id, sv, ur, hi): every `CaseWorkflow*Resources` except `FormEntry`,
`FormEntryValue`, `FormEntryByCaseKeyValue` (case-record owns them; `FormEntryByCaseKeyValue` already had all seven).
Key parity, `{0}`/`{1}` placeholder parity and non-empty values verified by script (`gen.py`, 0 problems); machine
translation, POC quality, 150 distinct strings translated once and applied to all classes. ar and ur wrap Latin tokens,
`@Id`-style parameters and placeholders in U+2068/U+2069 isolates; Western digits kept; hi, ka, sv, id plain. `.fr.resx`
untouched. Server culture registration for ka/id/sv/ur/hi is shared (SHARED-TODO). SPA: `caseSql.*` (9 keys) is complete
in en, pl, ar (audit 0 missing); proposals for ka, id, sv, ur, hi in `review/case-workflow-config.locale-proposals.json`
(placeholder parity checked, vue-i18n `{'@'}` escapes preserved). Grid titles are hard-coded English (finding 12). Not
audited visually: font clipping and RTL layout of the SQL editor (no browser run); the editor uses no physical
left/right classes and no hard-coded strings.

## 7. Docs and TODO markers

Nine pages under `docs/Configuration/CaseManagement/CreatingCaseWorkflow*/index.md` plus
`docs/Frontend/CaseChecklists.md` and `docs/Frontend/DatasourceTemplateDialect.md`: 117 `TODO(screenshot)` markers
inserted above the existing images (Workflows 8, Status 8, Form 16, Action 12, Display 8, Macro 9, Filter 7, XPath 49,
Release Hook 0: owner to supply, marker listed under TODO (docs)) and 41 `TODO(docs)` markers appended per page for
behaviour to verify or new behaviour (parent pinning, display and macro scoped to the case workflow, XPath regular
expression rule, SQL permission 67, outbound policy gap, run-time table scope, permission numbers). Grep `TODO(` in
those files for lines. `docs/Testing/ServiceRepositories.md` and `ServiceQueries.md` updated with the 10 new tests and
counts. Screenshot file names: `case-workflow-config-<page>-NN.png`, route `/models`.

## 8. Shared-code TODOs raised

Seven lines at the end of `review/SHARED-TODO.md`: BUG unguarded legacy HTTP dispatch (CaseDispatcher,
CaseStatusNotifier, SendHttpEndpoint); BUG `CaseWorkflowFormEntryRepository` null tenant; BUG `GetCaseJournalQuery`
regex timeout; BUG hard-coded English in CaseSqlRunner and CaseDispatcher; BUG grid titles of 12 DTOs; FIXED null-tenant
query; design: run-time table permission scope.

## 9. Manual test sheet

Models page, Case Workflows (permission to list, then author per child): create, edit, lock, delete a workflow; each
child (status, form, action, display, macro, filter, XPath, release hook, checklist and items, form validators) as a hub
child; Role assignment on each child (a Role of another tenant refused). Edit a child and confirm its parent workflow
cannot change by API (PUT with another `caseWorkflowId` keeps the original). Status: escalation target in another
workflow refused, weaker priority refused, colours must be `#RRGGBB`. SQL fields: without permission 67 the editor is
read only and a save that changes the SQL gives 403; with it, `DROP TABLE`, `;` batches, unknown `@param`, a 9000
character text and a table outside the allow-list are refused; Run SQL on a case id of another tenant answers not found.
Form: template with `<script>`, `<a href>`, `onclick`, `style`, `id` refused with line and column; `v-if` on `Form.x`;
submit a key that is not a field. Display: open a case; a display id of another workflow answers not found; template
output of `<b>` in payload values is encoded. Macro: run against a case of another workflow answers not found; email
destination from rows must be an address. XPath: conditional regular expression `(a)\1` and `(?=a)a` refused, `^[0-9]+$`
accepted; colours with a trailing newline refused. Release hook: URL to `localhost`, `169.254.169.254`,
`http://10.0.0.5`, `file://`, token in the host, credential variable `ConnectionString` all refused; refusal messages in
pl, ar. Status HTTP endpoint pointed at `http://127.0.0.1` with no SQL: observe that the call is made (finding 8,
expected to be fixed by cases-workspace). Concurrency: two tabs edit the same status, second gets 409. Deep link and
refresh, narrow width, dark theme, en, pl, ar (RTL), keyboard only, one hostile string per text field (script, SQL, 300
characters, Georgian and Devanagari text).

## 10. Commit pathspecs

`python3 -I .claude/skills/jube-slice-review/scripts/slice_audit.py case-workflow-config --paths` plus:
`Jube.Data/Repository/CaseWorkflow*Repository.cs` (eight changed),
`Jube.Data/Query/GetEntityAnalysisModelRequestXpathByCaseWorkflowQuery/`,
`Jube.Validations/Repository/CaseWorkflow{XPath,ReleaseHook}/`, the two execution services,
`Jube.Resources/CaseWorkflowXPathResources.{cs,resx}` and the 168 new `CaseWorkflow*Resources.*.resx`, the changed tests
(eight service test files, the two execution tests, the XPath tests),
`Jube.App/Vue/src/components/__tests__/caseSqlApi.spec.ts`, the nine docs pages, the two Frontend docs pages,
`docs/Testing/ServiceRepositories.md` and `ServiceQueries.md` (hand-pick hunks: they hold other slices' rows),
`review/case-workflow-config.md`, `review/case-workflow-config.locale-proposals.json`, `review/SHARED-TODO.md`
(hand-pick the last seven lines).

## 11. Not covered

Rider lint: ran on the ReleaseHook validator, XPath validator and the query (allocation hints only, plus the two naming
and unused-using findings, fixed). Not linted: the eight repositories, the two execution services, the new tests and the
docs. Not read line by line: the 23 endpoint files beyond `CaseWorkflowDisplayExecutionEndpoints`, the seven non-Status
Role services and repositories (pattern read in StatusRole, covered by `PenTestCaseWorkflowRoleBase`), the Checklist and
ChecklistItem services beyond the SQL guard calls, the ten `*VersionHistory` query services (same 204-line template,
permission shape not compared), `CaseWorkflowChecklistService`/`ChecklistReconciler` runtime paths (case-record), the
form entry submit path (`FormSubmissionValidator`, case-record), `TemplateEvaluator`/`TemplateExpressionParser`
internals (limits read, not fuzzed), the vite and Playwright specs, visual RTL and font checks, the engine side of case
status dispatch. No PenTest for FormValidator, Checklist, ChecklistItem (see section 5). `ValueField` length not
constrained (finding 14).

## 12. Follow-up pass (owner policy: bugs fixed, gaps closed)

Resumed after a session restart: the edits below were verified present in the tree (git status and file reads) before
this section was written; nothing was redone.

### 12.1 SSRF in payload-mode HTTP endpoints: fixed

Finding 8 is closed. The existing machinery was reused, not duplicated: `GuardedHttpEndpoint` (generic, in
`Jube.Case/Outbound`) already carries the connect-time address check against DNS rebinding (`ConnectCallback`,
`OutboundHostPolicy.CheckResolved`), no redirects, no proxy, and the `JUBE_HOOK_ALLOWED_HOSTS`/`JUBE_HOOK_ALLOWED_CIDRS`
allow-list that the release hooks use. `TenantBaseUrlPolicy`/`AdaptationConnector` from model-http-adaptation are
tenant-base-URL and engine specific (https only, no allow-list), so they were read and not used for case hooks; nothing
engine-specific had to be extracted.

- `Jube.Case/Outbound/PayloadEndpointCall.cs` (new): `SendAsync` resolves the endpoint template with
  `WebhookTarget.Resolve` (absolute http or https, scheme, host and port literal, tokens only after the authority, host
  drift and userinfo refused) then sends through `GuardedHttpEndpoint.For(environment)`; `IsWellFormedTemplate` (1 s
  regex timeout) is the shared authoring check.
- `GuardedHttpEndpoint.SendAsync`: the HTTP call now runs through `Jube.ResilientHttp.ResilientHttpExecutor.Shared`
  keyed by host:port (per-host breaker, 10 s timeout, no retry), 3xx still refused, an open circuit returns a failed
  `DispatchResult` ("temporarily suspended") instead of waiting, so one failing hook host cannot stall case processing.
  `Jube.Case.csproj` gained the project reference (no cycle).
- `CaseDispatcher.LegacyCallAsync` and `CaseStatusNotifier.CallHttpEndpointAsync` (new `dynamicEnvironment` parameter,
  caller `LegacyCaseStatusDispatch` updated) no longer use `SendHttpEndpoint`; a refusal is recorded as `Refused` in the
  dispatch audit (Engine path logs a warning). `SendHttpEndpoint` itself is untouched and now has no non-test callers in
  the case paths.
- Authoring: the Action, Form, Status and Macro validators refuse an enabled `HttpEndpoint` that is not a well-formed
  literal URL (`HttpEndpointInvalid`, new in the four resource classes, neutral resx and all seven locales regenerated
  by script with parity check, 0 problems). Display and Filter carry an inert endpoint field that no dispatcher uses.
  Files touched in cases-workspace's area (also on SHARED-TODO, last line):
  `Jube.Service/CaseDispatch/CaseDispatcher.cs` (one hunk in `LegacyCallAsync`),
  `Jube.Engine/CaseEscalation/CaseStatusNotifier.cs`, `LegacyCaseStatusDispatch.cs`. Shared:
  `Jube.Case/Outbound/GuardedHttpEndpoint.cs`, `Jube.Case/Jube.Case.csproj`.
  Tests added (unrun), next to `GuardedHttpEndpointTests`: `Jube.Tests/Case/PayloadEndpointCallTests.cs` (internal
  literal targets refused, token in host or other scheme refused, token in path rendered and an allow-listed loopback
  host called, repeated 500s open the breaker and stop calls); service theories
  `InsertWithAnHttpEndpointThatIsNotAWellFormedLiteralUrlThrowsValidationFailedAsync` in the Action, Form, Status and
  Macro service tests. The loopback macro tests already set `LoopbackAllowed.Cidrs`, so payload-mode tests still reach
  their listener. Catalogue rows added to `docs/Testing`.
  Residual: the breaker keeps per-process state and shares a key with any other caller of the same host:port; the 10 s
  timeout is a constant in `GuardedHttpEndpoint` (not a DynamicEnvironment key).

### 12.2 Smaller BUG lines

- Hard-coded English in `CaseSqlRunner` and `CaseDispatcher` refusal strings stays a BUG line: those files and
  `CaseWorkflowSqlResources` keys are cases-workspace's audit text, and moving them needs a decision on whether the
  dispatch audit stores a key or text.
- Grid `ListColumn` titles of the 12 DTOs: the mechanism (`FormModelLocaliser`, key `{area}.column.{field}` in
  `FormModelResources`) exists but no area has keys yet and the resx files are form-model-engine's. Done instead:
  `review/case-workflow-config.form-model-proposals.json` lists all 33 exact keys with English and seven translations
  for the integration pass. The BUG line stays open until applied.
- `FormEntryRepository` null tenant and `GetCaseJournalQuery` regex: not slice-owned, still BUG lines. (cases-workspace
  has since fixed the SelectToken regex sites in `ProcessCaseQuery`; `GetCaseJournalQuery.cs:177` re-check owed by
  case-journal.)

### 12.3 PenTest classes for FormValidator, Checklist, ChecklistItem

Search first: `PenTestCaseChecklist` covers the runtime checklist endpoints (case-record), none covers the authoring
CRUD. The existing `PenTestCaseWorkflowCrudBase` and `CaseWorkflowChildTestBase` cannot host these entities (they assume
a Locked column and an integer parent id; these three have neither and are parented by status, checklist and form
guids), so a lean sibling base was written: `Jube.Tests/Security/Case/PenTestCaseWorkflowGuidChildBase.cs` plus
`PenTestCaseWorkflowChecklist.cs`, `PenTestCaseWorkflowChecklistItem.cs`, `PenTestCaseWorkflowFormValidator.cs`. They
reuse `PenTestCaseWorkflowBase` (seeded worlds A and B, `RefusalMatrixAsync`, attack strings), seed their own rows
directly and clean them up in `DisposeAsync` before the base cleanup. 8 tests each: refusal matrix on every verb
(anonymous, garbage bearer, no permission, no tenant), cross-tenant read, update and delete by id in both directions
with existence-equivalence and hostile segments, foreign and invalid parent guids on create, mass assignment on create
and update with stale and forged versions (409), parent pinning on update, soft delete and replay, hostile text, JSON
type confusion, malformed and oversize bodies, and missing antiforgery or origin evidence on every mutation. Catalogue
sections added to `docs/Testing/SecurityAccessIsolation.md`. Rider lint (errors) on the four new files and
`PayloadEndpointCallTests`: clean. Not run.

### 12.4 Files not read before, now read

- 23 endpoint files: each normalised against `CaseWorkflowStatusEndpoints` and diffed (differences are route order and
  names only); every file has `RequireAuthorization` (grep count, none at 0), no `AllowAnonymous` or
  `DisableAntiforgery`, error bodies are validation results, localised not-found or conflict messages (`ex.Message` only
  for the version-conflict body). No defect.
- 7 Role services and 10 VersionHistory services: diffed against the Status Role and Status VersionHistory services
  after name normalisation. Role services differ only in names and permission arrays; version history services differ in
  names and permission arrays (Action 22, Status 19, ...). Observation, not a defect: only
  `CaseWorkflowChecklistItemVersionHistoryService` redacts SQL for callers without permission 67; the other SQL-bearing
  histories (Status, Form, Display, Macro, Action) and the live GET routes return the stored SQL to anyone holding the
  area's read permission, consistent with the editor showing SQL read only without 67. There is no FormValidator version
  history query. Left as is, listed for the owner.

### 12.5 Lint

Rider `lint_files` (min severity error) clean: `PayloadEndpointCall`, all four new PenTest files,
`PayloadEndpointCallTests`, `CaseWorkflowRepository`, `CaseWorkflowStatusRepository`, `CaseWorkflowFormRepository`.
Warning level (allocation hints only, no errors) on `GuardedHttpEndpoint`, `PayloadEndpointCall`, `CaseDispatcher`,
`CaseStatusNotifier`. The Rider MCP server timed out and then dropped its connection on every later call, so these
remain unlinted: `CaseWorkflowActionRepository`, `CaseWorkflowDisplayRepository`, `CaseWorkflowMacroRepository`,
`CaseWorkflowFilterRepository`, `CaseWorkflowXPathRepository`, the two execution services, the Action, Form, Status and
Macro validators (their edit is one rule and a using), `LegacyCaseStatusDispatch`, and the edited service and execution
tests.

### 12.6 What a viewer with only a case-view permission can reach through case SQL (design decision, evidence)

Facts from code. A viewer cannot write or run SQL: running it needs permission 67 (`TestSql`, `CaseSqlGuard`), and
authored SQL is parameterised only by `@Id` and `@CaseKeyValue` of the case being viewed. What the viewer triggers with
permission 1 alone (`CaseWorkflowDisplayExecutionService.ExecuteAsync`, `RenderFormAsync`) is the SQL a Display or Form
author saved, executed by `CaseSqlRunner` with
`DynamicSqlTableAccess.BuildVisualisationAllowedTables(viewer, _ => true)`. The `_ => true` predicate skips the
per-table permission check (`rule.RequiredPermissions`), so every table in the registry is available except the 13
`neverChartable` ones (user, role, tenant registries and their versions, API keys, HTTP adaptation). The row scope of
each table template is still applied with the viewer's own name: tenant membership always, and for `Case` also the
viewer's workflow and status role grants. Save-time vetting (`CaseSqlGuard.Append`) uses the author's permissions and
the author's tables, so the author cannot select a table the author could not read, but the viewer's own lack of
permission is never consulted. The same applies to Macro, Action and Status SQL (email, SMS and HTTP bodies built from
the rows, sent to author-chosen destinations, audited in `CaseWorkflowDispatch`), to form validator SQL and to checklist
show and validate SQL (boolean results only, but they gate what the viewer sees and can tick).
Example: an administrator holding permissions 1, 27 and 67 saves a Display whose SQL is
`SELECT "Message" FROM "CaseCreationWarning" WHERE "CaseKeyValue" = @CaseKeyValue` (table rule requires permission 27).
An analyst holding only permission 1 and the workflow, status and display roles opens a case and sees those warning rows
in the display, although the analyst could not query `CaseCreationWarning` directly, in Ask Jooby or a datasource.
Conversely the analyst can never see another tenant's rows, nor `Case` rows of workflows the analyst's roles do not
hold.
Why it is a design question and not a defect: the data shown is exactly what an authorised author chose to attach to the
display, which is the purpose of the feature, and `DatasourceTemplateDialect.md` documents it as intended. The
alternatives are (a) run with the viewer's permissions (a display would then silently show less to junior roles), (b)
intersect author and viewer permissions, or (c) keep it and add a visible "runs with author access" note on the editor.
Recommendation: keep, and add (c).

