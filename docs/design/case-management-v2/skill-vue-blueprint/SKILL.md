---
name: jube-vue-blueprint
description: Deterministic Vue 3 architecture blueprint for the Jube Case Management v2 rewrite. Use when generating any Vue component, composable, Pinia store or page for Jube. Covers component tree with props and emits, composables and stores, edge cases and accessibility, migration phases from the current Razor/jQuery/Kendo pages, library replacement, MCP setup and the bearer-token security posture (D-V27).
---

# Jube Case Management v2: Vue 3 architecture blueprint

**Status:** final for build handoff, 27 September 2026.
**Inputs:** `jube_case_management_v2_spec_0.11.md`, the design walkthrough and pass-4 screens (A1–F5, M1–M6), `ui-specification-golden-audit.md` (Parts A–C and Appendices A–B, whose endpoint refs are used verbatim here).
**Numbering:** G1–G12 are the regulatory gaps (design doc §17 and the audit); M1–M6 are the market gaps (§8 here). They never share a prefix.
**Rule for the generating agent:** do not improvise. If a component, prop, event, endpoint or state is not listed here, stop and ask; don't invent one. Endpoint refs (CP-, CH-, S1–S6-, TC-, MF-, KP-, DC-, LB-, H-, G-, CG-) point to Appendix A of the golden audit and to section 9 of this file.

---

## 0. Decisions this blueprint assumes

The golden audit left several items as "DECISION NEEDED". To keep the blueprint deterministic, each has a recommended answer here. Change a row and the dependent sections change with it.

| # | Question | Adopted | Consequence |
|---|---|---|---|
| D-V1 | SFC build or in-browser templates (A.1) | **SFCs built in CI with Vite; the built bundle is vendored and served by `Jube.App`.** Developers can run the app without Node. | Vue's runtime-only build does not compile templates in the browser, so CSP needs no `unsafe-eval`. This closes the same CodeQL finding class that ACE causes (A.14). |
| D-V2 | Types | `<script setup lang="ts">`. DTO types are generated in CI from the existing Swagger/OpenAPI document into `src/api/types.gen.ts`. They are never hand-written. | One source of truth: the C# DTOs. |
| D-V3 | Live transport (A.2) | **The existing `ServiceChangeHub` (`/serviceChangeHub`) is the single invalidation channel.** It is backed by `IServiceChangeBus`, so it works on Redis, in-process or single-node deployments. `WatcherHub` stays for the activation stream. A small new `CaseHub` carries **presence only**. No Server-Sent Events, and no bespoke push of entity data. | `@microsoft/signalr` stays vendored; see §2.4 |
| D-V20 | Server state | **TanStack Query for Vue (`@tanstack/vue-query`)** holds all server data, keyed `[Area, entityId, …]`. A `ServiceChange` message marks the matching queries stale, and they refetch in the background. Pinia holds client state only. | §2.4; the loaders in §2.1 become `useQuery` wrappers |
| D-V4 | Widget spelling (A.4) | **`colour`.** Fix the two `color` usages in `EntityAnalysisModelActivationRuleDto`, and make the renderer reject unknown widget names at build time. | `FormFieldRenderer` has one exhaustive switch. |
| D-V5 | Responsive scope (A.6) | Two tiers: **Desk** (≥ 1280 px) is everything. **Read-and-act** (< 1280 px) is Summary, Activations (read-only), the held payment card and its decision lightbox, Inbox and approvals. The rest shows "Open on desktop". | See §3.3. |
| D-V6 | Charts (A.7) | **`Jube.Charting` server SVG, never Radzen.** The Vue side only mounts `<JubeChart>`, which fetches SVG. | No chart JS in the bundle. |
| D-V7 | Graph renderer (Part C) | `GraphDiagram` (Jube.Charting) for Flow and Hierarchy layouts and for snapshots. A dependency-free force layout (`useForceLayout`, ≤ 150 nodes) for the Force layout. **No Kendo Diagram.** | |
| D-V8 | Case-view audit (G12) | Build it as a new `CaseEvent` type (`CaseViewed`), written on Fetch, Skim and Peek. | No new table. |
| D-V9 | Checklist logic (A.15) | `CaseWorkflowChecklistItem.FilterJson`, compiled by `ParserJsonToSqlCase`. | The client never evaluates checklist logic. |
| D-V10 | Rule editing | **jQuery QueryBuilder stays**, isolated in one "island" component. **ACE goes**, replaced by Jube's own lightweight coder (`JubeCoder`). | jQuery is loaded only on pages that mount `RuleBuilderIsland`. |
| D-V11 | i18n (A.5) | ~~vue-i18n fed by `GET /api/Resources/{culture}` from `.resx`.~~ **Superseded by D-V29.** | — |
| D-V12 | Icons | Lucide, vendored ESM, tree-shaken. | |
| D-V13 | AskJooby personas after Researcher | **Analyst Agent** first, then **Rule Writer**, which uses `Jube.Parser`, `POST /api/Parser` and the Model Integrity dependency tools. | M1 depends on Analyst Agent. |
| D-V14 | Agent triage (M1) default | **Off for new tenants; opt-in per filter.** This follows the same pattern as Suspend Bypass. | Filter setting `AgentTriageEnabled` |
| D-V15 | Web research (M4) egress | **Per-tenant opt-in**, shaped like HTTP Adaptation endpoints. Every outbound query is logged as an `AskJoobyRun` tool call, so what left Jube can always be reconstructed. | Tenant setting `WebResearchEnabled`; audit through the existing run log |
| D-V16 | Market-gap order | **M2 (QA) and M3 (RFI) first.** They extend existing notes and notification hooks. Then M1, M6, M5 and M4. | §4 V13 split |
| D-V18 | Hold rules per market | **One `PaymentHoldPolicy` row per market** (and optionally per scheme and direction), versioned by effective date, and edited with the generic DTO form with maker-checker. If no row matches, the hold is **EscalateOnly**. | Hold worker, HeldPaymentCard and PaymentDecisionForm read the row from CG-25 |
| D-V19 | Payer notice | **Optional per market** (`PayerNoticeEnabled`), with a due time, channel and template on the same row | HeldPaymentCard shows "Notice sent hh:mm", "Due by …" or "No notice for this market" |
| D-V21 | Lists and Dictionaries | **Model artefacts.** Every change is a versioned change set (draft, diff, backtest, approval, publish), and the version and hash are recorded in the model snapshot. There are no direct value writes from any UI. Emergency publish needs approval within 24 h. | ListChangeSetEditor; the Lists menu moves to model configuration |
| D-V22 | Override surface | **Suppression is the only override.** Every suppression is time-bound (default 14 d, max 90 d), has a reason, and can link to a case. Each affected match is stamped `SuppressedBy`. It can be created from a case or on its own. | SuppressForm, SuppressionsPage, SuppressionBadge |
| D-V23 | Suppression governance | Maker-checker for model scope or more than 30 d. MLRO approval for keys with a pending SAR or a confirmed TP in the last 12 months. Floor rules can never be suppressed. No step-up. Bulk revoke only. | Preview returns `approval` and `blocked` |
| D-V24 | Writing lists from a case | **Replaced by `ProposeListChange`**, a line in the model's draft change set. BlockIdentifier and "also block beneficiary" are removed. | ProposeListChangeForm replaces BlockIdentifierForm |
| D-V25 | Wording | An adaptation that returned an `Error` (null Value) is shown as **"unavailable"** under model health, never as "suppressed". | AdaptationUnavailableLine |
| D-V26 | Generated narratives | **Every backend-generated case or data narrative is a direct, stateless AskJooby call with a server-built framed context.** It never uses chat history. The client names a registered frame and a subject, never prompt text. | §2.5; `useNarrative`; CG-37 to CG-39 |
| D-V17 | Bearer and cookie split | ~~Keep `HybridAuthorisationHandler` as it is.~~ **Superseded by D-V27.** | §7 |
| D-V28 | Application shape | **One Vue SPA replaces Razor routes.** `Jube.App` serves `wwwroot/app/index.html` as the fallback for every non-API, non-hub path; Vue Router owns all navigation (history mode). No Razor layout hosts Vue, and no route-by-route coexistence exists. Razor Pages remain only for server endpoints with no UI (OIDC callback handshake), which hand off to the SPA. | §4; coexistence rule rewritten |
| D-V29 | Localisation | **vue-i18n with locale files in the Vue project:** `ClientApp/src/locales/{locale}.json` (English `en.json` is the source and the type reference). Bundled with the build and lazy-loaded per locale; no server resource endpoint. `.resx` is retired for UI strings. A CI check fails the build on keys missing from `en.json` or unused keys. | §2 `useI18n`; CG-15 withdrawn |
| D-V27 | Session token | **JWT in a tab-scoped secure store, sent as `Authorization: Bearer`. Cookies are removed from the auth pipeline; antiforgery (CG-16) is withdrawn.** | §2 `useTokenStore`, `useApi`; §7 |

---

## 1. Component hierarchy and prop matrix

### 1.0 Conventions (binding)

- File per component: `src/components/<Area>/<ComponentName>.vue`. Pages: `src/pages/<Name>Page.vue`.
- Props use `defineProps<T>()` with `withDefaults`. Emits use `defineEmits<{...}>()`, and every emit is listed here. Never emit an event that isn't listed.
- **Components never call `fetch`.** All I/O goes through a composable (§2). Components receive data as props, or read a Pinia store with the permission given in §2.3.
- Styling: daisyUI 5 classes and Tailwind utilities only. No component-scoped CSS except `@keyframes`. Theme via `data-theme` on `<html>`; density via `data-density`.
- Tabulator is wrapped once (`DataGrid`), and nothing else touches Tabulator directly.
- Types: `CaseId = number`, `Guid = string`, `IsoDateTime = string`. Money is `{ amount: string; currency: string }`, with a decimal string and never a float.

Type shorthands used in the tables:
`Lane = 'LiveMoney'|'Mandatory'|'DeadlineNear'|'Standard'`,
`LikelihoodBand = 'Unlikely'|'Possible'|'Likely'|'VeryLikely'`,
`ImpactBand = 'Low'|'Moderate'|'High'|'Severe'`,
`RiskChipVm = { lane: Lane; likelihood: number; band: LikelihoodBand; impact: ImpactBand; priority: number; priorityOverride?: { value: number; by: string; reason: string; expires: IsoDateTime }; method: 'ColdStart'|'Calibrated' }`,
`DiaryVm = { id: number; kind: 'User'|'Regulatory'|'PaymentHold'|'Claim'; label: string; dueAt: IsoDateTime; state: 'Normal'|'DueSoon'|'Breached'|'Paused'|'HoldExpiring'; pausedReason?: string; remainingOnResume?: string }`,
`OpName` = any operation in Appendix A6/A7 of the audit (for example `'Close'|'File'|'Approve'|'BreakLock'|'ReleasePayment'|…`).

### 1.1 Tree

```
App
├─ AppShell
│  ├─ TopNav
│  ├─ CommandPalette
│  ├─ InboxDrawer
│  ├─ ToastHost
│  ├─ ConnectionBanner
│  └─ <RouterView>
│     ├─ LoginJourneyPage ── LoginStep · ChangePasswordStep · MfaStep · TotpEnrolStep · RecoveryCodesStep
│     ├─ CasesPage
│     │  ├─ FilterHub ── HubCard*
│     │  ├─ CasesToolbar ── SortSelect · SaveFilterButton · RiskMatrixToggle
│     │  ├─ RiskMatrix
│     │  ├─ DataGrid(cases) ── RiskChip (cell) · LockCell (cell)
│     │  ├─ BulkBar
│     │  └─ PeekDrawer ── HeldPaymentCard · DiaryMeter*
│     ├─ CasePage
│     │  ├─ CaseHeader ── RiskChip · LaneBadge · DiaryMeter · PresenceAvatars · LockBadge
│     │  ├─ LiveChangeBanner
│     │  ├─ CaseKeyPanel ── IdentifierRow*
│     │  ├─ CaseMain ── SectionNav
│     │  │  ├─ SummarySection ── RankedLine · CaseDetailView* · HeldPaymentCard · AgentRunCard (M1)
│     │  │  ├─ ActivationsSection ── ActivationCard* ── ScoreStrip · JubeChart(waterfall) · SensitivityLine · RulePathView · SanctionsMatchCard · ShadowTestPanel (M6, drawer)
│     │  │  ├─ JournalSection ── LensBar · PivotBanner · AggregationStrip · JubeChart(daily) · JubeChart(sparkline) · DataGrid(journal) · ExplodeGrid · TagMenu
│     │  │  ├─ NetworkSection ── GraphCanvas · NodePanel
│     │  │  ├─ NotesSection ── ActionComposer · NoteList · RfiThread (M3)
│     │  │  └─ CaseJournalSection ── PriorCases · DataGrid(events) · HashChainBadge · DataGrid(riskHistory)
│     │  ├─ RightColumn ── DecideCockpit (SuggestedStep · Checklist · DiaryMeter* · OperationButtons) | AskJoobyPanel (ProposalCard* · WebResearchCard (M4))
│     │  ├─ CaseRiskPopover
│     │  ├─ TransactionCardDrawer ── TagMenu
│     │  ├─ PayloadDrawer
│     │  ├─ ModelFactsDrawer
│     │  ├─ ComposeMode ── FactsTray · NarrativeEditor · QualityScorecard · FramingList
│     │  └─ OperationLightbox ── ConsequenceList · <form slot> · StepUpPanel · ExecutionResult
│     │        form slot ∈ CloseForm · FileForm · ApproverView · BreakLockForm · BulkCloseForm · PaymentDecisionForm · LinkKeyForm · BlockIdentifierForm · RfiForm (M3) · QaReviewForm (M2)
│     ├─ TeamViewPage (desk | wall) ── AnalystTable · FilterLoadTable · RiskMatrix · PendingChangesCard · QaSamplingCard (M2)
│     ├─ QaReviewPage (M2)
│     ├─ ReferralPage (public, tenant-branded)
│     ├─ RfiPortalPage (public, tenant-branded, M3)
│     ├─ ReportStatusPage
│     ├─ AutomationRecipesPage (M5) ── RecipeCard* · RecipeBuilder · RecipeRunLog
│     ├─ ConfigHubPage ── Breadcrumbs · HubCard* 
│     ├─ DtoGridPage ── DataGrid
│     ├─ DtoFormPage ── DtoForm ── FormGroup* ── FormFieldRenderer* ── <widget>
│     │     widget ∈ TextField · TextareaField · NumberField · PercentField · SliderField · SelectField · RadioField · SwitchField · ColourField · DateField · GuidField · LookupField · RoleAllocationField · CodeEditorField(JubeCoder) · QueryBuilderField(RuleBuilderIsland)
│     └─ UserSettingsPage ── TotpEnrolStep · RecoveryCodesStep · PasskeyCard (phase 2)
└─ (global) KeyboardScopeProvider
```

### 1.2 Shell

| Component | Lives in | Props (name: type, req, default — purpose) | Emits (event: payload — trigger) |
|---|---|---|---|
| **AppShell** | `App` root | — (reads `useSessionStore`, `useUiStore`) | — |
| **TopNav** | AppShell | `inboxCount: number`, Y, — : unread badge · `userName: string`, Y · `tenantName: string`, Y | `open-palette: void` on click or Ctrl/Cmd+K · `open-inbox: void` on click or `g i` · `toggle-theme: void` on click · `sign-out: void` on menu item |
| **CommandPalette** | AppShell | `open: boolean`, Y · `scope: 'global'\|'cases'\|'case'`, Y · `caseId: CaseId\|null`, N, `null` | `close: void` on Esc or backdrop · `run: { commandId: string; args?: Record<string,unknown> }` on Enter with an enabled item (disabled items show why and never emit) |
| **InboxDrawer** | AppShell | `open: boolean`, Y | `close: void` · `open-item: InboxItemVm` on the row button · `mark-done: { id: number }` on "Done" |
| **ToastHost** | AppShell | — (reads `useUiStore().toasts`) | `undo: { operationId: Guid }` on the Undo button within the undo window |
| **ConnectionBanner** | AppShell | `state: 'online'\|'reconnecting'\|'offline'`, Y · `retryInSeconds: number`, N, `0` | `retry: void` on "Retry now" |
| **KeyboardScopeProvider** | App | `scope: string`, Y | — (provides `useKeyboard` context) |

### 1.3 Login journey (one stepper, one route)

| Component | Lives in | Props | Emits |
|---|---|---|---|
| **LoginJourneyPage** | Router `/login` | `returnUrl: string`, N, `'/'` | — (drives steps via `useLoginJourney`) |
| **LoginStep** | LoginJourneyPage | `busy: boolean`, Y · `error: string\|null`, N, `null` | `submit: { userName: string; password: string }` on form submit when both are non-empty |
| **ChangePasswordStep** | LoginJourneyPage | `busy: boolean`, Y · `policy: PasswordPolicyVm`, Y · `error: string\|null`, N | `submit: { current: string; next: string }` when `next` passes policy and matches the confirmation |
| **MfaStep** | LoginJourneyPage, StepUpPanel | `methods: ('Rsa'\|'Totp'\|'Passkey')[]`, Y · `method: 'Rsa'\|'Totp'\|'Passkey'`, Y · `busy: boolean`, Y · `error: string\|null`, N | `choose-method: 'Rsa'\|'Totp'\|'Passkey'` on tab click · `submit: { code?: string; assertion?: PublicKeyCredentialJSON }` on Verify with a 6-digit code, or on passkey completion |
| **TotpEnrolStep** | LoginJourneyPage, UserSettingsPage | `otpauthUri: string`, Y · `secretDisplay: string`, Y · `busy: boolean`, Y | `confirm: { code: string }` when the code is 6 digits |
| **RecoveryCodesStep** | same | `codes: string[]`, Y | `finish: void` only after the "I've stored these" box is ticked · `download: void` · `copy: void` |

### 1.4 Cases page

| Component | Lives in | Props | Emits |
|---|---|---|---|
| **CasesPage** | Router `/cases?filter=&sort=&sel=` | — (URL state via `useUrlState`) | — |
| **FilterHub** | CasesPage | `filters: FilterCardVm[]`, Y · `activeFilterId: number\|null`, Y | `select: { filterId: number }` on card click |
| **HubCard** | FilterHub, ConfigHubPage | `title: string`, Y · `icon: string` (Lucide name), Y · `count: number\|null`, N, `null` · `summary: string`, N, `''` · `stateLine: string`, N, `''` · `stateTone: 'neutral'\|'warning'\|'error'\|'success'`, N, `'neutral'` · `active: boolean`, N, `false` · `to: string\|null`, N, `null` (router link) | `activate: void` on click or Enter when `to` is null |
| **CasesToolbar** | CasesPage | `sort: 'lane,priority'\|'priority'\|'age'\|'due'`, Y · `sessionModified: boolean`, Y · `riskMatrixOpen: boolean`, Y | `update:sort: string` · `save-filter: { name: string }` from the SaveFilterButton dialog (CP-19) · `toggle-risk-matrix: void` · `edit-session-filter: void` opens the RuleBuilderIsland dialog |
| **RiskMatrix** | CasesPage, TeamViewPage | `cells: { band: LikelihoodBand; impact: ImpactBand; count: number }[]`, Y · `interactive: boolean`, N, `true` | `select-cell: { band: LikelihoodBand; impact: ImpactBand }` on click when `interactive` |
| **DataGrid** | many | `columns: GridColumnVm[]`, Y · `rows: unknown[]`, Y · `rowKey: string`, Y · `selectable: 'none'\|'single'\|'multi'`, N, `'single'` · `density: 'comfortable'\|'compact'`, N (from store) · `pinnedKeys: (string\|number)[]`, N, `[]` · `loading: boolean`, N, `false` · `emptyText: string`, N, `''` · `serverSort: boolean`, N, `false` | `row-activate: { key; row }` on Enter or double-click · `row-select: { keys: (string\|number)[] }` on selection change · `sort-change: { field: string; dir: 'asc'\|'desc' }` when `serverSort` · `cell-action: { key; field: string; action: string }` on an in-cell button (for example `+ Tag`, `Card`) |
| **RiskChip** | grid cell, CaseHeader, PeekDrawer | `risk: RiskChipVm`, Y · `size: 'sm'\|'md'`, N, `'md'` · `showMethod: boolean`, N, `false` | `open-popover: void` on click or `R` (header only) |
| **LockCell** | grid cell | `lockedBy: string\|null`, Y · `mine: boolean`, Y · `canBreak: boolean`, Y | `break: void` on "Break…" |
| **BulkBar** | CasesPage | `selectedCount: number`, Y · `allowedOps: OpName[]`, Y | `bulk-op: { op: 'Assign'\|'ChangePriority'\|'Merge'\|'Close' }` on click (never `ReleasePayment`, G10) · `clear: void` |
| **PeekDrawer** | CasesPage | `open: boolean`, Y · `caseId: CaseId\|null`, Y | `close: void` on Esc or Space · `open-case: { caseId }` on Enter · `decide-payment: { caseId; holdId: number }` on H |
| **HeldPaymentCard** | PeekDrawer, SummarySection | `hold: PaymentHoldVm`, Y (includes `policy: HoldPolicyVm` from CG-25 and `notice: { state: 'NotRequired'\|'Due'\|'Sent'\|'Overdue'; at?: IsoDateTime }`) · `compact: boolean`, N, `false` | `decide: { holdId: number }` on button or H |

### 1.5 Case page

| Component | Lives in | Props | Emits |
|---|---|---|---|
| **CasePage** | Router `/case/:id?sec=&key=&sel=&lens=` | `id: CaseId` (route param), Y | — |
| **CaseHeader** | CasePage | `anchor: CaseAnchorVm`, Y (from CH-00) · `presence: PresenceVm[]`, Y · `lock: LockVm`, Y | `open-risk: void` · `decide-payment: { holdId }` · `take-lock: void` |
| **LaneBadge** | CaseHeader | `lane: Lane`, Y · `reason: string`, N, `''` | — |
| **DiaryMeter** | CaseHeader, DecideCockpit, PeekDrawer | `diary: DiaryVm`, Y · `compact: boolean`, N, `false` | `record-breach-reason: { diaryId }` when `state='Breached'` |
| **PresenceAvatars** | CaseHeader | `users: PresenceVm[]`, Y | — |
| **LockBadge** | CaseHeader | `lock: LockVm`, Y | `break: void` when `lock.canBreak` |
| **LiveChangeBanner** | CasePage | `changes: CaseChangeVm[]`, Y | `review: { section: SectionId }` · `dismiss: void` |
| **CaseKeyPanel** | CasePage | `keys: CaseKeyVm[]`, Y · `focusKey: string`, Y | `pivot: { key: string; value: string }` on identifier click · `link: { key; value }` · `reveal: { field: string }` for a masked field (audited) |
| **IdentifierRow** | CaseKeyPanel | `item: CaseKeyVm`, Y | `pivot` · `link` · `reveal` (same payloads) |
| **SectionNav** | CaseMain | `sections: { id: SectionId; label: string; badge?: number; changed?: boolean }[]`, Y · `active: SectionId`, Y | `navigate: { id: SectionId }` on click or keys 1–6 |
| **SummarySection** | CaseMain | `anchor: CaseAnchorVm`, Y · `holds: PaymentHoldVm[]`, Y · `agentRun: AgentRunVm\|null`, N, `null` | `jump: { section: SectionId; activationId?: number }` · `decide-payment: { holdId }` |
| **CaseDetailView** | SummarySection | `view: DetailViewVm`, Y (server-resolved tokens, never HTML) | `jump` (same) |
| **ActivationsSection** | CaseMain | `activations: ActivationVm[]`, Y | `open-card: { archiveId }` · `open-model-facts: { modelId; snapshotId }` · `feedback: { activationId; contributionKey: string }` · `add-fact: { activationId; factKey: string }` · `suppress: { activationId }` (opens SuppressForm) |
| **ActivationCard** | ActivationsSection | `activation: ActivationVm`, Y · `view: 'waterfall'\|'table'`, N, `'waterfall'` | the same four emits, plus `update:view` |
| **ScoreStrip** | ActivationCard | `score: ScoreVm`, Y | — |
| **JubeChart** | many | `src: string` (endpoint URL returning SVG), Y · `params: Record<string,string\|number>`, N, `{}` · `height: number`, Y · `title: string`, Y (becomes `aria-label`) | `error: { message: string }` on fetch failure |
| **SensitivityLine** | ActivationCard | `sensitivity: SensitivityVm\|null`, Y | — |
| **RulePathView** | ActivationCard | `path: RulePathVm`, Y (S2-09) | `open-card: { archiveId }` |
| **SanctionsMatchCard** | ActivationCard | `match: SanctionsMatchVm`, Y (KP-04) | `open-drawer: void` |
| **JournalSection** | CaseMain | `focus: { key: string; value: string; isSubject: boolean }`, Y · `lens: 'Subject'\|'Merge'\|'Pin'\|'Explode'`, Y | `pivot` · `back: void` (Alt+←) · `update:lens` · `open-card: { archiveId }` · `tag: { archiveIds: number[]; tagId: number; on: boolean }` |
| **LensBar** | JournalSection | `lens`, Y · `mergeKeys: MergeKeyVm[]`, Y (max 4) | `update:lens` · `add-key: void` · `remove-key: { key; value }` |
| **PivotBanner** | JournalSection | `trail: { key: string; value: string; label: string }[]`, Y | `go: { index: number }` · `back-to-subject: void` |
| **AggregationStrip** | JournalSection | `items: AggregateVm[]`, Y (S3-14) | — |
| **ExplodeGrid** | JournalSection, NetworkSection | `lanes: ExplodeLaneVm[]`, Y (S3-06/S4-09) | `select-node: { nodeId: string }` |
| **TagMenu** | JournalSection, TransactionCardDrawer | `open: boolean`, Y · `anchorEl: HTMLElement\|null`, Y · `groups: TagGroupVm[]`, Y · `applied: number[]`, Y | `toggle: { tagId: number; on: boolean }` · `close: void` |
| **NetworkSection** | CaseMain | `graph: GraphVm`, Y (S4-01) · `layout: 'Force'\|'Flow'\|'Hierarchy'`, Y · `selectedArchiveId: number\|null`, N | `select-node: { nodeId }` · `select-edge: { edgeId }` → Journal filter (S4-05) · `update:layout` · `capture: void` (Graph/Capture) |
| **GraphCanvas** | NetworkSection | `nodes: GraphNodeVm[]`, Y (≤ 150) · `edges: GraphEdgeVm[]`, Y · `layout`, Y · `highlightEdgeIds: string[]`, N, `[]` | `select-node` · `select-edge` |
| **NodePanel** | NetworkSection | `node: GraphNodeVm\|null`, Y | `pivot` · `link` · `propose-list-change: { key; value }` |
| **NotesSection** | CaseMain | `notes: NoteVm[]`, Y · `actions: ActionCodeVm[]`, Y | `record: { actionId: number; resultId: number\|null; text: string; mentions: number[] }` |
| **ActionComposer** | NotesSection | `actions: ActionCodeVm[]`, Y · `busy: boolean`, Y | `record` (same) on Ctrl+Enter when an action is chosen and the text is non-empty |
| **NoteList** | NotesSection | `notes: NoteVm[]`, Y | — |
| **CaseJournalSection** | CaseMain | `events: CaseEventVm[]`, Y · `riskHistory: RiskAssessmentVm[]`, Y · `chain: { verified: boolean\|null; checkedAt: IsoDateTime\|null }`, Y | `verify-chain: void` (S6-03) |
| **HashChainBadge** | CaseJournalSection | `chain`, Y | `verify: void` |
| **DecideCockpit** | RightColumn | `available: AvailableOpVm[]`, Y (from `GET /api/CaseOperation/Available`) · `checklist: ChecklistItemVm[]`, Y (DC-01) · `diaries: DiaryVm[]`, Y · `suggested: AvailableOpVm\|null`, Y | `open-op: { op: OpName; defaults?: Record<string,unknown> }` · `do-now: { checklistItemId: number }` |
| **AskJoobyPanel** | RightColumn | `caseId: CaseId`, Y · `suggested: AvailableOpVm\|null`, Y | `open-op: { op; defaults }` (from a Finding card's primary button only) · `toggle-back: void` (Alt+J) |
| **ProposalCard** | AskJoobyPanel | `proposal: ProposalVm`, Y (`kind: 'Finding'\|'Opinion'`) | `accept: { proposalId; op: OpName }` (Finding only) · `ask-evidence: { proposalId }` (Opinion only) · `dismiss: { proposalId }` |
| **CaseRiskPopover** | CasePage | `open: boolean`, Y · `risk: CaseRiskVm\|null`, Y (CH-03) | `assess: { level: 'Low'\|'Medium'\|'High' }` (Op: AssessRisk) · `close: void` |
| **TransactionCardDrawer** | CasePage | `archiveId: number\|null`, Y | `close` · `open-payload: void` · `pivot` · `around: void` (S3-10) · `add-fact` · `tag` |
| **PayloadDrawer** | CasePage | `archiveId: number\|null`, Y | `close` |
| **ModelFactsDrawer** | CasePage | `modelId: number\|null`, Y · `snapshotId: Guid\|null`, Y | `close` · `open-diff: { from: Guid; to: Guid }` (MF-02) |
| **ComposeMode** | CasePage | `caseId`, Y · `reportType: string`, Y | `save-version: void` (Ctrl+S) · `exit: void` (Esc) · `file: void` → opens FileForm |
| **FactsTray** | ComposeMode | `facts: FactVm[]`, Y | `insert: { factId: string; slot?: string }` |
| **NarrativeEditor** | ComposeMode | `sections: NarrativeSectionVm[]`, Y (each section carries its `useNarrative` provenance and `stale`) | `update:sections` · `accept-inference: { sentenceId }` · `reject-inference: { sentenceId }` · `draft: { section }` (frame `NarrativeSection`) · `regenerate: { section }` |
| **QualityScorecard** | ComposeMode | `items: ScorecardItemVm[]`, Y | `fix: { itemId: string }` · `acknowledge: { itemId: string }` (amber only, audited) |
| **FramingList** | ComposeMode | `framings: FramingVm[]`, Y | `use: { framingId }` |

### 1.6 Operation lightbox (one shell for every operation)

| Component | Lives in | Props | Emits |
|---|---|---|---|
| **OperationLightbox** | CasePage, CasesPage | `op: OpName\|null`, Y (null = closed) · `caseIds: CaseId[]`, Y · `defaults: Record<string,unknown>`, N, `{}` | `close: { executed: boolean }` · `executed: { operationId: Guid; op: OpName }` |
| **ConsequenceList** | OperationLightbox | `items: ConsequenceVm[]`, Y (server Preview) · `stale: boolean`, Y | — |
| **StepUpPanel** | OperationLightbox | `reason: string`, Y · `methods: ('Totp'\|'Passkey'\|'IdpReauth')[]`, Y · `boundTo: { previewHash: string } \| null`, Y (G8: payment decisions) | `verified: { stepUpToken: string; method: string }` · `cancel: void` ("Back to the form") |
| **ExecutionResult** | OperationLightbox | `steps: ExecStepVm[]`, Y · `phase: 'sending'\|'confirmed'\|'unknown'\|'failed'`, Y | `retry-safely: void` (same idempotency key, H5) · `done: void` |
| **CloseForm** | lightbox slot | `model: CloseInput`, Y · `typologySuggestions: TypologyVm[]`, Y · `templates: RationaleTemplateVm[]`, Y | `update:model: CloseInput` |
| **FileForm** | slot | `model: FileInput`, Y · `approval: ApprovalStateVm`, Y | `update:model` · `request-approval: { approverId: number }` |
| **ApproverView** | slot | `approval: ApprovalVm`, Y (LB-A) | `approve: void` · `reject: { reason: string }` (reason ≥ 10 characters) |
| **BreakLockForm** | slot | `model: { reason: string }`, Y · `holder: LockVm`, Y | `update:model` |
| **BulkCloseForm** | slot | `model: CloseInput`, Y · `count: number`, Y | `update:model` |
| **PaymentDecisionForm** | slot | `hold: PaymentHoldVm`, Y · `model: PaymentDecisionInput`, Y · `userLimit: Money`, Y | `update:model` (keys 1–3 set `decision`) |
| **LinkKeyForm** | slot | `model`, Y · `target: { key; value }`, Y | `update:model` |
| **SuppressForm** | slot (from ActivationCard, CloseForm FP suggestion, SuppressionsPage) | `model: { keyField: string; keyValue: string; scope: 'Rule'\|'Model'; ruleIds: number[]; modelId: number; until: IsoDateTime; reasonCode: string; reasonText: string; noteId: number\|null; caseId: CaseId\|null }`, Y · `keyOptions: { field: string; value: string }[]`, Y (only suppression-enabled fields on the transaction) · `policy: { defaultDays: number; maxDays: number }`, Y | `update:model` (scope, key, duration and until are consequence inputs, so they re-preview) |
| **ProposeListChangeForm** | slot (NodePanel, IdentifierRow, PaymentDecisionForm) | `model: { listId: number; op: 'Add'\|'Remove'; value: string; reason: string }`, Y · `lists: { id; name; modelName; usedByRules: number }[]`, Y | `update:model` |
| **RfiForm** (M3) | slot | `model: RfiInput`, Y · `templates: RfiTemplateVm[]`, Y · `tippingOff: TippingOffCheckVm`, Y | `update:model` |
| **QaReviewForm** (M2) | slot | `model: QaReviewInput`, Y · `checklist: QaCheckVm[]`, Y | `update:model` |

### 1.7 Other pages

| Component | Lives in | Props | Emits |
|---|---|---|---|
| **TeamViewPage** | Router `/team`, `/team/wall` | `mode: 'desk'\|'wall'`, Y (from route) | — |
| **AnalystTable** | TeamViewPage | `analysts: AnalystLoadVm[]`, Y · `selectable: boolean`, Y (false on wall) | `reassign: { analystIds: number[] }` · `offer-absence: { analystId }` |
| **FilterLoadTable** | TeamViewPage | `rows: FilterLoadVm[]`, Y | `open-filter: { filterId }` |
| **PendingChangesCard** | TeamViewPage | `changes: PendingChangeVm[]`, Y | `review: { changeId }` |
| **QaSamplingCard** (M2) | TeamViewPage | `plan: QaPlanVm`, Y | `open-queue: void` · `edit-plan: void` |
| **QaReviewPage** (M2) | Router `/qa` | — | — |
| **ReferralPage** | Router `/refer/:tenantSlug` (public) | `tenant: { name: string; logoUrl: string }`, Y | — |
| **RfiPortalPage** (M3) | Router `/rfi/:token` (public, single-use token) | `token: string`, Y | — |
| **ReportStatusPage** | Router `/report/:id` | `id: number`, Y | — |
| **AutomationRecipesPage** (M5) | Router `/config/automations` | — | — |
| **RecipeBuilder** (M5) | AutomationRecipesPage | `recipe: RecipeVm`, Y · `catalogue: RecipeStepDefVm[]`, Y | `update:recipe` · `test: void` (dry run against the last 30 days) · `publish: void` (maker-checker) |
| **RecipeRunLog** (M5) | AutomationRecipesPage | `runs: RecipeRunVm[]`, Y | `open-run: { runId }` |
| **ConfigHubPage** | Router `/config/:path*` | `path: string[]`, Y | — |
| **Breadcrumbs** | ConfigHubPage, DtoFormPage | `items: { label: string; to: string }[]`, Y | — |
| **DtoGridPage** | Router `/config/:entity` | `entity: string`, Y (a `[FormEndpoint]` name) · `parentId: number\|null`, N | — |
| **DtoFormPage** | Router `/config/:entity/:id` | `entity: string`, Y · `id: number\|'new'`, Y · `parentId: number\|null`, N | — |
| **DtoForm** | DtoFormPage | `schema: FormSchemaVm`, Y (from `[FormGroup]`/`[FormField]`) · `modelValue: Record<string,unknown>`, Y · `readonly: boolean`, N, `false` (true when `ILockable` is locked) | `update:modelValue` · `submit: void` when client validation passes (the server re-validates) · `cancel: void` |
| **FormGroup** | DtoForm | `group: FormGroupVm`, Y · `modelValue`, Y | `update:modelValue` |
| **FormFieldRenderer** | FormGroup | `field: FormFieldVm`, Y · `modelValue: unknown`, Y · `visible: boolean`, Y · `enabled: boolean`, Y · `required: boolean`, Y · `error: string\|null`, N | `update:modelValue: unknown` |
| widgets | FormFieldRenderer | `modelValue`, Y · `field: FormFieldVm`, Y · `disabled: boolean`, Y · `invalid: boolean`, Y | `update:modelValue` |
| **LookupField** | widget | + `lookup: { endpoint: string; valueKey: string; labelKey: string }`, Y | `update:modelValue` |
| **RoleAllocationField** | widget | + `available: RoleVm[]`, Y | `update:modelValue: number[]` |
| **CodeEditorField** (JubeCoder) | widget, RuleBuilderIsland | + `language: 'jube-rule'\|'sql'\|'json'`, Y · `completions: CompletionVm[]`, N, `[]` | `update:modelValue: string` · `validate: void` (server parse) |
| **RuleBuilderIsland** | QueryBuilderField, CasesToolbar dialog | `modelValue: RuleJson`, Y · `fields: QueryFieldVm[]`, Y · `mode: 'builder'\|'coder'`, N, `'builder'` | `update:modelValue: RuleJson` · `update:mode` · `compile: void` (server compile, for example `POST /api/SessionCaseSearchCompiledSql`) |
| **UserSettingsPage** | Router `/me` | — | — |
| **SuppressionsPage** | Router `/suppressions?key=&value=&status=` | — (URL state) | — |
| **SuppressionGrid** | SuppressionsPage | `rows: SuppressionVm[]`, Y · `selectable`, Y | `open: { id }` · `revoke: { ids: number[] }` (opens the revoke lightbox; reason required) |
| **SuppressionDetail** | SuppressionsPage | `suppression: SuppressionVm`, Y · `history: SuppressionEventVm[]`, Y (CG-31) | `renew: void` · `revoke: void` · `open-matches: void` |
| **SuppressionBadge** | CaseKeyPanel, ActivationCard, Journal row | `items: SuppressionVm[]`, Y · `variant: 'chip'\|'card'\|'icon'`, Y | `open: { id }` · `revoke: { id }` |
| **AdaptationUnavailableLine** | ActivationCard, ModelFactsDrawer | `name: string`, Y · `error: string`, Y · `affectedRules: string[]`, Y | — |
| **ListChangeSetEditor** | DtoFormPage for `EntityAnalysisModelList` / `…Dictionary` | `list: ListVm`, Y · `draft: ChangeSetVm`, Y (CG-34) · `backtest: BacktestVm\|null`, N | `add` · `remove` · `upload-csv: File` · `run-backtest` · `submit` (maker-checker) · `emergency-publish: { reason }` |
| **AgentRunCard** (M1) | SummarySection | `run: AgentRunVm`, Y (CG-01) · `canRerun: boolean`, Y | `accept: { op: 'Close'; defaults: CloseInput }` → the parent opens OperationLightbox pre-filled · `override: { reason: string }` (≥ 10 characters, CG-03) · `rerun: void` (CG-02) |
| **RfiThread** (M3) | NotesSection | `rfis: RfiVm[]`, Y | `open-upload: { uploadId: number }` · `cancel: { rfiId: number }` (CG-11) |
| **WebResearchCard** (M4) | AskJoobyPanel | `result: ResearchVm`, Y (CG-12; `citations: { url; retrievedAt; quote }[]`) | `save: void` (CG-13) · `add-fact: { factText: string }` · `dismiss: void` |
| **ShadowTestPanel** (M6) | ActivationsSection (drawer), rule config page | `ruleId: number`, Y · `performance: RulePerformanceVm`, Y (CG-20) · `backtest: BacktestVm|null`, Y (CG-21) | `run-backtest: { proposal: RuleJson }` · `start-shadow: { days: number }` (CG-22) · `open-missed: { caseIds: CaseId[] }` |
| **PasskeyCard** | UserSettingsPage | `enabled: boolean`, Y (feature flag, phase 2) | `register: void` |

---

## 2. Unified composables and state

### 2.1 Composables (`src/composables/*.ts`)

Each entry lists the state it returns (refs and computeds) and its methods. Methods are `async` unless marked. Every network call goes through `useApi`.

**useApi** (the only place `fetch` is allowed)
- State: `inFlight: Ref<number>` (0).
- `request<T>(method: 'GET'|'POST'|'PUT'|'DELETE', url: string, body?: unknown, opts?: { idempotencyKey?: string; stepUpToken?: string; signal?: AbortSignal }): Promise<T>`
  1. Build the absolute URL on the same origin, always `/api/...`. A cross-origin base URL is a build error.
  2. `credentials: 'omit'`. Set `Authorization: Bearer <token>` from `useTokenStore().token`. Never decode the JWT.
  3. After every response, if `X-Refreshed-Token` is present, call `useTokenStore().set(token, X-Token-Expires)`. No antiforgery header (D-V27).
  4. Add `Idempotency-Key` and `X-Step-Up-Token` when given.
  5. On 401 with `step_up_required`, throw `StepUpRequiredError { reason, methods, previewHash? }`.
  6. On 401 without it, call `useAuthSession().handleExpired()` and throw `SessionExpiredError`.
  7. On 409 with `case_version_conflict`, throw `ConcurrencyError { currentVersion }`.
  8. On 400, throw `ValidationError { fieldErrors: Record<string,string[]> }` (ProblemDetails).
  9. On timeout (default 30 s, and 30 s for payment Execute), throw `OutcomeUnknownError` for POSTs that carry an idempotency key, otherwise `NetworkError`.
- `get<T>(url, params?)`, `post<T>(url, body, opts?)`: thin wrappers.

**useTokenStore** (D-V27; the only place the JWT is held)
- `token: Ref<string|null>` · `expiresAt: Ref<Date|null>` · `set(token, expiresAt): void` · `clear(): void`.
- Holds the token in memory and mirrors it to `sessionStorage` under one key so a reload of the same tab keeps the session. Never `localStorage`, never `BroadcastChannel` or any cross-tab sharing. `clear()` runs on sign-out and on `SessionExpiredError`.

**useAuthSession**
- `expiresAt: Ref<Date|null>`, read from `useTokenStore().expiresAt` only. `secondsLeft: ComputedRef<number>`.
- **No refresh call exists or is needed.** `TokenRefreshMiddleware` reissues the JWT on every authenticated request in the `X-Refreshed-Token` header (sliding 15 min, capped by `AbsoluteSessionLifetime`).
- `keepAlive(): void` (sync): while the user is active (key, pointer or scroll within the last 5 min) and there has been no API call for 10 min, send `GET /api/Me`. This covers long idle reads such as Compose. With no user activity, let the session lapse.
- `watchExpiry(): void` (sync): re-read `useTokenStore().expiresAt` after each response. At 60 s before absolute expiry, show a toast: "Session ends in 1 minute: save your work".
- `handleExpired(): void`: store `route.fullPath`, then `router.replace('/login?returnUrl=…')`.
- **Must not** decode the JWT or touch `localStorage`. `sessionStorage` is used for auth only through `useTokenStore`.

**useLoginJourney**
- `step: Ref<'login'|'changePassword'|'mfa'|'enrolTotp'|'recovery'|'done'>`, `busy`, `error`.
- `submitLogin(u: string, p: string)` calls the existing login endpoint (the Account/Login handler behind today's `Pages/Account/Login`; confirm the exact route in V2); the next step comes from the response flags (`mustChangePassword`, `mfaRequired`, `mustEnrol`).
- `submitChangePassword(cur, next)`; `submitMfa({code?, assertion?})`; `confirmTotp(code)`; `finish()` goes to `returnUrl`.

**useStepUp**
- `pending: Ref<StepUpRequiredError|null>`, `method`, `busy`.
- `begin(method: 'Totp'|'Passkey'|'IdpReauth'): Promise<void>` calls `POST /api/Authentication/StepUp/Begin` (LB-06). For a passkey, run `navigator.credentials.get(options)`. For IdpReauth, open the IdP with `prompt=login&max_age=0` in a popup.
- `complete(payload): Promise<string>` calls `POST /api/Authentication/StepUp/Complete` with `{ method, code?|assertion?, previewHash? }` and returns `stepUpToken`.
- Window: the server enforces 5 minutes; for payment decisions the token is bound to `previewHash` (G8). The client never caches a token beyond the one operation that requested it.

**useOperation(op: Ref<OpName|null>, caseIds: Ref<CaseId[]>)** (the lightbox brain)
- State: `input: Ref<Record<string,unknown>>` · `preview: Ref<PreviewVm|null>` · `previewStale: Ref<boolean>` · `phase: Ref<'editing'|'previewing'|'stepUp'|'executing'|'result'>` · `result: Ref<ExecResultVm|null>` · `error: Ref<string|null>`.
- `canSubmit: ComputedRef<boolean>` = `phase==='editing' && preview && !previewStale && preview.unmet.length===0 && validators(op).every(v => v(input))`.
- `consequenceInputs: ComputedRef<string[]>` = `preview?.consequenceInputs ?? []` (D26: only these keys re-preview).
- `open(defaults)`: reset, merge the defaults, then `runPreview()`.
- `runPreview()`: `POST /api/CaseOperation/{op}/Preview` with `{ caseIds, caseVersions, input }`. Store the preview and set `previewStale = false`. Debounce 300 ms; abort any in-flight preview.
- Watch `input`: if a changed key is in `consequenceInputs`, set `previewStale = true` and `runPreview()`.
- `submit()`:
  1. Generate `idempotencyKey` (UUID v4) once per open; reuse it on every retry.
  2. `POST /api/CaseOperation/{op}/Execute` with `{ previewHash, caseVersions, input }` and the headers.
  3. On `StepUpRequiredError`, set `phase = 'stepUp'`, wait for `useStepUp`, then re-submit with the token and the same key.
  4. On `ConcurrencyError`, re-preview and show the "case changed" state (A3). Submit stays disabled until the user acknowledges.
  5. On `OutcomeUnknownError`, set `phase = 'result'` with `result.phase = 'unknown'`.
  6. On success: `phase = 'result'`. If `preview.undoableSeconds > 0`, `useUiStore().pushUndoToast(operationId, seconds)`.
- `retrySafely()` re-sends Execute with the same key. `acknowledgeChange()` clears the stale banner.

**useUndo**
- `undo(operationId: Guid)` calls `POST /api/CaseOperation/Undo/Execute` with `{ operationId }` (D19). It is only callable while the toast is alive.

**useCaseAnchor(id: Ref<CaseId>)**
- `anchor: Ref<CaseAnchorVm|null>`, `loading`, `error`, `version: ComputedRef<number>`.
- `load()` calls `GET /api/Case/{id}/Anchor` (CH-00, < 300 ms budget); the server writes `CaseViewed` (D-V8). It is a `useQuery` on `qk.case(id)`. There is no client-side patching: a `ServiceChange` for `Case/{id}` invalidates it and it refetches (§2.4).

**usePresence(id: Ref<CaseId>)** (the only per-case hub; presence is not in `ServiceChangeEvent`)
- `presence: Ref<PresenceVm[]>`.
- `connect()` joins the new `CaseHub` (`/caseHub`), same origin, `accessTokenFactory: () => useTokenStore().token` (D-V27), and calls `Join(caseId)`. The server re-checks permission. It subscribes to `Presence` only.
- `disconnect()` calls `Leave(caseId)` on unmount or route leave.
- Case changes, risk changes and hold changes do **not** come through here. They arrive as `ServiceChange` invalidations (§2.4). This supersedes the audit's CH-02, CH-10 and CH-11 push payloads.

**useSection loaders** are `useQuery` wrappers keyed by the §2.4 key factory. Each returns `{ data, isLoading, isFetching, error, refetch }`. Show a skeleton only on `isLoading`, the first load. A background refetch (`isFetching`) shows at most a quiet "Updating…" label and never blanks content.
- `useActivations(caseId)` → S2-01 · `useJournal(focus, lens, window)` → S3-01 (+ tags[]), S3-14, S3-06 · `useGraph(caseId, focus, layout)` → S4-01 · `useNotes(caseId)` → notes + S5-03/S5-04 · `useCaseEvents(caseId)` → S6-02, S6-05 · `useChecklist(caseId)` → DC-01 · `useAvailableOps(caseId)` → `GET /api/CaseOperation/Available` · `usePaymentHolds(caseId)` → `GET /api/Case/{id}/PaymentHolds` · `useTransactionCard(archiveId)` → TC-01 · `useModelFacts(modelId, snapshotId)` → MF-01 · `useCaseRisk(caseId)` → CH-03 · `useAgentRun(caseId)` → CG-01 (M1).
- Loading rule: sections 1 and 2 come from CH-00. Sections 3–6 load on first scroll into view (IntersectionObserver) or on key 1–6, never all at once.

**useUrlState<T>(schema: Record<keyof T, 'string'|'number'|'list'>)**
- `state: Ref<T>`, two-way synced with `route.query`: `router.replace` for UI state, `router.push` for navigation. The source of truth for section, focus key, selection, lens, filter and sort (D: "state in the URL").

**useKeyboard(scope: string)**
- `bind(combo: string, handler: () => void, opts?: { when?: () => boolean; description: string })`. It is ignored while focus is in an input, textarea or contenteditable, except Esc and Ctrl/Cmd+Enter. Bindings are registered with the scope so the `?` sheet lists them. Scopes: `global`, `cases`, `case`, `lightbox` (lightbox shadows case).

**useDtoForm(entity: Ref<string>, id: Ref<number|'new'>)**
- `schema: Ref<FormSchemaVm|null>` from `GET /api/{entity}/Schema` (new, CG-14, serialises the `[FormEndpoint]` metadata) · `model` · `original` · `dirty: ComputedRef<boolean>` · `fieldState: ComputedRef<Record<string,{visible,enabled,required}>>`, evaluated from `[VisibleWhen]/[EnabledWhen]/[RequiredWhen]` with the same `ConditionOp` semantics as the server · `errors`.
- `load()`: schema, then `GET /api/{entity}/{id}`, or `[NewDefault]` values for `'new'`. `save()`: POST/PUT; the server re-evaluates the conditions and validation, and 400 maps to `errors`. `activate(on)` and `lock()` for `IActivatable`/`ILockable`.

**useDtoGrid(entity, parentId)**: `columns` from `[ListColumn]`, rows, paging, server sort.

**useForceLayout(nodes, edges, opts)** (sync, pure): deterministic seed from case id, 300 iterations, returns positions. Unit-tested; no DOM.

**useI18n** (D-V29): wraps vue-i18n; `load(locale)` dynamically imports `@/locales/{locale}.json` (one chunk per locale) and sets `<html lang dir>`. English is bundled eagerly as the fallback. Keys are typed from `en.json`. ICU message format for plurals; dates, numbers and money via `Intl`. Tenant-configured labels still come from the API with their translations (spec 14.7); they are data, not locale-file keys.

**useTheme**: `theme: Ref<'light'|'dark'>`, `density: Ref<'comfortable'|'compact'>`, persisted to `localStorage` (UI preference only), applied to `<html>` data attributes.

### 2.2 Local state machines (bind to these exact states)

- **OperationLightbox:** `closed → editing ⇄ previewing → (stepUp →) executing → result(confirmed|unknown|failed) → closed`. `stale` is a flag on `editing`, not a state.
- **Payment decision** adds `result.unknown → executing` via "Retry safely", with the same key.
- **Case page connection:** `online → reconnecting → online|offline`. While `offline`, every `OperationButtons` is disabled and unsaved composer text persists to `sessionStorage` under `draft:<caseId>` (not auth data).
- **Login journey:** `login → (changePassword)? → (mfa)? → (enrolTotp → recovery)? → done`.

### 2.4 Server state and live invalidation (ServiceChangeHub + TanStack Query)

**What already exists server-side (no feature work):**
- Every service-layer write publishes an event. `OperationScope` (`Jube.Service/Observability/OperationScope.cs`) fires `ServiceChangeEvent(Area, TenantRegistryId, Kind, EntityId, Version, Actor, At, CorrelationId)` on `IServiceChangeBus` whenever an operation succeeds and names its entity.
- `Startup.cs` picks the bus from the deployment shape: `RedisServiceChangeBus` (channel `jube:svc-change:{tenantId}`), `InProcessServiceChangeBus`, or `NullServiceChangeBus`.
- `ServiceChangeRelay` re-broadcasts each event to `ServiceChangeHub` (`/serviceChangeHub`) as a `"ServiceChange"` message, to the group `Tenant_{id}`. The hub auto-joins that group in `OnConnectedAsync` from the user's claims, so the client only connects. It never calls "join".
- No browser client listens today. This package switches it on.

**The payload is thin by design.** It says "this entity is stale", never the new value, so the client pattern is **cache invalidation**, not a live data store:

```ts
type ServiceChangeEvent = {
  area: string; tenantRegistryId: number;
  kind: string;                       // e.g. 'Created' | 'Updated' | 'Deleted'; use the server enum names verbatim
  entityId: number | string; version: number;
  actor: string; at: IsoDateTime; correlationId: string;
}
```

**Query keys** (`src/api/queryKeys.ts`, the only place keys are built):
- The first element is the **Area string exactly as `ServiceChangeEvent.Area` emits it**, taken from the server's area constants. Never retype it by hand.
- The second is the `entityId`, or `'list'` for collection queries. Anything after that is sub-resource or parameters.
- Examples: `qk.case(42) = ['Case', 42]`, `qk.caseJournal(42, focus, lens) = ['Case', 42, 'Journal', focus, lens]`, `qk.caseList(filterId, sort) = ['Case', 'list', filterId, sort]`, `qk.dto(entity, id) = [entity, id]`, `qk.dtoList(entity, parentId) = [entity, 'list', parentId]`.
- Prefix matching means invalidating `['Case', 42]` refreshes everything nested under it: anchor, sections and drawers.

**useServiceChanges()** is an app-wide singleton, started once in `main.ts` after the session is known. It is never per page or per component.
1. Build the connection with `new HubConnectionBuilder().withUrl('/serviceChangeHub').withAutomaticReconnect([0, 2000, 5000, 10000, 30000]).build()`, the same pattern as today's `Activation.js` for `/watcherHub`, made a singleton. Same origin, with `accessTokenFactory: () => useTokenStore().token` (D-V27).
2. `on('ServiceChange', e => queue(e))`. **Dedupe:** skip if the `version` is at or below the last seen version for `[area, entityId]`. Skip if `correlationId` is in the recent own-writes set (below).
3. **Batch:** flush the queue every 250 ms. For each distinct `[area, entityId]`, call `queryClient.invalidateQueries({ queryKey: [area, entityId] })` and `queryClient.invalidateQueries({ queryKey: [area, 'list'] })`. Then apply the dependency map.
4. **Dependency map** (`src/api/invalidationMap.ts`), for extra keys an area also stales. Initial rows follow; confirm the area names against the server constants:

| Area | Also invalidate |
|---|---|
| `Case` | `['Narrative', *, 'case:'+id]` (marked stale, never auto-regenerated), `['CaseFilterCount']`, `['TeamView']`, `['RiskMatrix']`, `['Inbox']` |
| `CaseNote`, `CaseEvent`, `CaseUpload` | `['Case', caseId]` (the event must name the case, as the entity or its parent) |
| `ArchiveTag` | `['Case', '*', 'Journal']` via a predicate on key[2] === 'Journal' |
| `Suppression` | `['Suppression','list']`, `['Case', caseId]` when linked, `['TeamView']` |
| `EntityAnalysisModelList`, `EntityAnalysisModelDictionary` | `[area, id]`, `[area, 'list']`, `['PendingChange','list']` |
| `CaseWorkflowFilter` | `['CaseFilter', 'list']`, `['CaseFilterCount']` |
| `PaymentHold`, `CaseRisk` | `['Case', caseId]`, `['Case', 'list']`, `['TeamView']` |
| any configuration entity | `[entity, 'list']` only (the default rule) |

5. `connectionState` (`online | reconnecting | offline`) feeds `ConnectionBanner`.

**Force background refresh:** the bus has no replay, so missed events are covered by these rules.

| Trigger | Action |
|---|---|
| Hub `onreconnected` | `queryClient.invalidateQueries()` on **all active** queries, because events may have been missed during the gap. Inactive queries are marked stale and refetch on next mount. |
| Tab visible again after more than 5 min hidden | The same full invalidation. |
| Window focus | `refetchOnWindowFocus: true`, gated by `staleTime`. |
| Stale-state "Refresh" button (§3.1) | `refetch()` on that query only. |
| Money-critical views (held payments, Live money lane, hold countdowns) | A `refetchInterval: 30_000` safety net as well as invalidation. This is the only polling in the app. |
| Own successful Execute (`useOperation`) | Invalidate the operation's keys **immediately**, without waiting for the echo. Record `correlationId` (the server returns it on Execute) in the own-writes set for 30 s, so the echo is skipped. |

**QueryClient defaults:**
- `staleTime: 30_000`, `gcTime: 300_000`;
- `retry: 2` with exponential backoff, for queries only;
- **mutations are never retried by vue-query** (`useOperation` owns retry with the idempotency key);
- `placeholderData: keepPreviousData` for grids and paged lists, so rows don't flash on refetch.

**Dirty-edit guard:** never refetch into what the user is editing.
- If the entity behind a dirty `DtoForm`, an open lightbox or an unsaved composer is invalidated, don't overwrite the local model. Show "Changed by {actor} at {at} · **Reload** · Keep editing", with `actor` and `at` from the event.
- Saving afterwards meets the server's version check (409 → `ConcurrencyError`).
- The open-lightbox case is the existing "case changed" state (A3). It is now triggered by `ServiceChange` for `['Case', caseId]` with a `version` above the previewed `caseVersion`.
- `LiveChangeBanner` works out what changed by comparing the refetched anchor with the previous one (for example "1 new Activation"). The event only says that the case changed, not what.

**Server prerequisites:** these are the only backend tasks.
- The case-risk worker, the hold worker, the dispatcher's result handler and the Undo path must run inside `OperationScope` **and name the entity**. Otherwise they won't publish, and screens will only update on focus or reconnect.
- Child writes (notes, events, uploads, tags) must name the parent case (as `EntityId`, or a parent field if one is added) so the case view is invalidated.
- Add the `Origin` check (§7.4) to `ServiceChangeHub` negotiate and upgrade.

**Tests:**
- Vitest: a fake hub emits events, and the test asserts the invalidated keys, the dedupe by version and `correlationId`, the batching, and the reconnect full invalidation.
- Playwright: two browser contexts on the same tenant. An edit in A appears in B within 2 s. A foreign tenant sees nothing.

### 2.5 Generated narratives: framed context, not chat context (D-V26)

**Rule:** every narrative the backend produces goes through `POST /api/AskJooby/Narrate`. That covers the case summary, Compose section drafts, the agent triage summary, the close rationale suggestion, report narratives, and "explain this activation or journey" text. Each call is **stateless**. The server assembles the context from a registered **frame** and the subject's facts. **No chat history, no panel conversation and no client-supplied prompt text** is ever included.

**Frame catalogue:** frames live on the server, are versioned, and are tested like code. Initial set:

| Frame | Subject | Context the server assembles | Output |
|---|---|---|---|
| `CaseSummary` | caseId | CH-00 anchor, ranked activations, key facts, risk assessment | 3–5 sentences for SummarySection |
| `NarrativeSection` | caseId + section (Subject, Activity, ActionsTaken) | FactsTray facts for that section and the report type's field rules | a section draft for NarrativeEditor |
| `SuspicionFramings` | caseId | typology candidates + facts | 2–3 **options** for FramingList (Grounds for suspicion remains human-written) |
| `CloseRationale` | caseId + outcome + typology | facts, checklist state, rationale templates | a suggested rationale for CloseForm |
| `AgentTriageSummary` | agentRunId | the run's tool results only | recommendation text for AgentRunCard |
| `ActivationExplain` | activationId | exact contributions, rule path, sensitivity | plain-language explanation for ActivationCard |
| `JourneyExplain` | archiveId + adaptation | the adaptation's `Journey`, `Contribution` and `Calibration` | a path explanation for ModelFactsDrawer |

**Request:** `{ frame: string, subject: {…}, params?: {…} }`. The server rejects unknown frames and unknown params.

**Response:** `{ runId, frame, frameVersion, contextHash, model, sentences: [{ id, text, kind: 'Observed'|'Inference', factIds: string[] }], generatedAt }`.
- Every sentence cites fact ids.
- A sentence with no fact is always `Inference`, and the editor gutter shows it amber until a person accepts it.

**Provenance and audit:**
- Each call writes an `AskJoobyRun` (frame, version, context hash, model, output hash) and a `CaseEvent` `NarrativeGenerated`.
- The same frame version and context hash can be re-run to reproduce or compare a draft.

**Chat stays separate:** AskJoobyPanel is conversational and read-only. Nothing said in chat reaches a narrative. To use a chat result, the analyst **accepts a Finding card as a fact** (`add-fact`), and from then on it is part of the framed context like any other fact.

**Not affected:** the HTTP Adaptation `Narrative` and `HumanLabel` wire fields come from the endpoint's curated template dictionary, not a generative model. Show them verbatim, labelled "from the model".

**useNarrative(frame, subject, params?)** is a `useQuery` on `['Narrative', frame, subjectKey, contextHash?]`.
- `generate()` / `regenerate()` → `POST /api/AskJooby/Narrate`. It is never called automatically on page load, except `CaseSummary` and `ActivationExplain` where the tenant enables auto-draft.
- `stale: ComputedRef<boolean>`: true when a `ServiceChange` for the subject arrives after `generatedAt`. The client **never regenerates automatically over text a person has edited**. It shows "Facts changed since this was drafted · Regenerate · Keep".
- `provenance`: frame, version, model and fact count, for the "Drafted by Jooby" line.
- Timeout (20 s) or error: the "Failed" state with Retry. The editor stays usable and nothing is lost.

**UI rule:** every generated text carries one line: "Drafted by Jooby · {frame} v{n} · from {k} facts · Regenerate". Its provenance popover lists the facts used.

**Tenant controls:**
- AskJooby narration on or off, which frames auto-draft, and the model (the deployment setting).
- When narration is off, every narrative field is simply a manual text box.

### 2.3 Pinia stores (`src/stores/*.ts`)

| Store | State | Actions | Read | Write |
|---|---|---|---|---|
| `useSessionStore` | `user: { id; name; roles: string[]; permissions: string[]; releaseLimit: Money\|null }`, `tenant: { id; name; culture; timezone }` | `loadMe()` → `GET /api/Me` (**new**, CG-23) · `can(permission): boolean` (getter) | all components | `useAuthSession`, `useLoginJourney` only |
| `useUiStore` | `theme`, `density`, `toasts: ToastVm[]`, `paletteOpen`, `inboxOpen` | `pushToast`, `pushUndoToast(operationId, seconds)`, `dismissToast`, `openPalette`, `openInbox` | all | shell components, `useOperation` (toasts only) |
| `useInboxStore` | `open`, `tab` (items come from vue-query `['Inbox','list']`, invalidated by `ServiceChange` area `Inbox`) | `markDone(id)` → `POST /api/Inbox/{id}/Done`, then invalidate | TopNav, InboxDrawer | InboxDrawer |
| `useCasesStore` | `activeFilterId`, `sessionGuid: Guid\|null`, `selection: CaseId[]`, `riskMatrixOpen` (client state only; filters, rows, counts and the matrix live in vue-query under `['CaseFilter','list']`, `qk.caseList`, `['CaseFilterCount']`, `['RiskMatrix']`) | `select`, `setFilter`, `saveFromSession(name)` (CP-19, then invalidate `['CaseFilter','list']`) | CasesPage tree | CasesPage tree |
| `useCaseStore` | keyed by `caseId`: `selection: { archiveId?: number; nodeId?: string }`, `lastSeenVersion` (for the LiveChangeBanner comparison) (the anchor and sections live in vue-query under `qk.case(id)`) | `select`, `markSeen(version)` | CasePage tree | CasePage |
| `useConfigStore` | none (schemas are vue-query `[entity, 'Schema']`, with `staleTime: Infinity` until a deploy) | — | — | — |

**Server data never goes in Pinia.** It lives only in the vue-query cache (§2.4). **Forbidden in any store or the query cache:** JWTs (held only in `useTokenStore`), step-up tokens, raw payloads (`Archive.Json`), revealed masked fields. These live only in component-local refs and are dropped on unmount.

---

## 3. Edge cases, states and accessibility

### 3.1 UI states (one pattern each, from D3)

| State | Rule | Skeleton or indicator |
|---|---|---|
| Loading | Shown after 150 ms (no flash). After 3 s the label becomes "Still loading {section}…". | `skeleton` blocks at the final size: grid is 10 rows × 28/36 px by density; case header 2 lines; ActivationCard 1 strip + 180 px chart; JubeChart at its `height`. |
| Empty | Say what to do next, with one action. | Lucide `inbox` + sentence + link. Cases filter: "No cases in this filter." Journal: "No transactions in this window. Widen to 90 days." Prior cases: "No earlier cases for this case key." |
| Failed | Say how to fix it + Retry + `ref` (the correlation id from ProblemDetails `traceId`). | `alert alert-error` inside the section only; the page never goes blank. |
| Stale | Last good values + "as at hh:mm" + Refresh. | `alert alert-warning` compact line. |
| Restricted | "Restricted link": no id, name or status. | `badge badge-ghost` + tooltip. Never show a 403 page for a nested item. |
| Offline | ConnectionBanner; operations disabled; drafts kept. | — |
| Case changed | LiveChangeBanner; changed section outlined; open lightbox shows the refreshed preview and "I've seen the change". | — |
| Graph over cap | "Over 150 nodes: hide hubs or use 1 hop" with both buttons. | — |
| Model not explainable | "No explanation available for this model" (never a fabricated chart). | — |
| Payment outcome unknown | "Outcome unknown", with "Retry safely (same key)". Never "Failed". | — |

### 3.2 Validation rules

- **Typed reasons** (BreakLock, Reject, RefusePayment, ReleasePayment, ChangePriority, StopClock, emergency config change): trimmed length ≥ 10. The error shows on blur, then live.
- **Close:** outcome required; typology required when outcome ∈ {TP, Suspicious}; rationale ≥ 20 characters or a template; submit disabled while `preview.unmet.length > 0`. "Do it now" resolves one unmet item through its own operation.
- **File:** approval state `Approved` is required. Scorecard red items block; amber needs an acknowledgement each.
- **Payment decision:** `decision ∈ {1,2,3}`; reason ≥ 10; evidence note selected or "No customer contact" chosen explicitly. If `decision = Release` and `amount > user.releaseLimit`, the verb becomes "Request release" and the operation becomes `RequestRelease`. Never bulk (G10).
- **Extend hold:** the new expiry must not pass the ceiling from the resolved `PaymentHoldPolicy` row (`MaxHold` in `HoldUnit` from `HoldClockStart`); a next step is required. If the row is EscalateOnly with no ceiling, only the reason and next step are required.
- **Step-up code:** exactly 6 digits (TOTP). Three failures end in a 60 s lockout, shown as a countdown.
- **DTO forms:** client rules mirror the attributes, and the server is authoritative. Save is disabled while not `dirty` or while any visible required field is empty.
- **Mentions:** `@` opens a user picker limited to users with access to the case. An unknown mention is plain text, never an Inbox item.
- **Double-submit:** every primary button is disabled while `phase ∈ {previewing, executing}`; the idempotency key protects anything that slips through.

### 3.3 Responsive breakpoints

| Width | Layout |
|---|---|
| ≥ 1920 | Case page with three columns (key panel 280, main fluid, right 380); Journal and Network side by side are allowed. |
| 1280–1919 | Three columns (key panel 240, right 340). This is the design baseline, 1366. |
| 1024–1279 (read-and-act) | Two columns: key panel collapses into a header popover; right column becomes a bottom sheet. Journal, Network, Compose and configuration show "Open on desktop" with a copy-link button. |
| < 1024 (read-and-act, phone) | One column: Summary, HeldPaymentCard, Activations (read-only), Inbox, approvals. Lightboxes become full-screen `modal-bottom`, and targets are ≥ 44 px. The PaymentDecisionForm and ApproverView work fully. |
| Team wall | Fixed 1920×1080 (kiosk), scales with CSS `zoom` to fit; no interaction. |

### 3.4 Accessibility

- **Landmarks:** `header` (TopNav), `nav` (SectionNav), `main` (CaseMain), `aside` (CaseKeyPanel, RightColumn), each with `aria-label`.
- **Grid:** Tabulator gets `role="grid"`, `aria-rowcount`, `aria-selected`. Row Enter = `row-activate`. Arrow keys move the row. In-cell buttons are reachable with Tab and have `aria-label="Tag ARC-771203"`.
- **Lightbox:** `role="dialog" aria-modal="true" aria-labelledby` = title id. Focus moves to the first invalid input, or the first control. Focus is trapped and returns to the invoker on close. Esc closes (in the result phase, only after the result is shown). The StepUpPanel takes focus when it appears, and "Back to the form" returns focus to Submit.
- **Drawers:** `role="dialog"` (non-modal Peek: `aria-modal="false"`), with Esc to close and focus returned to the row.
- **Live regions:**
  - Toasts: `aria-live="polite"`.
  - ConnectionBanner and "Outcome unknown": `role="alert"`.
  - Hold countdowns announce only at 60, 15 and 5 minutes (G11), never every second.
  - The step-up expiry warns 60 s before, with "Keep verified".
- **Colour never carries meaning alone:** risk chips carry band text; Merge lanes carry letters; diary states carry words; tag pills carry text.
- **Contrast:** 4.5:1 for text in both themes and both densities (compact 12 px minimum). Focus ring `outline-2 outline-offset-2` in the `primary` token.
- **Charts:** `JubeChart` wraps the SVG in `role="img"` with `aria-label={title}` and a visually hidden `<table>` fallback when the endpoint provides `?format=table`.
- **Keyboard map (single source):** Ctrl/Cmd+K palette · `g i` inbox · `g t` team · `?` sheet · Space peek · W skim · x select · H decide payment · 1–6 sections · R risk · D close · F file · c compose · Alt+J AskJooby · Alt+← back · Ctrl+Enter submit · Esc close. Unavailable keys open a popover explaining why.

---

## 4. Migration path and phases

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

## 5. Vendored libraries: keep, replace or drop

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

## 6. MCP setup for the generating agents

Two MCP servers are part of this skill. Every generation prompt ends with "use Blueprint MCP", and every Vue API question goes through Vue Docs MCP.

**daisyUI Blueprint** (official, commercial licence, runs locally over stdio):
```
claude mcp add daisyui-blueprint --env LICENSE=<licence> --env EMAIL=<email> -- npx -y daisyui-blueprint@latest
```
- **Licence:** Jube will buy it when build starts, before phase V0; one seat per developer using Claude Code on the UI.
- Use it for **exact daisyUI 5 component syntax** and its final inspection pass on generated markup.
- Do **not** use its creative-direction or theme-generation tools. The Jube theme, density and colour rules in this skill take precedence (colour means risk and state only; the neutral palette is only for Merge lanes).

**Vue Docs MCP** (hosted, free, streamable HTTP):
```
claude mcp add --scope user vue-docs --transport streamable-http https://mcp.vue-mcp.org/mcp
```
- Use it for Composition API, `<script setup>`, Pinia, Vue Router and vue-i18n questions.
- Optional during development only: `vite-plugin-vue-mcp` exposes the running app's component tree, state, routes and Pinia data to the agent. Never enable it in production builds.

**Generation contract** (how a junior agent uses this skill):
1. Pick exactly one component from §1.
2. Generate `<script setup lang="ts">` with the listed props and emits (no others), and the composables named for it in §2.
3. Build the template from daisyUI classes confirmed through Blueprint MCP.
4. Handle every state in §3.1 that applies, and the §3.4 attributes.
5. Write a Vitest unit test for each emit's trigger condition, and a Playwright step if the component appears in a journey.
6. If anything is ambiguous, stop and ask. Do not invent.

---

## 7. Security posture: bearer tokens and the SPA

**Session token handling (D-V27, supersedes D-V17 and the cookie posture).** The JWT is no longer carried in a cookie. Login returns it in the response body with its `expiresAt`, and the Vue app keeps it in a secure, tab-scoped store (`useTokenStore`). The store holds it in memory and mirrors it to `sessionStorage` so it survives a reload of the same tab. It is never written to `localStorage`, never shared across tabs, and is cleared on sign-out or when the tab closes. Every API request and SignalR connection sends it as `Authorization: Bearer`. Cookies are removed from the authentication pipeline altogether: `HybridAuthorisationHandler` drops the cookie scheme, `authentication-jwt` and `authentication-expiry` are no longer issued, and `TokenRefreshMiddleware` returns the reissued token in an `X-Refreshed-Token` response header (with `X-Token-Expires`) instead of re-setting a cookie. Because the browser no longer attaches credentials automatically, CSRF does not apply to `/api/*` and the antiforgery double-submit (CG-16) is withdrawn. The trade-off is that a script running in the page could read the token, so the CSP and the `v-html` ban become the primary defence and are non-negotiable.

> **Note:** cookies are removed from the pipeline in favour of `Authorization` headers. The JWT lives in the tab-scoped secure store only. Because the UI is a single Vue SPA (D-V28), there is no Razor page left needing the cookie; the cookie scheme is deleted in the first SPA release. A new tab (for example, opening a case with a middle-click) has no token and signs in again.

**Forbidden:** the generating agent must refuse these, and code review must reject them.
- Storing the JWT in `localStorage`, IndexedDB, a Pinia store, the query cache or any cookie.
- Sharing the token across tabs (`BroadcastChannel`, `storage` events, `window.opener`).
- Decoding the JWT on the client. Expiry comes from `expiresAt` / `X-Token-Expires`.
- CORS with credentials and wildcard or reflected origins.
- Any `v-html`, `eval` or inline script: with a readable token, XSS is the main threat.

**Required:**
1. **Same origin, always.** In development, Vite serves the app with `server.proxy` for `/api`, `/caseHub`, `/watcherHub` and `/Account` (`ws: true` for the hubs), pointing at the local `Jube.App`. No CORS configuration is added for the SPA.
2. **Pipeline:** remove the cookie scheme from `HybridAuthorisationHandler` (API key, then Bearer). Stop issuing `authentication-jwt` and `authentication-expiry` in `AuthenticationCookieIssuer`. `TokenRefreshMiddleware` writes `X-Refreshed-Token` and `X-Token-Expires` and exposes them via `Access-Control-Expose-Headers` only if a cross-origin client is ever added. OIDC correlation and nonce cookies remain, because they belong to the login handshake rather than the session; the OIDC return leg hands the JWT to the app in the response body, never in the URL.
3. **Hubs** (`ServiceChangeHub`, `WatcherHub`, `CaseHub`): browsers cannot set headers on WebSockets, so SignalR sends the token as `access_token` in the query string. The server reads it from the query **only** on hub paths, and request logging strips it. Validate `Origin` on negotiate and upgrade. `CaseHub.Join` re-checks case permission server-side.
4. **GET is safe.** No GET endpoint may change state. The exception is `CaseViewed` audit writes, which are side-effect-only and idempotent per request.
5. **CSP:** `default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline'` (Tailwind is compiled in CI with no browser runtime; `'unsafe-inline'` for styles covers Tabulator's inline widths; move to hashes once audited); `connect-src 'self'`; `frame-ancestors 'none'`; **no `unsafe-eval`** (D-V1). Add Trusted Types (`require-trusted-types-for 'script'`). Public portals use the same CSP plus the tenant logo host.
6. **No HTML from data.** `v-html` is banned by lint. Case Workflow Displays arrive as typed props (A.15). Narratives render as text with fact chips.
7. **Step-up tokens:** single use, bound to `previewHash` for payment decisions (G8), sent as a header, never stored.
8. **Tests:** add PenTest-category tests asserting that (a) every `/api` route and hub rejects a request carrying only a cookie, (b) the token never appears in `localStorage`, the URL or the logs, (c) a second tab starts without a token, (d) `X-Refreshed-Token` rotates the token in the store, and (e) the hub rejects a foreign Origin.

---

## 8. Competitive gaps: what to build (M-series, pass 5)

Researched against current market leaders in September 2026: Unit21, Hummingbird, Lucinity, Flagright and NICE Actimize. The design already matches or leads on explainability (exact contributions), maker-checker, typed diaries, held payments, graph and Journal. The gaps below are where competitors ship something Jube's design doesn't yet have. Screens are M1–M6 in `Jube Case Management v2 - pass 4 screens.dc.html`.

| # | Gap | Who ships it | Jube design | New calls |
|---|---|---|---|---|
| **M1** | **Agent L1 triage with a visible work log.** An agent pre-investigates the alert and recommends a disposition. The analyst approves or overrides it, and every step it took is logged. | Unit21 (AI agents doing L1 triage, evidence and narratives with a work log), Lucinity (Luci copilot) | **AgentRunCard** at the top of Summary. It shows the recommendation (Close as FP / Escalate) with a confidence band and the evidence it cites, then the work log: each tool call with its inputs and results, all read-only calls through `PageAgentToolBinder` with `AskJoobyReadOnly`. Buttons: **Accept → opens the normal Close lightbox pre-filled** (the analyst still submits) · Override (reason required) · Show log. The agent never executes an operation. Its runs are CaseEvents, and agent-vs-analyst agreement feeds QA (M2). | CG-01 `GET /api/Case/{id}/AgentRun` · CG-02 `POST /api/Case/{id}/AgentRun` (re-run) · CG-03 Op: `OverrideAgent` · background: triage worker on new cases (tenant opt-in per filter) |
| **M2** | **QA sampling and review.** Completed cases are sampled on a schedule and reviewed against a checklist, with feedback to the analyst. | Hummingbird (random sampling, scheduled QA/QC) | **QaSamplingCard** on the Team view shows plan, rate, next run and queue size. **QaReviewPage** is a queue of sampled closed cases. **QaReviewForm** is a checklist of pass, fail and N/A items with comments; the outcome is Pass, Minor or Major. A Major finding offers **Reopen** (maker-checker). Results roll up per analyst on the Team view. Agent disagreement (M1) raises the sampling weight. | CG-04 `GET/PUT /api/QaPlan` · CG-05 `GET /api/QaQueue` · CG-06 Op: `RecordQaReview` · CG-07 Op: `Reopen` · background: sampler job |
| **M3** | **RFI to the customer inside the case.** Send a request for information and get the answer back into the case, with a diary. | Hummingbird (RFIs sent and received within the case) | **RfiForm** lightbox: template, questions, document requests and a due date. The **tipping-off check** is shown and blocks for SAR-linked cases unless the MLRO overrides it. Delivery by email with a single-use link to **RfiPortalPage**, which is tenant-branded (like the referral form) and has no login: the customer answers and uploads documents (virus-scanned). An **RfiThread** in Notes shows sent, viewed, answered or overdue; the answers become Case Uploads plus a note. An RFI diary starts. | CG-08 Op: `SendRfi` · CG-09 `GET /api/Rfi/{token}` (public) · CG-10 `POST /api/Rfi/{token}/Answer` (public, rate-limited, single-use token, no session) · CG-11 Op: `CancelRfi` |
| **M4** | **Web research and adverse media with citations.** | Unit21 (Agentic Task Builder: online search returning a structured answer with citations) | **WebResearchCard** in AskJoobyPanel ("Researcher" persona, which already exists as the only RoutingRule). It shows the query, a structured answer, and **citations with the URL, retrieval time and quoted snippet**. "Save to case" stores the citations as a Case Upload (a PDF snapshot of the sources) and can add a narrative fact. Opinion vs Finding rules apply: a finding needs at least 2 independent sources. Off by default per tenant (data egress). | CG-12 `POST /api/AskJooby/Research` · CG-13 Op: `SaveResearch` |
| **M5** | **Automation recipes with a visual builder and run log.** | Hummingbird (visual automation builder, recipe library, activity logs) | **AutomationRecipesPage**: a library of recipe cards (Auto-assign by filter, Deadline digest, Periodic KYC review, QA sampling, Pre-populate case). **RecipeBuilder**: trigger (event or schedule) → conditions (a FilterJson predicate, the same compiler as D-V9) → steps (only registered operations, the same catalogue as macros). **Test** is a dry run over the last 30 days showing what would have happened. **Publish** goes through maker-checker. **RecipeRunLog** shows each run, its target, the result and a link to the CaseEvents. This unifies macros, hooks and scheduled tasks into one audited mechanism. | CG-17 `GET/POST/PUT /api/AutomationRecipe` · CG-18 `POST /api/AutomationRecipe/{id}/DryRun` · CG-19 `GET /api/AutomationRecipe/{id}/Runs` · Op: `PublishRecipe` |
| **M6** | **Rule tuning from outcomes: shadow test before deploy.** | Unit21 (backtesting and shadow-mode rule simulation) | **ShadowTestPanel** is reachable from the rule dependency path in Activations ("Tune this rule…") and from the rule configuration page. It shows the rule's hit rate, TP rate and closed-case outcomes for 90 days, a proposed threshold change, and a **backtest** (reusing the backtest service on `llm-tool-offload`) with before and after hits, TPs caught and missed, and analyst hours. **Shadow mode** runs the new version alongside the live one for N days without creating cases, then promotes it through maker-checker. | CG-20 `GET /api/ActivationRule/{id}/Performance` · CG-21 `POST /api/ActivationRule/{id}/Backtest` · CG-22 Op: `StartShadow` / `PromoteShadow` |

**Deliberately not copied:**
- A cross-institution **fraud consortium**, as Unit21 offers. It needs a network operator, which conflicts with self-hosted sovereignty. Revisit only as an opt-in federated feature.
- **Autonomous closing** by agents. In Jube, agents recommend and humans execute. This is the defensible position with regulators and is stated in the product.

**Jube's differentiators to keep visible in the design and sales material:**
- open source (AGPL) and self-hosted;
- models are swappable through `DynamicEnvironment`, and none are hardcoded;
- **exact explanations**, with an honest "none available" instead of approximations;
- every AI step is a CaseEvent;
- no per-alert pricing.

---

## 9. New endpoints introduced by this blueprint (CG-series)

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

## 10. Figure → component map

Figures are in `design/img/` and the design specification document. The HTML prototypes are **design references only**: they load daisyUI, Tailwind and Tabulator from CDNs and use a different component runtime. Recreate them in Vue from this blueprint; never copy their markup or scripts.

| Figures | Components |
|---|---|
| 01, 02, 03 | CasesPage, FilterHub, HubCard, CasesToolbar, RiskMatrix, DataGrid, RiskChip, LockCell, BulkBar, PeekDrawer |
| 04, 05, 32 | CasePage, CaseHeader, RiskChip, LaneBadge, DiaryMeter, PresenceAvatars, CaseKeyPanel, SummarySection, CaseRiskPopover |
| 06, 06b, 07 | ActivationsSection, ActivationCard, ScoreStrip, JubeChart, SensitivityLine, RulePathView, SanctionsMatchCard |
| 08, 08b, 09, 29, 30, 35, 36 | JournalSection, LensBar, PivotBanner, AggregationStrip, DataGrid, ExplodeGrid, TagMenu |
| 10, 11 | NetworkSection, GraphCanvas, NodePanel, ExplodeGrid |
| 12 | NotesSection, ActionComposer, NoteList |
| 13, 13b | CaseJournalSection, HashChainBadge, DataGrid |
| 14, 15, 16, 31 | TransactionCardDrawer, PayloadDrawer, ModelFactsDrawer, TagMenu |
| 17–21 | OperationLightbox, ConsequenceList, CloseForm, FileForm, ApproverView, BreakLockForm |
| 22–28, 48 | HeldPaymentCard, PaymentDecisionForm, StepUpPanel, ExecutionResult, ApproverView |
| 33, 34 | DiaryMeter (all states), LiveChangeBanner, ConnectionBanner |
| 37, 38 | ComposeMode, FactsTray, NarrativeEditor, QualityScorecard, FramingList, AskJoobyPanel, ProposalCard |
| 39, 40, 41, 42 | CommandPalette, InboxDrawer, the §3.1 state patterns, ConfigHubPage, HubCard |
| 43, 44 | TeamViewPage (desk and wall), AnalystTable, FilterLoadTable, RiskMatrix, PendingChangesCard |
| 45, 46, 47 | ReferralPage, ReportStatusPage, the escalation email template (server-rendered) |
| 49 | UserSettingsPage, TotpEnrolStep, RecoveryCodesStep |
| 56–59 (S1–S4) | SuppressForm, SuppressionBadge, AdaptationUnavailableLine, SuppressionsPage, SuppressionGrid, SuppressionDetail, ListChangeSetEditor, ProposeListChangeForm |
| 50–55 (M1–M6) | AgentRunCard, QaSamplingCard, QaReviewPage, QaReviewForm, RfiForm, RfiPortalPage, RfiThread, WebResearchCard, AutomationRecipesPage, RecipeBuilder, RecipeRunLog, ShadowTestPanel |

*End of skill.*
