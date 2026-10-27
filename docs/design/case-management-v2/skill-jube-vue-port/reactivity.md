# Reactivity, state and I/O

Blueprint §2 verbatim: composables, state machines, Pinia, TanStack Query + ServiceChangeHub invalidation, framed narratives. Plus §7 security posture.


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

## Security posture: bearer tokens and the SPA

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