# Handoff: Jube v2, the AML and fraud transaction monitoring platform

**Build handoff, 4 October 2026.** Jube Operations Limited.

## Resource pack

- **For the website:** `Jube v2 - What is being built.pdf` (save it from the printable page if it is not already in this folder).
- **To read or edit:** the Word copy, `Jube v2 - What is being built.docx` (opens in Word and Google Docs).
- **To build from:** the Markdown, the Vue blueprint, the specification and the code audit (see Reading order).
- **To see it working:** the clickable prototype and the screen boards.
- **Brand:** `brand/` holds the Jube Brand and Style Guide (Word), the wordmark and the buffalo.

Read "Read this first" at the start of the document before anything else: most of this already exists.

## Overview

This package rebuilds everything Jube's fraud and financial crime users do after a transaction is scored: finding and prioritising cases, understanding why a transaction alerted, the Journal and Network, held payments with step-up, inbound holds, recall, payee name checks, APP reimbursement claims, reporting, assurance (QA, read audit), automation, rule tuning, suppressions and versioned lists. It moves from Razor Pages + jQuery + Kendo to **Vue 3 + daisyUI 5 + Tabulator**. Charts stay server-rendered by `Jube.Charting`.

Every screen maps each click, key and search to an endpoint, labelled Exists, Extend or New.

## About the design files

The `.dc.html` files are **design references built in HTML**. They are prototypes showing the intended look and behaviour; they are not production code. They load daisyUI, Tailwind and Tabulator from CDNs and use a prototyping runtime (`support.js`).

**The task:** recreate them in Vue 3 inside `Jube.App`, following `skill-vue-blueprint/SKILL.md` exactly. Never copy prototype markup or scripts into the product.

## Fidelity

**High fidelity for layout, content, states and interactions.**
- Components, copy, ordering, states, keyboard behaviour and endpoint wiring are final.
- Visual styling is daisyUI 5's default light and dark themes with the tokens below, so recreate it with daisyUI classes rather than pixel-matching screenshots.
- Sample data (names, amounts, ids, dates) is illustrative.

## Reading order

1. **`design/Jube v2 - What is being built.md`** is the source of truth for Claude Code (printable copy: `Jube v2 - What is being built.dc.html`). It opens with what is being built and the build order, then the journeys, then every screen in the order a user meets it, each interaction with its call and status, the endpoints and the build handoff. It contains no decision history; that is in `design/Decision log.md`. A Word copy, `Jube v2 - What is being built.docx`, is generated from the same Markdown.
2. **`skill-vue-blueprint/SKILL.md`** is the Vue 3 blueprint, and it is binding:
   - §0 adopted decisions;
   - §1 component tree with every prop and event;
   - §2 composables, state machines and Pinia stores;
   - §3 states, validation, breakpoints and accessibility;
   - §4 migration phases V0–V15, with the current files each one retires;
   - §5 libraries to keep, replace or drop;
   - §6 MCP setup and the generation contract;
   - §7 security posture;
   - §8–9 market gaps and the CG endpoints;
   - §10 figure-to-component map.
3. **`specs/ui-specification-golden-audit.md`** is the code audit: corrections, the consolidated endpoint list (Appendix A) and the backend build order (Appendix B).
4. **`specs/jube_case_management_v2_spec_0.11.md`** is the product and technical specification (decisions D1–D27, interaction ids in section 22).

## Set up Claude Code

1. Copy `skill-vue-blueprint/` into the repository as `.claude/skills/jube-vue-blueprint/`.
2. Add the MCP servers (SKILL §6):
   ```
   claude mcp add daisyui-blueprint --env LICENSE=<licence> --env EMAIL=<email> -- npx -y daisyui-blueprint@latest
   claude mcp add --scope user vue-docs --transport streamable-http https://mcp.vue-mcp.org/mcp
   ```
3. Put this package's `specs/` and `design/` folders in the repository (for example `docs/design/case-management-v2/`), so the agent can read them.
4. First prompt:
   > Use the jube-vue-blueprint skill. Start phase V0 (toolchain) and V1 (shell) from SKILL §4. Follow §7 security rules without exception. Generate one component at a time per the §6 generation contract; stop and ask on anything not in the blueprint. use Blueprint MCP

## Screens and views

| Area | Screens (figures) | Purpose | Layout |
|---|---|---|---|
| Cases page | 01 list, 02 risk matrix, 03 peek | Triage in lane order (Live money → Mandatory → Deadline near → Standard), then Priority | Filter hub cards left (240 px); Tabulator list fluid; peek drawer right (420 px, non-modal) |
| Case page | 04, 05, 32–34 | Investigate one case | Three columns at ≥ 1280 px: case key panel (240/280 px) · main sections 1–6 (fluid) · Decide or AskJooby (340/380 px); sticky header with risk chip, diaries, presence and lock |
| Activations | 06, 06b, 07 | Why each rule or model fired, with exact contributions | Card per activation: score strip, waterfall (Jube.Charting SVG), sensitivity, health, rule path, sanctions |
| Journal | 08, 08b, 09, 29–31, 35, 36 | Transactions for the focus key; pivot; lenses (Subject, Merge, Pin, Explode); tags | Aggregation strip, daily chart, sparkline, grid (36 px rows, or 28 px compact) |
| Network | 10, 11 | 1-hop graph with a shared selection; Explode as its list view | Graph canvas (≤ 150 nodes) with a node panel |
| Notes, case journal | 12, 13, 13b | Coded notes with results; typed events with hash chain; risk history | Composer on top, list below |
| Drawers | 14–16 | Transaction card, raw payload, model facts | Right drawers, 520 px; the payload drawer stacks on the card |
| Lightboxes | 17–21, 48 | Close, File (maker-checker), Approver, Break lock, Bulk close, Approve release | `modal-box` max-w-3xl: form, "What will happen" list (from the server Preview), step-up panel, result |
| Held payments, step-up | 22–28, 49 | Decide a held payment; re-authenticate in place; honest result ("Outcome unknown"); enrolment | Hold card in peek and Summary; decision lightbox keys 1–3 |
| Global | 39–42 | Command palette, Inbox, state catalogue, configuration hub cards | Palette centred (640 px); Inbox drawer right (440 px) |
| Team view | 43, 44 | Supervisor desk; wall screen | Desk: analyst table plus a risk matrix column (380 px). Wall: fixed 1920×1080 in the dark theme |
| Outside the case | 45–47 | Referral form and receipt; report status; hold-expiring email | Tenant-branded single page; lifecycle steps |
| Suppression and lists | 56–59 (S1–S4) | Suppression is the only override (time-bound, with a reason, stamped on each affected match, created from a case or on its own); lists and dictionaries are versioned model artefacts | See the doc's §22 and SKILL D-V21 to D-V25 |
| Market gaps | 50–55 (M1–M6) | Agent triage + work log; QA sampling; RFI; web research with citations; automation recipes; rule tuning with backtest and shadow mode | See the doc's §20 and SKILL §8 |

Full per-screen interaction tables are in the design specification, sections 2–20.

## Interactions and behaviour (summary)

- **Operations:** every operation opens a lightbox that calls `POST /api/CaseOperation/{op}/Preview`, then submits with `/Execute` using an idempotency key and the case version.
  - It re-previews only when a consequence input changes.
  - Reversible operations show a 10 s undo toast, and hooks wait for it to expire.
  - Irreversible ones (File, Approve, Break lock, Release, Refuse) ask for step-up inside the lightbox.
- **Live updates are a cache-invalidation signal, not a data feed.**
  - The existing `ServiceChangeHub` (`/serviceChangeHub`) already receives an event for every service-layer write (`OperationScope` → `IServiceChangeBus` → `ServiceChangeRelay`, tenant group auto-joined). No browser has listened until now.
  - One app-wide connection marks the matching TanStack Query keys `[Area, entityId]` (and `[Area, 'list']`) stale, and they refetch in the background.
  - Full background refresh on reconnect and after a long-hidden tab; a 30 s safety poll on money-critical views only.
  - Never refetches into a dirty form; shows "Changed by X · Reload" instead.
  - If the case changes under an open lightbox, submit is disabled until the user acknowledges the change.
  - `CaseHub` is new and carries presence only. See SKILL §2.4.
- **Keyboard:** Ctrl/Cmd+K palette · Space peek · W skim · H decide payment · 1–6 sections · R risk · D close · F file · c compose · Alt+J AskJooby · Alt+← back · Ctrl+Enter submit · Esc close.
- **States:** loading after 150 ms as a skeleton at the final size; empty with a next action; failed with Retry and a trace ref; stale with "as at"; restricted link; offline. Details in SKILL §3.
- **Validation and responsive tiers:** SKILL §3.2–3.3. Below 1280 px only the read-and-act screens work (Summary, Activations read-only, held payment, Inbox, approvals).

- **Generated narratives:** each one is a stateless AskJooby call (`POST /api/AskJooby/Narrate`) with a server-built, versioned frame. Chat context is never used, every sentence cites facts, drafts are marked stale when the facts change, and nothing is regenerated over edited text. See SKILL §2.5 and D-V26.

## State management

- **Server data:** TanStack Query for Vue, keyed by service-change Area and entity id, and invalidated by `ServiceChangeHub` (SKILL §2.4, D-V20).
- **Pinia:** holds client state only (session, ui, selections).
- **Composables:** `useApi`, `useOperation`, `useStepUp`, `useServiceChanges`, `usePresence`, `useUrlState`, `useDtoForm` and others are specified field by field in SKILL §2. The URL holds section, focus key, selection, lens, filter and sort.

## Session tokens

**Session token handling (D-V27, supersedes D-V17 and the cookie posture).** The JWT is no longer carried in a cookie. Login returns it in the response body with its `expiresAt`, and the Vue app keeps it in a secure, tab-scoped store (`useTokenStore`). The store holds it in memory and mirrors it to `sessionStorage` so it survives a reload of the same tab. It is never written to `localStorage`, never shared across tabs, and is cleared on sign-out or when the tab closes. Every API request and SignalR connection sends it as `Authorization: Bearer`. Cookies are removed from the authentication pipeline altogether: `HybridAuthorisationHandler` drops the cookie scheme, `authentication-jwt` and `authentication-expiry` are no longer issued, and `TokenRefreshMiddleware` returns the reissued token in an `X-Refreshed-Token` response header (with `X-Token-Expires`) instead of re-setting a cookie. Because the browser no longer attaches credentials automatically, CSRF does not apply to `/api/*` and the antiforgery double-submit (CG-16) is withdrawn. The trade-off is that a script running in the page could read the token, so the CSP and the `v-html` ban become the primary defence and are non-negotiable.

> **Note:** cookies are removed from the pipeline in favour of `Authorization` headers. The JWT lives in the tab-scoped secure store only. Because the UI is a single Vue SPA (D-V28), there is no Razor page left needing the cookie; the cookie scheme is deleted in the first SPA release. A new tab (for example, opening a case with a middle-click) has no token and signs in again.

Full rules: SKILL §7.

**Application shape (D-V28).** Jube v2 is a single Vue SPA rather than Razor routes. `Jube.App` serves the built `index.html` as the fallback for every non-API, non-hub path, and Vue Router owns navigation. The Razor/Kendo UI stays in production untouched until the SPA replaces it in one release; there is no route-by-route mixing.

**Localisation (D-V29).** Localisation is done in Vue through locale files (`ClientApp/src/locales/{locale}.json`, English as source) loaded by vue-i18n, one lazy chunk per locale. `.resx` is retired for UI strings and `GET /api/Resources/{culture}` (CG-15) is withdrawn. Tenant-configured labels keep their server-side translation table (spec 14.7).

## Design tokens

- **Type:** IBM Plex Sans 400/500/600/700 for UI; IBM Plex Mono 400/500 for ids, amounts in logs, code and kbd. Base 14 px; compact 12 px; headings 15.5 / 18 / 21 px, weight 600.
- **Themes:** daisyUI 5 `light` and `dark`, unmodified; set with `data-theme` on `<html>`.
- **Density:** `data-density="comfortable"` gives 36 px grid rows and 13 px text; `compact` gives 28 px rows and 12 px text (the floor).
- **Colour meaning** (never decorative):
  - `error` means risk: Likely or Very likely, confirmed TP, the Very high band, breached diaries;
  - `warning` means time pressure: Live money, due soon, Hold expiring;
  - `info` means new or changed;
  - `success` means confirmed or passed;
  - case-creating rules are shown by weight, not colour.
- **Merge-lane palette** (the only categorical colours; each lane also carries its letter): border `oklch(0.62 0.07 H)`, background `oklch(0.95 0.02 H)`, text `oklch(0.42 0.07 H)`, with H = 250 (A), 160 (B), 70 (C), 320 (D).
- **Radius, shadow, spacing:** daisyUI and Tailwind defaults (`rounded-box`, `shadow-sm` on cards, `shadow-xl` on lightboxes and drawers; 4 px spacing scale).
- **Icons:** Lucide.

## Assets

- `design/img/*.png`: screenshots captured from the prototypes, referenced by figure number, plus the wordmark and buffalo used on the cover.
- `brand/`: the style guide, wordmark and buffalo. Tenant logos on the referral and RFI portals are placeholders.
- Fonts come from Google Fonts in the prototypes; vendor them in the product.

## Files

| File | What it is |
|---|---|
| `Jube v2 - What is being built.pdf` | The document as a PDF, for the website |
| `brand/Jube Brand and Style Guide.docx` | Brand and style guide: logo, buffalo, colour, type, writing |
| `Jube v2 - What is being built.docx` | Word copy of the same document, generated from the Markdown (opens in Word and Google Docs) |
| `design/Jube v2 - What is being built.md` | What is being built, in Markdown (source of truth); images in `design/img/` |
| `design/Decision log.md` | How each choice was reached, code-audit answers, what is still open |
| `Jube v2 - What is being built.dc.html` | The same document as a printable web page (open in a browser) |
| `Jube Case Management v2 - spec 0.11.dc.html` | Clickable prototype: Cases page, Case page, drawers, lightboxes, held payment and step-up, tags |
| `AskJooby screens.dc.html` | AskJooby full page, corner pop-up, Bites, and an agent proposal through snapshot and maker-checker |
| `Jube Case Management v2 - pass 4 screens.dc.html` | Static board of the remaining screens (A1–F5) and market gaps (M1–M6) |
| `support.js`, `design/doc-page.js` | Prototype runtime only; not for the product |
| `skill-vue-blueprint/SKILL.md` | Vue 3 blueprint (the Claude Code skill) |
| `specs/…spec_0.11.md`, `specs/ui-specification-golden-audit.md` | Specification and code audit |

## Open items

Answered against the code on 27 September 2026, and folded into SKILL §0 (D-V13 to D-V17), §2, §5, §7 and §9.

**Resolved:**
- **Session refresh:** `TokenRefreshMiddleware` extends the session on every request, so there is no route to call. The blueprint adds a keep-alive while the user is active.
- **Current user:** `GET /api/Me` is new (CG-23), as is `PaymentReleaseLimit`.
- **Rule validate and completions:** existing `POST /api/Parser` and `GET /api/Completions/ByEntityAnalysisModelIdParseTypeId`.
- **Excel export:** client-side with SheetJS through Tabulator. This also removes today's external `proxy.jube.io` download proxy.
- **Bearer and cookie handling:** superseded by D-V27 (below). Cookies leave the auth pipeline; the JWT is sent as `Authorization: Bearer`.
- **AskJooby:** Analyst Agent persona next, then Rule Writer. M1 (agent triage) is off by default, opt-in per filter. M4 (web research) is opt-in per tenant, and every outbound query is logged.
- **Scope order:** M2 and M3 first (V13a), then M1, M6, M5 and M4 (V13b).
- **Blueprint licence:** bought when build starts.

**Decided:**
- **Hold rules:** hold rules live in a `PaymentHoldPolicy` table with one row per market (D-V18, CG-24/25). The row covers the ceiling, how it is counted, when the clock starts, what happens at the ceiling, and the escalation timings.
- **Payer notice:** optional per market (D-V19).
- **Egypt row:** compliance fills it in. Until then, it defaults to **EscalateOnly** with no automatic action and no notice.

**Also carry into spec 0.12:**
- CP-18 and CP-19;
- AskJooby personas beyond Researcher (new work);
- confirm the exact Account/Login route in V2.
