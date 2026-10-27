# Decisions D-V1 to D-V29

Binding. Verbatim from the Vue blueprint §0. Cite the id beside any rule you derive from it.


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