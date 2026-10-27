# Component catalogue

Blueprint §1 verbatim. If a component, prop, emit or state is not here, stop and ask. Then §10: which design figure each component comes from.


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

## Figure → component map

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