# Migration phases and library map

Blueprint §4 (phases V0–V15 with gates, cut-over rule) and §5 (keep/replace/drop) verbatim, then §9 new endpoints.


Built from the current `Jube.App` layout (Razor Pages under `Pages/`, page scripts under `wwwroot/js/`) as mapped in the golden audit. Under D-V28 the result is one SPA: the "Current files it replaces" column lists what is deleted when the phase lands, not a route that switches over. The order is enforceable: each phase names its gate. It is the golden audit's Appendix B, restated as Vue work packages.

| Phase | Vue work | Current files it replaces | Gate |
|---|---|---|---|
| **V0 · Toolchain** | Vite project `Jube.App/ClientApp/`; CI builds to `wwwroot/app/` (vendored output, D-V1). OpenAPI type generation (D-V2). ESLint rule banning `fetch` outside `useApi`, `v-html` anywhere, and `eval`/`new Function`. CSP header without `unsafe-eval`. | — | none; runs in parallel with branch consolidation (audit Phase 0) |
| **V1 · Shell** | AppShell, TopNav, CommandPalette (empty registry), ToastHost, ConnectionBanner, theme and density, i18n with locale files (D-V29), `useApi`, `useTokenStore` (D-V27), `useAuthSession`, **QueryClient + `useServiceChanges` singleton + `queryKeys.ts` + `invalidationMap.ts`** (§2.4), Vue Router (history mode) and the SPA fallback (`MapFallbackToFile("app/index.html")`, D-V28), proven on the first config grid. | `Pages/Shared/_Layout.cshtml`, `wwwroot/js/site.js` equivalents | V0 |
| **V2 · Login journey** | LoginJourneyPage over the existing Account endpoints; TOTP provider (`TotpMfaProvider : IMfaProvider`) + enrolment + recovery codes. | `Pages/Account/Login.cshtml(.cs)`, `ChangePassword.cshtml(.cs)`, RSA challenge flow | V1 |
| **V3 · Generic renderer** | DtoForm, FormFieldRenderer + all widgets, DtoGrid, HubCard, Breadcrumbs, RoleAllocationField, LookupField, JubeCoder, RuleBuilderIsland; Schema endpoint (CG-14). Proven on the four reference DTOs (`EntityAnalysisModelDto`, `…RequestXPathDto`, `…InlineScriptDto`, `…GatewayRuleDto`). | `wwwroot/js/Tree.js`, `RoleAllocation/RoleAllocation.js`, `BuilderCoder.js` (coder half) | V1; no dependency on the Case page |
| **V4 · Configuration rollout** | Every configuration DTO migrated to `[FormField]` and rendered by V3. Retire the per-entity `wwwroot/js/Model/Frame/*.js` and `Administration/Frame/*.js` files as each lands. ACE is removed from `CaseWorkflowMacro.js`, `CaseWorkflowDisplay.js` and `CaseWorkflowForm.js` with their redesigns (macros become operation sequences; displays become components). | `wwwroot/js/Model/Frame/*`, `wwwroot/js/Administration/Frame/*`, `wwwroot/js/ace/` | V3 |
| **V5 · Backend foundations** | No UI. Generic Preview/Execute, CaseVersion, CaseEvent enum + hash chain + CaseViewed, dispatcher + execution log, ArchiveScore, case-risk worker, CaseDiary, sanctions detail persistence. | — | audit Phase 5 |
| **V6 · Cases page** | CasesPage tree; session filter editing through RuleBuilderIsland (keeps today's `POST /api/SessionCaseSearchCompiledSql`); Save filter (CP-19). | `Pages/Case/CaseSearch.cshtml(.cs)`, `wwwroot/js/Case/CaseSearch.js`, `CaseFilterBuilder.js` | V3, V5 |
| **V7 · Case page core** | CasePage, CaseHeader, CaseKeyPanel, Summary, Activations, DecideCockpit, CaseRiskPopover, OperationLightbox shell + CloseForm. | `wwwroot/js/Case/Case.js` (1,848 lines; the eight first-paint calls collapse into CH-00) | V5, Jube.Charting merged |
| **V8 · Journal, drawers, network** | JournalSection (all lenses), TagMenu (`PUT /api/Invoke/Archive/Tag`), TransactionCardDrawer, PayloadDrawer, ModelFactsDrawer, NetworkSection + `useForceLayout`. | Case Key Journal tab in `Case.js`; `wwwroot/js/Model/Integrity.js` pattern (Kendo Diagram) | V7 |
| **V9 · Notes, events** | NotesSection with action results and mentions; CaseJournalSection with the chain badge and risk history. | notes/events tabs in `Case.js` | V7 |
| **V10 · Remaining lightboxes** | FileForm + ApproverView (maker-checker), BreakLockForm, BulkCloseForm, Link and Block. | lock/assign UI in `Case.js` | V7 |
| **V11 · Payments + step-up** | HeldPaymentCard (both directions), PaymentDecisionForm, StepUpPanel, ExecutionResult, RecallForm, RecoveryCard, RecallRequestCard, PayeeCheckChip; passkeys (phase 2). Design doc §13–14. | — | V10 |
| **V11b · Claims and support needs** | ClaimPage (two clocks, payments in scope, assessment, receiving PSPs), StopClockForm, DecideClaimForm, SupportNeedsChip, VulnerabilityForm, outcome tile; CG-44 to CG-48. | — | V11, V13a (Stop the clock uses RFI) |
| **V12 · Team, Inbox, AskJooby, Compose** | TeamViewPage (desk and wall), InboxDrawer, AskJoobyPanel, ComposeMode. | `wwwroot/js/Watcher/Activation.js` (the consumer pattern is kept; UI replaced) | V7; AskJooby personas on the branch |
| **V13a · M2 QA, M3 RFI** | QaSamplingCard, QaReviewPage, QaReviewForm, RfiForm, RfiPortalPage, RfiThread. | — | V10 |
| **V12b · Suppression and lists** | SuppressForm, SuppressionBadge, SuppressionsPage, AdaptationUnavailableLine, ListChangeSetEditor, ProposeListChangeForm; migrate legacy suppressions (reason "Legacy, no reason", expiry = migration + 30 d). | today's Suppression page and the Lists and Dictionary value grids | V3, V7 |
| **V13b · M1, M6, M5, M4** | AgentRunCard (needs the Analyst Agent persona), ShadowTestPanel, AutomationRecipesPage, WebResearchCard, in that order. | — | V12, V13a |
| **V14 · Visualisation, Exhaustive** | Visualisation on `Chart.*()` via JubeChart; Exhaustive relayout. | `wwwroot/js/Administration/Frame/VisualisationRegistryDatasource.js` (ACE SQL editor → JubeCoder), `wwwroot/js/Model/EntityAnalysisModelSample.js`, Exhaustive page scripts | V3 |
| **V15 · Hardening** | Playwright suites per journey (J1–J8); CodeQL workflow; axe-core in CI; Lighthouse budget (first paint < 300 ms API, < 1.5 s LCP on desk). | — | all |

**Cut-over rule (D-V28):** there is no route-by-route coexistence. The SPA is built behind a feature flag on the same origin and replaces the Razor UI in one release once V4 and V6–V10 pass their gates; until then production keeps the current Razor/Kendo app unchanged. From the first SPA release, cookies and antiforgery are gone (D-V27). There is never a Kendo widget inside a Vue page. The one exception is **RuleBuilderIsland**, the only component allowed to load jQuery.

---

## Vendored libraries: keep, replace or drop

The rule: drop any vendored library that has a daisyUI (or native platform) equivalent.

| Library today | Where | Decision | Replacement |
|---|---|---|---|
| Kendo Grid | grids everywhere | **Drop** | Tabulator via `DataGrid` |
| Kendo TreeView | `Tree.js`, CaseSearch filter tree | **Drop** | `HubCard` + `Breadcrumbs` |
| Kendo ListBox (dual list) | `RoleAllocation.js` | **Drop** | `RoleAllocationField` (daisy `list`, checkboxes, filter input) |
| Kendo Window / Dialog | modals | **Drop** | daisy `modal` (`<dialog>`), via OperationLightbox or a plain modal |
| Kendo TabStrip | Exhaustive, model pages | **Drop** | daisy `tabs`, or the flow layout (A.10) |
| Kendo Notification | toasts | **Drop** | daisy `toast` + `alert` via ToastHost |
| Kendo DropDownList / ComboBox | forms | **Drop** | daisy `select`; searchable → daisy `dropdown` + `input` (LookupField) |
| Kendo DatePicker / DateTimePicker | forms | **Drop** | native `<input type="date|datetime-local">` styled with daisy `input` |
| Kendo NumericTextBox, Switch, Slider | forms | **Drop** | daisy `input`, `toggle`, `range` |
| Kendo Diagram | Model Integrity, topology | **Drop** | `Jube.Charting` GraphDiagram (Flow and Hierarchy layouts); `useForceLayout` + SVG (Force layout) |
| Kendo Charts | visualisations | **Drop** | `Jube.Charting` server SVG via `JubeChart` |
| ACE editor | 5 files (A.14) | **Drop** | **JubeCoder** (below) |
| jQuery QueryBuilder | `BuilderCoder.js`, `CaseFilterBuilder.js` | **Keep**, isolated in RuleBuilderIsland | — |
| jQuery | global | **Keep, scoped** (loaded only by RuleBuilderIsland) | — |
| barrating | case rating stars | **Drop** | daisy `rating` |
| jszip + Kendo Excel export + `proxy.jube.io` | `CaseSearch.js`, `Case.js`, `Visualisation.js` (`toolbar: ["excel"]`) | **Drop.** Today's export proxies downloads through the external host `https://proxy.jube.io`, which is a data-egress point. | Tabulator's built-in `.xlsx` and CSV download, backed by vendored **SheetJS**, entirely client-side with no proxy. `connect-src 'self'` (§7) blocks any regression. |
| @microsoft/signalr | WatcherHub | **Keep** | also used for ServiceChangeHub (singleton) and the presence-only CaseHub |
| — (new) | — | **Add** `@tanstack/vue-query`, vendored in the Vite build | server-state cache and invalidation (D-V20) |
| Font / PNG icons | `wwwroot/icons` | **Replace** | Lucide (vendored ESM) |

**JubeCoder** (replaces ACE, D-V10). It is a small Vue component, not a library:
- a `<textarea>` overlaid on a highlighted `<pre>`, kept in scroll sync;
- the tokenizer is a pure function per language (`jube-rule`, `sql`, `json`), unit-tested, with **no `eval` and no `new Function`**;
- line numbers, bracket matching, Tab/Shift+Tab indent, and a completion popup fed by the `completions` prop (the model's field dictionary);
- "Validate" calls `POST /api/Parser` (existing, `ParserService.ParseAsync` → `ParseRuleResultDto`, which is what `BuilderCoder.js` calls today) and shows errors as line markers. Completions come from `GET /api/Completions/ByEntityAnalysisModelIdParseTypeId` (existing);
- performance ceiling: 2,000 lines, which is well above any rule or datasource SQL.

---

## New endpoints introduced by this blueprint (CG-series)

These are in addition to the golden audit's Appendix A.

| Ref | Method and path | Purpose | Status |
|---|---|---|---|
| CG-01/02 | `GET`/`POST /api/Case/{id}/AgentRun` | Latest agent triage run with work log / re-run | New |
| CG-03 | Op: `OverrideAgent` | Record disagreement with the agent, reason required | New |
| CG-04 | `GET`/`PUT /api/QaPlan` | QA sampling plan | New |
| CG-05 | `GET /api/QaQueue` | Sampled cases awaiting review | New |
| CG-06/07 | Op: `RecordQaReview`, Op: `Reopen` | QA outcome; reopen a closed case (maker-checker) | New |
| CG-08/11 | Op: `SendRfi`, Op: `CancelRfi` | RFI lifecycle | New |
| CG-09/10 | `GET /api/Rfi/{token}`, `POST /api/Rfi/{token}/Answer` | Public RFI portal (single-use token, rate-limited) | New |
| CG-12/13 | `POST /api/AskJooby/Research`, Op: `SaveResearch` | Web research with citations; save to case | New |
| CG-14 | `GET /api/{entity}/Schema` | Serialised `[FormEndpoint]`/`[FormGroup]`/`[FormField]`/`[ListColumn]` metadata for the generic renderer | New |
| CG-15 | ~~`GET /api/Resources/{culture}`~~ | Withdrawn by D-V29: locale files ship in the Vue build | Withdrawn |
| CG-16 | ~~`GET /api/Antiforgery`~~ | Withdrawn by D-V27: no cookie auth, so no CSRF surface | Withdrawn |
| CG-17/18/19 | `/api/AutomationRecipe` CRUD, `/DryRun`, `/Runs`; Op: `PublishRecipe` | Automation recipes | New |
| CG-23 | `GET /api/Me` | Current user: id, name, roles, permissions, tenant, culture, timezone, `PaymentReleaseLimit` (new column on the user). Also used as the keep-alive request. | New |
| CG-24 | `/api/PaymentHoldPolicy` (DTO CRUD + `/Schema`) | Hold ceiling, clock, default action, escalation timings and optional payer notice, one row per market | New |
| CG-25 | `GET /api/PaymentHoldPolicy/Resolve?market=&direction=&at=` | The row that applies to a payment at a given time | New |
| CG-26 | `GET /api/Suppression?key=&value=&status=&scope=&caseId=` | Search and list (extends today's Fetch) | Extend |
| CG-27 | Op: `CreateSuppression` (Preview/Execute) | Create a suppression, from a case or on its own; the Preview returns consequences, approval and blocked reasons | New |
| CG-28 | Op: `RevokeSuppression` | Revoke, reason required | New |
| CG-29 | Op: `RenewSuppression` | Renew within policy, new reason | New |
| CG-30 | `GET /api/Suppression/Impact?keyField=&keyValue=&scope=&ruleIds=&days=30` | Matches that would have been suppressed; any later confirmed TP | New |
| CG-31 | `GET /api/Suppression/{id}/History` | Created, approved, renewed and revoked, with hit counts | New |
| CG-32 | `GET /api/Case/{id}/Suppressions` | Active suppressions on the case's keys | New |
| CG-33 | Engine: `SuppressedBy[{id, version}]` on each archived activation | Makes "matched · suppressed" auditable and replayable | New (engine) |
| CG-34 | `/api/EntityAnalysisModelList/{id}/ChangeSet` (the same for Dictionary): draft, diff, CSV, submit, publish | Versioned list changes | New (replaces direct value CRUD) |
| CG-35 | Op: `ProposeListChange` | A change-set line from a case (replaces `BlockIdentifier`) | New |
| CG-36 | Engine: list and dictionary version + hash in the model snapshot | Reliable replay | New (engine) |
| CG-37 | `POST /api/AskJooby/Narrate` | Stateless framed narration (D-V26) | New |
| CG-38 | `GET /api/AskJooby/Frame` | Frame catalogue with versions (admin and tests) | New |
| CG-39 | `GET /api/AskJooby/Run/{id}` | Provenance of one narration: frame, context hash, facts, model | New |
| CG-20/21/22 | `GET /api/ActivationRule/{id}/Performance`, `POST …/Backtest`; Op: `StartShadow`/`PromoteShadow` | Rule tuning and shadow mode | New (backtest reuses `llm-tool-offload`) |