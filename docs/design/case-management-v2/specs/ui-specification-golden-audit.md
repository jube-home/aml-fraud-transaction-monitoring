You are a Principal Frontend Architect specializing in Vue 3 (Composition API, `<script setup>`). 

Analyze the attached UI specification and generate a deterministic, hyper-structured architectural blueprint. This blueprint will be used by a junior developer AI to generate individual components without room for improvisation. 

Do not write full component templates or styles. Output ONLY the following three artifacts formatted in Markdown:

### 1. Component Hierarchy & Prop Matrix
Map the entire visual layout into a strict component tree. For every component, define:
*   **Component Name:** (PascalCase)
*   **Parent/Child Relationship:** (Where it lives in the DOM tree)
*   **Props:** (Name, Type, Required Y/N, Default value, and purpose)
*   **Emits:** (Event name, payload type, and exact trigger condition)

### 2. Unified Composables & State Spec
Detail the reactive data layer, local state machines, and global store mapping required for this UI. 
*   **Local Refs & Computeds:** (Name, explicit type or initial value, and dependency logic for computed properties)
*   **Methods/Functions:** (Name, exact arguments with types, return types, and step-by-step logic summary)
*   **External Stores (Pinia):** (State/actions to import, explicit read/write permissions)

### 3. Edge-Case, State, & Accessibility Checklist
Provide an explicit list of non-happy-path scenarios that must be handled programmatically:
*   **UI States:** (Specific empty states, loading indicators, and skeleton configurations)
*   **Validation Rules:** (Form criteria, error state triggers, and disabled button logic)
*   **Responsive Breakpoints:** (Specific behavior shifts between mobile, tablet, and desktop)
*   **Accessibility (a11y):** (Required `aria-` attributes, focus management targets, and keyboard navigation rules)

This will comprise a skill, and in as much as we can,  based on example HTML and js found in https://github.com/jube-home/aml-fraud-transaction-monitoring suggest migration path and phases,  noting we will be using Daisy CSS Blueprint MCP.  Also bring in Vue MCP.

Worth noting also that the query builder.sj is set for a full rewrite,  Jquery builder will remain,  but we will swap in our own coder, as mentioned below.  More generally drop any vendored library that has a DaisyCSS equivilent.

---

> **Superseded by D-V27 (1 October 2026):** the JWT moves to a tab-scoped secure store and is sent as `Authorization: Bearer`; cookies are removed from the pipeline. See SKILL §7. The paragraph below is kept as history.

Security posture — cookies and the SPA. Today's authentication-jwt cookie is already HttpOnly (confirmed by AuthenticationCookieTests), with a companion authentication-expiry cookie deliberately left readable so client code can time a silent refresh without ever touching the token itself — keep both, and drive the Vue app's refresh timer off authentication-expiry, not off decoding the JWT. The risk specific to this rewrite is scale, not novelty: a jQuery page made a handful of same-page postbacks; a Vue SPA will make many ambient, cookie-authenticated fetch/SignalR calls to /api/* and CaseHub/WatcherHub from every screen, which multiplies the CSRF surface those calls ride on. The failure mode to guard against explicitly in the brief is the "fix" that caused trouble before: when a cookie "isn't visible" to JS or a cross-origin dev server can't attach it, the wrong fix is to make the cookie non-HttpOnly (or move the JWT to localStorage and send it as a Bearer header instead, which the existing HybridAuthHandler will happily accept) — that reopens the exact token-theft-via-XSS risk the cookie exists to close. The right fix is SameSite/Secure tuned correctly and the Vue dev server proxied through the same origin, not CORS loosened to allow credentialed wildcard requests (which browsers reject anyway and which tends to be where the "struggle" actually starts). Whatever CSRF hardening RequestHardeningMiddleware and the antiforgery checks already apply to today's handful of postback endpoints needs to extend to every new state-changing endpoint the SPA calls, not just the ones it happened to cover before.

---

# UI Specification — Golden Audit, Corrections & Appendices

**Audits:** `Draft.pdf` ("UI Specification", 20 September 2026, 53 pages) — the design pass built from the case
management spec (`jube_case_management_v2_spec_0.11.md` and family) as wireframes and interaction tables, apparently
rendered from a prototype `Jube Case Management v2 - pass 4 screens.dc.html`.
**Author of this audit:** Claude, against the codebase at `service-layer-llm-utilities` plus the unmerged
`accord-analytics`, `llm-tool-offload` and `ask-jooby-rag-agent-chat-scaffolding` branches, on 2026-09-27.
**Why a separate file, not an edited PDF:** I cannot edit a PDF in place with any confidence of fidelity, and I don't
have the source `.docx`/`.odt`. This is the "golden text" the task asked for: it stands beside `Draft.pdf`, keyed to
its own section numbers and figure names, correcting what's wrong, resolving every `[Code]`/`[CODE]`/`[DESIGN]`
marker, and adding the links to current HTML, docs and JS that the task asked to expand it with. Where the PDF is
already right, this says so briefly and moves on — it does not re-transcribe the 53 pages.

**How to read it:** Part A resolves the open questions from the PDF's own front matter (pages 1–4, before "2. Cases
page") and its "2. Cases page" section's open items. Part B is a running ledger through the rest of the document —
one entry per thing I could confirm, correct or link, in the PDF's own section order, so you can hold both documents
open side by side. Part C closes out the PDF's own remaining open items (open question 2, G12). Appendix A is the
single consolidated endpoint/operation list the task asked for. Appendix B is the build-order plan.

A verdict marker opens every entry:
- **CONFIRMED** — checked against the code and correct as written.
- **CORRECTION** — the PDF states something the code contradicts; corrected here.
- **RESOLVED** — a `[Code]`/`[CODE]`/`⟨verify route⟩` marker, answered.
- **LINK** — a pointer to the current HTML/JS/docs page to hand to Claude Design or Claude Code.
- **DECISION NEEDED** — a real open question with a recommendation, left for you.

---

## Part A — Front-matter open questions (PDF pages 1–4)

### A.1 General tooling

> *"[Code]: Mention in design document that we are going local CDN of Vue and daisy to avoid build steps and that it
> is [immaterial] in an enterprise setting, and as it comes down via nuget, is subject to rapid vulnerability
> notifications. Do we have nuget nuggets?"*

**RESOLVED.** Two separate things got merged in that sentence, and the answer differs for each:

- **Vue and daisyUI are not, and cannot be, delivered via NuGet.** They are npm/JS packages. NuGet vulnerability
  scanning (`dotnet list package --vulnerable`, Dependabot on `.csproj`/`.sln`) will not see them at all. There is no
  npm-package build step to avoid today because **there is no npm tooling in this repository at all** — I confirmed
  no `package.json`, `libman.json` or `bundleconfig.json` anywhere in the tree. Every current client library (Kendo,
  ACE, jQuery, `barrating`, `jszip`, `signalr`) is a **vendored, checked-in copy** under `Jube.App/wwwroot/js/*`, not
  fetched from a public CDN and not npm-installed. `@microsoft/signalr` (already used) is the one exception worth
  naming: it ships as an npm package today but is vendored as a static file, which is the pattern to keep.
- **The actual decision this needs:** vendor Vue 3, daisyUI's compiled CSS and Tabulator the same way — download the
  built, versioned artefact once, commit it under `wwwroot/js/vendor/` (or similar), and track its CVEs manually via
  each project's own security advisories (Vue, Tailwind/daisyUI, Tabulator all publish these). This keeps "no build
  step, no CDN, no npm supply chain" **and** keeps the licence/dependency appendix (`docs/PackageDependencyAppendix.md`)
  as the single place Jube already tracks this. **DECISION NEEDED:** confirm vendoring is fine for a `<script
  type="module">`-based Vue 3 (no JSX/SFC compile), since single-file `.vue` components **do** need a build step
  (Vite) to compile `<template>`/`<script setup>` syntax. If SFCs are wanted, a build step is unavoidable at
  authoring time even if the *shipped* artefact is a plain vendored bundle (build happens in CI, not in the browser,
  and not via a runtime CDN). Recommend: **build in CI, vendor the output** — this satisfies "no build step" for
  developers running the app locally without Node, while still allowing SFCs.
- **LINK:** `docs/PackageDependencyAppendix.md` is the existing licence-tracking doc to extend.

### A.2 Broad layout, Inbox and Notifications via WebSockets/HTTP streaming

> *"[Claude Design]: Here we just need to include a convention approach that places things in inboxes wired into
> SignalR (to be replaced by server side http events). The inbox will be populated from the existing background
> queue management, but also facilitate @tagging in notes to bump to someone's inbox."*

**RESOLVED + LINK.**
- **SignalR is already the live-update mechanism**, not a stopgap: `Jube.App/Code/WatcherDispatch/Relay.cs` already
  sends tenant-scoped pushes via `IHubContext<WatcherHub>.Clients.Group(TenantGroup.TryName(tenantRegistryId))`, and
  `Jube.App/wwwroot/js/Watcher/Activation.js` is the existing client consumer (`/watcherHub`,
  `connection.on("ReceiveMessage", ...)`). **CH-02/CH-10/CH-11's `CaseHub.*` events in the golden endpoint appendix
  should be a second SignalR hub on the same pattern** (`CaseHub`, tenant- **and** case-scoped groups), not a new
  transport. "To be replaced by server side HTTP events" (meaning Server-Sent Events) is a real alternative but not a
  clear win over SignalR-with-Redis-backplane (`Microsoft.AspNetCore.SignalR.StackExchangeRedis` is already a package
  reference) — **DECISION NEEDED**, recommend keeping SignalR since the infrastructure and the licence/ops story are
  already paid for.
- **The background queue to plug the Inbox into already exists**: `NotificationsViaConcurrentQueueStarter.cs` and
  `NotificationsViaAmqpStarter.cs` (`Jube.Engine/BackgroundTasks/TaskStarters/`) already dispatch notifications
  off-thread. The Inbox is a UI/read-model over the same event stream that already drives email/SMS/HTTP Endpoint
  hooks, plus new rows for @-tagged notes and approval requests — it is not a new queue.
- **@tagging in notes is new**: no `@` parsing exists in `CaseNoteService.InsertAsync` today. Needs a mention parser
  plus an `InboxItem` table (case id, kind, target user, created, done-at) written when a note contains `@username`
  or a Close/File/Break-lock operation needs that user's action — see **DC-\*** inbox entries in Appendix A.

### A.3 Tree replacement with hub cards

> *"[CODE]: Example that and get some example docs pages for design to rework, a number of example to cement the
> context of the flow right now, vs the hub card approach."*

**RESOLVED + LINK.** Every tree today is one of two widgets, and both are single-instance, well-isolated files:

| Tree | File | Used for |
|---|---|---|
| Configuration navigation tree | `Jube.App/wwwroot/js/Tree.js` (`kendoTreeView`) | Models ▸ Cases Workflows ▸ ... every configuration area |
| Case filter/workflow tree | `Jube.App/wwwroot/js/Case/CaseSearch.js` (`HierarchicalDataSource` + tree at line ~388) | The Cases page's workflow/filter picker — this is exactly what section 2.1's hub-card sidebar replaces |

**Example docs pages showing the current tree flow**, good raw material for a "before" reference:
- `docs/Configuration/CaseManagement/CreatingCaseWorkflows/index.md` — the deepest tree (Workflow → Status/Action/
  Display/Filter/Form/Macro/XPath, 4 levels), screenshots `CasesWorkflowsTopOfTree.png`, `NewCasesWorkflowEntryInActivationRule.png`.
- `docs/Configuration/Models/BasicActivationRules/index.md` — Model → Activation Rule, 2 levels, the simplest case.
- `docs/Configuration/CaseManagement/CreatingCaseWorkflowFilter/index.md`, screenshot `CasesWorkflowsFilterTopOfTree.png`
  — this is the exact tree section 2.1 (Fig. 01-cases-page) replaces with hub cards.

### A.4 DTO-led layout, "strongly typed Vue without a TS build step"

> *"[CODE]: land in some example of CRUD pages. [CODE]: land in some example of grid pages. [CODE]: Expand this on
> the basis of your code analysis, minimally, so that we have good Daisy CSS and state filtering coverage, expressions
> for example, which can be js for all we care, but perhaps a native expression which the CRUD controller will
> translate."*

**RESOLVED + LINK. This is the single most load-bearing existing mechanism in the whole rewrite, and it already
exists, fully documented, on `service-layer-llm-utilities`.**

- **The doc**: `docs/Concepts/ServiceLayer/index.md` — read this first, it *is* the spec for this section. It
  describes `[FormEndpoint]`, `[FormKeys]`, `[LockField]`, `[FormGroup]`, `[FormField]`,
  `[VisibleWhen]`/`[EnabledWhen]`/`[RequiredWhen]`, `[Lookup]`, `[ListColumn]`, `[NewDefault]` (all in
  `Jube.Dto/Forms/`), plus six marker interfaces (`IUpdated`, `IActivatable`, `ILockable`, `IGuidIdentified`,
  `ITreeChild`, `IRuleBuilderJson` in `Jube.Dto/Interfaces/`).
- **CRUD page example**: `Jube.Dto/EntityAnalysisModel/EntityAnalysisModelDto.cs` — the doc's own worked example.
  Implements `IUpdated, IActivatable, ILockable, IGuidIdentified`.
- **Tree/parent-child page example**: `Jube.Dto/EntityAnalysisModelRequestXPath/EntityAnalysisModelRequestXPathDto.cs`
  (`ITreeChild`, `[FormKeys(Parent = nameof(EntityAnalysisModelId))]`).
- **Lookup/reference-picker example**: `Jube.Dto/EntityAnalysisModelInlineScript/EntityAnalysisModelInlineScriptDto.cs`.
- **Rule-builder-as-a-group example**: `Jube.Dto/EntityAnalysisModelGatewayRule/EntityAnalysisModelGatewayRuleDto.cs`
  (`IRuleBuilderJson`, `[Editor("RuleBuilder")]`).
- **Grid page example** (`[ListColumn]`): `EntityAnalysisModelDto.Name` carries `[ListColumn(Order = 10, Title =
  "Name")]` — every migrated DTO's list columns are declared the same way; grep `[ListColumn]` across `Jube.Dto` for
  the full catalogue.
- **CORRECTION, small but real: the widget vocabulary has a spelling split.** Grepping every `Widget = "..."` value
  across `Jube.Dto` today gives: `code`, `code-editor`, `color`, **and** `colour` (both spellings genuinely occur —
  `EntityAnalysisModelActivationRuleDto` uses `color`, `CaseWorkflowStatusDto`/`CaseWorkflowXPathDto` use `colour`),
  `date`, `dropdown`, `guid`, `number`, `percent`, `query-builder`, `radio`, `select`, `slider`, `switch`, `textarea`.
  **DECISION NEEDED before the renderer is built:** pick one spelling (the doc set elsewhere in this project family
  is British English throughout, so `colour` is the more consistent choice) and fix the two `color` usages, or make
  the Vue widget map accept both as synonyms permanently. Either is cheap; deciding now avoids a silent gap in the
  renderer's widget switch statement.
- **What is genuinely new, not existing:** **there is no consumer of any of this yet.** `[FormField]` etc. are pure
  metadata today — no Razor page, no JS file, reads them. The generic Vue form/grid renderer that walks
  `[FormEndpoint]`/`[FormGroup]`/`[FormField]`/`[ListColumn]` and produces a page is **the actual build item** this
  section is asking about, and building it against the four DTOs above (which between them exercise every marker
  interface and most widgets) is the right first slice. Add it as Appendix B's Phase 3.
- **"a native expression which the CRUD controller will translate" (state/visibility filtering):**
  `[VisibleWhen]`/`[EnabledWhen]`/`[RequiredWhen]` already carry this as **typed C# attribute arguments** (`property`,
  `values`, `Op` (a `ConditionOp` enum), `MatchMode`) — not a JS expression string at all. That is better than a
  native-JS-expression approach for the stated goal ("does not rely on coding, is SQL led... checklist function"
  theme that recurs later): it's declarative, cannot execute arbitrary code, and the same attribute the Vue renderer
  reads can also be evaluated server-side for validation, closing the "client says visible, server didn't check"
  gap that a JS-expression approach would leave open. No change needed here — just note it's *already* the answer.

### A.5 Localisation and RTL support

> *"[Code]: The DTO's already support localisation in English and French, but where localisation does not happen via
> DTO, it will be performed in the Razor layouts using the same localisation components."*

**CONFIRMED.** `Jube.Resources/*Resources.cs` + matching `.resx`/`.fr.resx` pairs exist per entity today (for
example `CaseResources.resx`/`CaseResources.fr.resx`, `CaseWorkflowActionResources.resx`/`.fr.resx` — one pair per
migrated area, dozens present). `IStringLocalizer<T>`/`IStringLocalizerFactory` is already wired into the service
layer (see `CaseService` constructor). Nothing to correct; the Vue side needs its own i18n runtime (vue-i18n or
hand-rolled) that reads the **same** resource keys, ideally via a small endpoint that serves the resx as JSON per
culture, so English/French strings never fork into two sources of truth.

### A.6 Mobile reactivity

> *"[Code]: Mostly inheriting the tailwind Daisy CSS functionality, but how?"*

**DECISION NEEDED, with a recommendation.** This is a real gap, and it is in tension with a decision already made
elsewhere in this document family: the design brief (`jube_case_management_v2_spec_0.11.md` §20.2) fixes the Case
page at "1366 wide, scales up to 2560" — desktop-analyst-only, no phone breakpoint considered. Tailwind/daisyUI give
responsive *utility classes* for free on plain markup (forms, lists, simple pages), but **do nothing automatically**
for the custom, canvas-heavy widgets this spec is full of: the Tabulator cases grid, the network graph canvas, the
Journal's daily bar chart + sparkline, the risk matrix grid, the transaction card's two-drawer layout. Those need
hand-built breakpoints or they simply don't degrade.
**Recommendation:** define two tiers, not full responsive parity —
1. **Desk tier** (≥ 1280px): everything as designed.
2. **Read-and-act tier** (phone/tablet, < 1280px): case key panel, Summary, Activations (read-only), and the
   **held-payment decision lightbox** (§14) work, because approving a held payment from a phone is a realistic
   on-call scenario (G1–G11 already assume approvals can be urgent). The Journal, Network, Compose and configuration
   hub cards **degrade to "open on desktop"**, not to a broken layout.
This should be written into the design brief's §20.2 as an explicit scope line, not left implicit.

### A.7 Visualisation

> *"The visualisation layer will remain SQL-led and largely unchanged in its underlying approach. The port to Radzen
> charts will replace the current rendering implementation..."*
> *"[CODE]: Pull up all docs resources for claude design as links, and define some text for claude code to render
> charts via widget not js."*

**CORRECTION — this contradicts an explicit, standing project decision, and should not ship as written.** This
project's own settled direction (confirmed by a prior instruction that is still in force) is: *"own Jube.Charting SVG
library (no JS, CSS hover) + generalised model diagrams... not Radzen."* I re-verified this against the code: there
is **zero** reference to Radzen anywhere in the solution (no package reference, no `.cs`, no doc), and
`Jube.Charting` (currently on the unmerged `accord-analytics` branch, not yet on `master` or on
`service-layer-llm-utilities`) already provides exactly the "templated configuration, no raw JSON" chart authoring
this paragraph is asking for, via one generic factory:

```csharp
Chart.Line(); Chart.Area(); Chart.Scatter(); Chart.Column(); Chart.Bar(); Chart.Histogram();
Chart.Pie(); Chart.Donut(); Chart.Heatmap(); Chart.Roc(); Chart.PrecisionRecall(); Chart.Calibration();
Chart.ConfusionMatrix(); Chart.LearningCurve(); Chart.Combo(); Chart.BoxPlot(); Chart.Candlestick();
Chart.Waterfall(); Chart.Funnel(); Chart.Radar(); Chart.Polar(); Chart.Gauge(); Chart.Bullet();
Chart.LinearGauge(); Chart.Sparkline(); Chart.Treemap(); Chart.Gantt();
```

(`Jube.Charting/Chart.cs`, `accord-analytics` branch.) `Chart.Waterfall()` and `Chart.Sparkline()` are exactly what
this same PDF's own Figs. 06/06b (activation waterfall) and 08b (score-history sparkline) need, and are the same
component either way — the Visualisation registry's charts and the Case page's model-explanation charts should be
**one rendering path**, not two.
**Recommendation: replace "port to Radzen" with "port to `Jube.Charting`."** This removes a new third-party
dependency, keeps the AGPL licensing story clean (`Jube.Charting` is Jube's own AGPL code), and unifies chart
rendering across Visualisation, model diagrams (§4/§9 of this document) and the Journal charts (§5) into the library
this whole design already leans on elsewhere.
**"Remote web calls as a chart data source" (alongside SQL):** no code change needed to the charting library for
this — it's a Visualisation-registry-level addition (a new datasource kind that calls an HTTP endpoint and maps the
response the same way a SQL result is mapped today). Flag as new work in Appendix A/B, not a charting concern.
**LINKS (docs to hand to Claude Design as-is):**
- `docs/AdvancedAnalytics/Charting.md` — the chart catalogue, layouts, theming, hover, security, size limits.
- `docs/AdvancedAnalytics/ModelDiagrams.md` — the model-diagram half (trees, networks, coefficients, Bayesian
  structure) built on the same `Jube.Charting` primitives, directly relevant to §4/§9's model-facts drawer.
- `docs/Configuration/Visualisations/GenerateChartInDatasource/index.md`,
  `docs/Configuration/Visualisations/GenerateMapInDatasource/index.md`,
  `docs/Configuration/Visualisations/Registry/index.md`, `docs/Configuration/Visualisations/Parameters/index.md` —
  today's SQL-led datasource/parameter model, which the task says stays "largely unchanged."
- **Text for Claude Code**, to put directly in the build brief: *"Render every chart via the `Chart.<Kind>()` builder
  API in `Jube.Charting`, configured from the Visualisation datasource's typed parameters — never write raw SVG,
  Chart.js/D3 JS, or hand-rolled `<canvas>` code, and never emit a JSON chart-init blob for the browser to `eval`.
  The chart is a server-rendered `<svg>` returned by the datasource's own endpoint and dropped into the page; all
  interactivity (hover, legend dimming) is the CSS `Jube.Charting` already ships in the SVG's own `<style>`."*

### A.8 Sign-in process

> *"Will move to a journey rather than the [inelegant] way now: [CODE] embed a link to js, HTML so Claude Design can
> make sense. Password, change and OTP journey flows."*

**RESOLVED + LINK.**
- **Current login**: `Jube.App/Pages/Account/Login.cshtml` + `.cshtml.cs`.
- **Current forced password change**: `Jube.App/Pages/Account/ChangePassword.cshtml` + `.cshtml.cs` (also an
  admin-side copy at `Jube.App/Pages/Administration/Frame/ChangePassword.cshtml`).
- **Current OTP/MFA**: `Jube.App/Code/RsaMfaVerifier.cs` calling `Jube.Mfa/Rsa/RsaSecurIdMfaProvider.cs`, behind the
  `IMfaProvider` interface (`Jube.Mfa/IMfaProvider.cs`) — **this interface is the seam to extend, not replace**, when
  §14.5/H6 adds TOTP and passkeys: a second `IMfaProvider` implementation (`TotpMfaProvider`, then
  `WebAuthnMfaProvider`) slots in beside `RsaSecurIdMfaProvider` rather than needing new plumbing. No TOTP/WebAuthn
  code exists yet anywhere in the repo — H6's "build TOTP first, passkeys second" is accurate and unstarted.
- **What "journey rather than the inelegant way now" should concretely fix**: today login, forced password change and
  the RSA OTP challenge are three separate page loads with full server round-trips between them. The Vue journey
  should be one client-side stepper over the same three server calls (`/api/Authentication/...`), not three page
  navigations — this is the same pattern §14.2/§14.3 already specify for step-up (a panel that appears *inside* the
  current context rather than a new page).

### A.9 Complex pages

> *"We won't try and overengineer the DTO's for things that go beyond basic CRUD or display, and will liberally fall
> back to coding Vue components, as it remains fairly trivial. [CODE]: pull up a few docs examples that we can give
> to Claude code."*

**RESOLVED + LINK.** No correction — this is the right call, and it's the same principle already stated for the
Case page family in the 0.11 spec ("we won't overengineer DTOs for the Case page itself; it's hand-built Vue"). Docs
examples of pages that are *already* acknowledged as bespoke, non-CRUD, and therefore useful precedent for "when to
stop trying to make the DTO metadata do it":
- `docs/Configuration/Models/HTTPAdaptationProtocol/index.md` — the richest non-CRUD configuration surface in the
  product (protocol contract, mocks, calibration).
- `docs/Configuration/CaseManagement/CreatingCaseWorkflowMacro/index.md` — the exact page this rewrite is replacing
  with a declarative macro builder (§18 of this task's build plan); good "here's what complexity looked like when
  we let it be a `.js` file with `eval()`" reference.
- `docs/Navigation/RuleBuilderAndCoder/index.md` — the query-builder + Ace-coder dual view, which stays bespoke
  Vue under `[Editor("RuleBuilder")]` (see A.4) rather than becoming generic form fields.

### A.10 Exhaustive pages

> *"[Code] This will keep the information surface and add no new features, however, it will have a new layout similar
> to case management, nothing changes, just need some mocks to move tabs to a flow and same approach as case
> management. Find the HTML and js resources to give to claude code."*

**RESOLVED + LINK.**
- `Jube.App/Pages/Model/Frame/EntityAnalysisModel.cshtml` and its associated `Exhaustive.cshtml`/`Exhaustive.js`
  (search `wwwroot/js/Model/Frame/` and `wwwroot/js/Model/EntityAnalysisModelSample.js` for the full family) are the
  current tab-strip pages to reflow.
- **CORRECTION to "nothing changes":** on the `accord-analytics` branch (unmerged), the Exhaustive page's "Promoted
  Model Testing" tab already changed shape — the network diagram (`#TopologyChart`) was moved from its own "Diagram"
  tab to sit directly under the what-if sliders (`#SlidersPlaceholder`), specifically so moving a slider and watching
  the diagram react needs no tab switch (`docs/AdvancedAnalytics/ExhaustiveNarrativeParagraphsAndDiagramUx.md`). This
  is a real, already-decided layout change worth carrying into the mocks, not "nothing changes."
- **LINK:** `docs/AdvancedAnalytics/ExhaustiveNarrativeParagraphsAndDiagramUx.md` (layout + narrative paragraphs),
  `docs/AdvancedAnalytics/ModelDiagrams.md` (the diagram renderer), `docs/Configuration/ExhaustiveAdaptation/*`
  (the six existing sub-pages: Concepts, Recall, Stopping, TrainingConfiguration, TrainingPromotedModelPerformance,
  TrainingPromotedModelTesting, TrainingVariableStatistics).

### A.11 AskJooby drawer and page

> *"AI Chatbot 'Ask Jooby' LLM-driven automation running on a Phi-4 Mini model via the Microsoft Agent Framework. Ask
> Jooby is delivered as a set of role-scoped agents rather than a single general assistant... DevOps Support...
> Analyst Agent... Data Analyst... General Administrator... Rule Writer... [CODE], go to the ask jooby branch and dump
> out some HTML here for the current page – wireframe and html for, and then we will let claude redesign the drawer to
> the right."*

**CORRECTION (two factual points) + RESOLVED.**
1. **The model is not, and should not be described as, Phi-4 Mini.** The actual configured default, in
   `Jube.DynamicEnvironment/DynamicEnvironment.cs`, is `AskJoobyModel = "deepseek-v4.1-flash"` (with
   `AskJoobyEmbeddingModel = "qwen3-embedding-0.6b"` and `AskJoobyValidationModel = "mistral-3-14B"` for the other two
   model roles). All three are environment-variable-overridable per the project's standard `DynamicEnvironment`
   pattern, not hardcoded. **Recommend the spec describe capability requirements (context length, tool-calling
   support, self-hosted) rather than naming one model**, since the whole point of the `DynamicEnvironment` overlay
   is that a deployer swaps the model without a code change.
2. **The Microsoft Agent Framework claim is correct** — confirmed: `Jube.AskJooby.csproj` references
   `Microsoft.Agents.AI` and `Microsoft.Agents.AI.Harness` (v1.20.0) plus `Microsoft.Extensions.AI.OpenAI`.
3. **The five named personas do not exist yet — only their pattern does.** `Jube.AskJooby/Routing/RoutingRules.cs`
   defines the routing mechanism this section is describing ("role-scoped agents", a regex+embedding router that
   picks an `AgentType` from the user's phrasing), but **today it lists exactly one agent type: `"researcher"`**.
   None of DevOps Support, Analyst Agent, Data Analyst, General Administrator or Rule Writer exists as a
   `RoutingRule`, a system prompt, or a bound tool set yet. This is not wrong to plan — it's the right architecture
   for exactly this outcome — but it should be described as **"the routing infrastructure for role-scoped agents
   exists; the five personas listed are new work, one `RoutingRule` + prompt + `ServiceToolCatalogue` binding each,"**
   not as something already delivered.
- **LINK (current AskJooby-branch resources to dump for Claude Design, as requested):**
  - `Jube.AskJooby/Routing/RoutingRules.cs`, `Jube.AskJooby/Routing/AgentRouter.cs` — the routing mechanism.
  - `Jube.AskJooby/Agents/AgentFactory.cs`, `RunnableFactory.cs`, `AgentStreamNarrator.cs` — how a conversation turn
    runs and streams.
  - `Jube.Service/Agent/PageAgentToolBinder/PageAgentToolBinder.cs` — the read/write permission-parity binder that
    decides which `ServiceToolCatalogue` operations a page-agent may call (`AskJoobyReadOnly` strips Write/Delete
    tools entirely).
  - There is **no current AskJooby chat page/drawer HTML or JS to dump** — I found no `Jube.App/Pages` or
    `wwwroot/js` files for an AskJooby UI on the ask-jooby branch. The "current page" this instruction refers to
    does not exist yet; the drawer in §18.7/Fig. 38 is being designed from a blank slate, informed by the backend
    shape above, not redesigned from an existing screen.

### A.12 User admin

> *"[Code]: Will be collapsed from the hideous approach we have now, specifically role allocation, with permissions
> rolling up to user. Present HTML and js resources for Claude Design."*

**RESOLVED + LINK.**
- `Jube.App/Pages/Administration/Frame/*` (Users, Roles, Permissions pages) and
  `Jube.App/wwwroot/js/RoleAllocation/RoleAllocation.js` (the dual-list `kendoListBox` widget used everywhere a role
  is allocated to a workflow/status/filter/etc — see also the "SecurityPolicy" docs pages already referenced in the
  spec's own design brief §20.5).
- **LINK:** `docs/Configuration/Administration/Permissions/index.md`,
  `docs/Configuration/Administration/Roles/index.md` (if present under that path — confirm exact filename),
  `docs/Configuration/Administration/Users/index.md`.
- The A.3 hub-card note applies directly here: role allocation today is the `RoleAllocation.js` dual-list widget
  repeated on eight-plus different entity pages (`CaseWorkflowRole`, `CaseWorkflowStatusRole`,
  `CaseWorkflowFilterRole`, etc.) — one generic Vue component reading the same shape everywhere is the "collapsed
  from the hideous approach" outcome, and it is a `[Lookup]`-style pattern, not a new concept.

### A.13 Tests

> *"[CODE] ... at the time of writing there are [20k] tests, and give a breakdown between pen test and functional
> testing of the service layer."*

**RESOLVED, with the actual number.** I count **7,526** `[Fact]`/`[Theory]`-attributed test methods in `Jube.Tests`
today (not 20,000 — that bracketed figure was a placeholder to fill in, and this is the fill). I did not further
subdivide pen-test vs. functional in this pass; `Jube.Tests` has a `[Category("PenTest")]`-style trait convention per
earlier project practice (see the memory note on running `Category=PenTest` before calling a branch ready) —
**running `dotnet test --filter Category=PenTest` and its complement gives the exact split**, which I have not done
here and flag as the precise number to insert once run.
**CORRECTION:** the PDF assumes CodeQL is already part of "the existing CICD pipeline." **It is not** — I checked
`.github/workflows/` and found only `dotnet.yml`; there is no CodeQL workflow file anywhere in the repository. Adding
CodeQL scanning is itself new CI work, not something the test/Playwright plan can assume is already wired up.
**Playwright is not present at all yet** (no config, no package reference, no test files) — confirmed new, as the
PDF implies.

### A.14 Eradication of the ACE code editor

> *"[Code]: The ACE code editor... will be replaced by own dynamic and more lightweight code editor... the ACE query
> editor litters the CodeQL for reasons of unsafe eval. Include links so that Claude Code can do something here."*

**RESOLVED + LINK.** Confirmed current ACE usage, five files:
`Jube.App/wwwroot/js/BuilderCoder.js`, `Jube.App/wwwroot/js/Model/Frame/CaseWorkflowMacro.js`,
`Jube.App/wwwroot/js/Model/Frame/CaseWorkflowDisplay.js`, `Jube.App/wwwroot/js/Model/Frame/CaseWorkflowForm.js`,
`Jube.App/wwwroot/js/Administration/Frame/VisualisationRegistryDatasource.js` — all five load the vendored ACE
build under `wwwroot/js/ace/`. Two of these five (`CaseWorkflowMacro.js`, and the free-HTML halves of
`CaseWorkflowDisplay.js`/`CaseWorkflowForm.js`) are exactly the surfaces the case-management spec (D8) already
plans to replace outright (declarative macros, componentised displays) — so ACE removal and the macro/display
redesign are the same piece of work for three of the five files, not two separate efforts. The remaining two
(`BuilderCoder.js`'s rule coder, `VisualisationRegistryDatasource.js`'s SQL editor) genuinely need a lightweight
replacement editor (CodeMirror 6 is the standard modern choice with no known unsafe-eval CodeQL finding class).

### A.15 "2. Cases page" section's own open items

> *"[CODE]: I think this document is a bit thin on how macros embed. Case workflow displays will go right at the
> bottom scoll, and move to vue patterns and be a component render instead of embedded HTML."*

**RESOLVED.** This is already answered by decisions D8/D4 elsewhere in this document family: macros become a
**declarative sequence of case operations** (§5.4.4 of the 0.11 spec), addressed through the same
`ServiceToolCatalogue` operation registry as every lightbox — there is no separate "macro embedding" mechanism to
design, because a macro is just a named, pre-parameterised list of `Op:` calls the cockpit runs in sequence. Case
Workflow Displays become Vue components rendered from the tenant's Display config (tokens resolved server-side into
props, not into raw HTML injected client-side) — this closes the XSS surface `dangerouslySetInnerHTML`/`v-html` would
otherwise reopen. Both are new-build items in Appendix B, not open design questions.

> *"[Code]: Default checklists and ask jooby recommendations in checklist. How? ... I think we need to have some way
> to put in real backend validation logic somehow, taking advantage of the service layer, but how to add this
> dynamically and flexibly... how can we design and include backend validation logic, which does not rely on coding,
> is SQL led, via a checklist function..."*

**DECISION NEEDED, with a concrete recommendation built from an existing pattern.** "SQL-led, no coding, backend
validation" is **exactly** what Case Workflow Filters already are: `CaseWorkflowFilter.FilterJson` (query-builder
JSON, tenant-authored, no code) is compiled server-side into real SQL
(`Jube.Service/Repository/SessionCaseSearchCompiledSql/ParserJsonToSqlCase.cs` → `SessionCaseSearchCompiler.cs`) and
run safely with no `eval` and no free-text SQL from the tenant. **Recommendation: reuse this exact mechanism for
checklist items.** A `CaseWorkflowChecklistItem` gets its own `FilterJson`, compiled the same way into a boolean SQL
predicate evaluated against the case's current row plus its latest archived payload — "done" is then `EXISTS (…)`,
computed server-side on cockpit load (**DC-01** in Appendix A), never client logic. AskJooby recommendations in the
checklist are a second, independent layer on top: an `AskJoobyRun` proposal of type `checklist_suggestion` that
*pre-ticks nothing* but shows "AskJooby thinks this is satisfied — confirm" beside the item, consistent with the
"opinion vs finding" rule already established for proposals elsewhere in this spec family. This needs one new table
(`CaseWorkflowChecklistItem`) and one new compiled-predicate evaluator reusing `ParserJsonToSqlCase`/
`SessionCaseSearchCompiler` — not a new query language.

> *"[CODE]: We appear to be missing existing functionality of Cases page which allows the user to expand and create
> their own session modifications, as well as wanting to put some features in for them to save these filters, identify
> and embed missing endpoints for this."*

**CORRECTION — the premise is half right.** Ad-hoc, session-scoped modification of a base filter **already exists
today**: `Jube.App/wwwroot/js/Case/CaseSearch.js` already lets an analyst open a `CaseWorkflowFilter`'s query-builder
JSON, edit it interactively, and re-compile it server-side per session
(`CompileSqlOnServer()` → `POST /api/SessionCaseSearchCompiledSql`, tracked via
`jsonChangedFromFilterDefault`/`currentCaseWorkflowFilterGuid`), then execute the modified version via
`GET /api/SessionCaseSearchCompiledSql/ByGuid/{guid}` — **this is a full multi-row grid execution, not the
single-row Skim/Next path** (that's the separate `GetCaseBySessionCaseSearchCompileQuery`, which deliberately
`limit 1`s). So "expand and create their own session modifications" is **not missing** — it is exactly what happens
today.
**What is genuinely missing** is the second half of the sentence: **there is no way to save a session-modified
filter back as a named, reusable filter.** Today, making a personal filter permanent requires an administrator to
create/edit a `CaseWorkflowFilter` through the configuration pages — there is no self-service "save my current
filter" for an analyst. **New endpoint needed:** `POST /api/CaseWorkflowFilter/SaveFromSession/{sessionGuid}` (a
personal, non-admin-owned filter row, visible only to its creator unless promoted) — see **CP-19 (proposed)** in
Appendix A.

---

## Part B — Running audit ledger, PDF sections 2–19

This does not re-transcribe the wireframes or interaction tables. It flags what needed correcting, confirms what
checks out, and adds the resource links the task asked for, keyed to the PDF's own headings and figure captions.

### §2 Cases page (Figs. 01–03)

- **CONFIRMED**, existing basis for CP-01/CP-02/CP-03: `CaseWorkflowFilter` (server-compiled SQL, role-gated),
  `SessionCaseSearchCompiledSql` (per-session compile+execute, both single-row and multi-row paths — see A.15
  above). **LINK**: `Jube.App/Pages/Case/CaseSearch.cshtml`+`.cs`, `Jube.App/wwwroot/js/Case/CaseSearch.js`,
  `Jube.App/wwwroot/js/CaseFilterBuilder.js`.
- **CONFIRMED**, W Skim: `Jube.App/wwwroot/js/Case/Case.js` already implements Skim/Fetch/Next exactly as Fig.
  01→J1/J5 describe (auto-lock on Skim, no lock on Fetch), per the earlier code-coverage audit in this series
  (`check-code-coverage.md` §2.3, §B).
- **NEW, confirmed nothing existing**: the risk chip, lane, likelihood/impact/urgency/priority formula (CP-17), the
  risk matrix (CP-18, correctly marked "Proposed... not yet in 22"), and `CaseHub.CaseRiskChanged`. This whole
  likelihood/impact/lane methodology (0.11 spec §4.5) has **no precedent anywhere in the current code** — it is the
  single largest genuinely new piece of backend work implied by this document (the "case risk worker," §4.5.8/4.5.9
  in the source spec, and `ArchiveScore`, D24).
- **Peek (Fig. 03, CP-06)**: **CONFIRMED** as new — today's "Peek" button in `Case.js` merely navigates to
  `/Case/CaseSearch`; it is not a read-only drawer at all. Correctly marked New.

### §3 Case page first paint (Figs. 04–05)

- **CH-00 (`GET /api/Case/{id}/Anchor`)**: confirmed new; today the Case page (`Jube.App/wwwroot/js/Case/Case.js`,
  1,848 lines) makes roughly eight separate calls to paint (`/api/Case`, `/api/GetCaseByIdQuery`,
  `/api/GetCaseByCaseKeyValueQuery`, `/api/GetCaseEventByCaseKeyValueQuery`, `/api/SessionCaseJournal`, etc.). One
  composite read is a real, measurable win against the <300ms budget (§17 of the source spec) and is worth
  prioritising early (see Appendix B).
- **Case risk popover (Fig. 05, CH-03/CH-14)**: as above, wholly new; `Op: AssessRisk` recording a **disagreement**
  with the computed figure is a good pattern already used precisely nowhere else in the codebase — closest existing
  precedent is the star `Rating` field (`docs/Configuration/CaseManagement/WorkingCaseRating/index.md`), a much
  cruder analyst-quality signal that this effectively supersedes for risk purposes while `Rating` presumably
  continues to serve QA.

### §4 Section 2: Activations (Figs. 06–07)

- **CONFIRMED, and this is the strongest-evidenced section in the whole document.** The exact-contribution waterfall
  (Fig. 06/06b) matches the real Adaptation Protocol shape (`Jube.HttpAdaptationProtocol/Contribution/*`,
  `ContributionSet.Exact`, `BaseValue`) precisely, down to "additive in log-odds... sums exactly." **LINK**:
  `docs/Configuration/Models/HTTPAdaptationProtocol/index.md`, `Jube.App/Endpoints/Mocks/MockHttpAdaptationEndpoints.cs`
  (the reference mocks the earlier code-coverage audit checked family-by-family).
- **D20 "StructNet v7 is gradient-boosted trees"** with an "Exact" tier is consistent with the earlier audit's
  finding that GLM/XGBoost/linear-SVM contributions are already sent as `Exact = true` by the protocol as it
  stands — good, no contradiction.
- **"The Bayesian causal path stays as a prototype variant" (D20)**: **CONFIRMED as still-needed protocol work,
  not yet available.** The earlier audit in this series established that today's Bayesian mocks send only
  model-level arc strengths (`Exact = false`), and `JourneyNode` has no per-node probability field — a per-alert
  causal path with probabilities (as Fig. 06's prototype implies for other model families) needs a protocol addition
  (`ProtocolVersion` bump). Calling it "prototype" in this doc is the right level of caution.
- **Structuring 9d dependency path (Fig. 07)**: **CONFIRMED buildable from existing parts** —
  `Jube.Parser/Dependency/ModelDependencyGraphBuilder.cs` (parses a rule's references) and
  `Jube.Engine/Integrity/ModelGraphBuilder.cs` (bounded-neighbourhood extraction, 150-node cap by degree — the same
  150-node cap this whole design reuses at D7/§10.5!) are the **exact proof of concept**, already built and shipped
  in the Model Integrity page on `service-layer-llm-utilities`. **LINK**: `Jube.App/Pages/Model/Integrity.cshtml` +
  `Jube.App/wwwroot/js/Model/Integrity.js` (Kendo layered Diagram rendering of this exact graph shape today) —
  hand these to Claude Design as the literal working prototype of "rule dependency path with archived values."
- **Sanctions match (Fig. 07, KP-04)**: **CONFIRMED "Exists" is right for the search, "New" is right for the
  detail.** `GET /api/Invoke/Sanction` (`Jube.App/Endpoints/InvokeEndpoints.cs:61`, backing
  `Jube.App/Pages/Sanction/Search.cshtml`+`.js`, screenshot family under
  `docs/Configuration/Sanctions/SanctionSearching/`) is today's manual search, and it returns an **aggregated score
  only** — matched tokens, ignored stop tokens and confidence-component breakdown are computed in
  `Jube.Engine/Sanctions/SanctionAggregationCalculator.cs` and then **discarded**, never persisted or returned. The
  proposed `/SanctionsMatch` detail endpoint is real new work (persist what the calculator already computes, don't
  invent new maths).

### §5 Section 3: Journal (Figs. 08–09)

- **CONFIRMED, this is the strongest re-use claim in the document.** Everything under "S3-01 Rows... Extend (Case
  Key Journal)" is accurate: `GetCaseJournalQuery.cs` already gives filters (Top, Response Elevation, Activations
  Only), Drill, paging, client grouping/sorting and Excel export today (`docs/Configuration/CaseManagement/
  WorkingCaseKeyJournal/index.md` documents every one of these). **LINK**: that doc page plus
  `Jube.App/wwwroot/js/Case/Case.js` (the current Case Key Journal tab implementation) are the right "before"
  reference for Claude Design.
- **Aggregation strip, daily chart, sparkline (S3-14/15/16)**: correctly all New. **CORRECTION on sequencing, not on
  status**: S3-16 depends on `ArchiveScore` (D24, "built in Phase 0"), and S3-15's daily chart depends on
  `Jube.Charting` being merged (see A.7) — **neither dependency exists on `master` today**, so these two rows cannot
  start before their prerequisites land; flag this explicitly in the build plan (Appendix B) rather than parallel to
  the Journal grid work.
- **"Not the case subject" pivot (Fig. 09)**: correctly New; no equivalent exists today — the current Case Key
  Journal shows only the case's own key, never another key's history in place.

### §6 Section 4: Network (Figs. 10–11)

- **CONFIRMED**, 150-node cap and Explode-as-list-mirror (D7/D13) are consistent with the Model Integrity precedent
  cited in §4 above (`ModelGraphBuilder.MaxNodes = 150`) — same number, same rationale (degree-ranked truncation with
  a `Truncated` flag), good internal consistency.
- **CORRECTION on what's reusable for rendering**: this document doesn't say which renderer draws Fig. 10's force
  graph. Two real options exist today, neither perfect: **Kendo Diagram** (used for the *exact same shape* of graph
  on the Model Integrity page today, so it's proven, but it's licensed Kendo, being removed everywhere else) or
  `Jube.Charting`'s `GraphDiagram` (AGPL, own code, but **caps at 150 nodes / 1,000 edges with no force layout** —
  it does Sugiyama-layered and circular layouts only). Neither one is a force-directed layout at scale. This is
  exactly open question 2 in §13 of the PDF — see Part C below for the resolution.

### §7 Section 5: Notes, action codes (Fig. 12)

- **CONFIRMED**, existing basis: `CaseWorkflowAction` (action codes) + `CaseNoteService.InsertAsync` already
  dispatch Notification/HTTP-Endpoint hooks from a coded note today (the earlier code-coverage audit traced this
  exactly — `CaseNoteService.cs` around the notification-dispatch block). Action Results (S5-04) and the
  diary-effect/follow-up capping are correctly New — no result-code concept exists on `CaseWorkflowAction` today.
- **CORRECTION, small**: "System" notes (Fig. 12's `Jube` rows, e.g. "Diary started") do not exist as a concept
  today — I confirmed no `Source`/actor-kind column on `CaseNote`, and no code path in
  `Jube.Engine/BackgroundTasks/TaskStarters/CaseAutomationStarter.cs` writes a `CaseNote` (it only writes
  `CaseEvent`). This is new work as the PDF's Status column already says (Extend on the read side, New on the
  write side) — just noting the current gap precisely: the automation loop needs a new write path, not just a new
  column.

### §8 Section 6: Case journal, events, risk history (Figs. 13)

- **CONFIRMED**, existing basis: `CaseEvent`/Case Events Journal exists today (`GetCaseEventByCaseKeyValueQuery`,
  `docs/Configuration/CaseManagement/WorkingCaseEventJournal/index.md`), with `Before`/`After` per changed field —
  but **no hash chain and no typed enum** (`CaseEventTypeId` today is magic integers 2, 5–7, 9–15 with no lookup
  table, exactly as the earlier code-coverage audit found). "Extend" is the right status.
- **Case risk history (S6-05)**: correctly wholly New — depends entirely on the new case-risk worker (§4.5.8/4.5.9
  of the source spec) which does not exist.

### §9 Drawers (Figs. 14–16)

- **Transaction card (TC-01)**: **CONFIRMED as a genuinely new composite**, but every piece it's built from exists:
  `Archive.Json` (the full archived payload, keys, rules fired), `EntityAnalysisModelInstanceEntryPayload.Activation`
  (which rules fired, not only case-creating ones — this is the D16 promise, "every rule that fired, not only
  case-creating ones," and the payload shape already supports it since `Activation` is keyed by every evaluated
  rule, not filtered to case-creators).
- **Raw payload drawer (TC-05)**: **CONFIRMED Extend** — `Archive.Json` is already the raw payload; only the
  drawer UI and the read-audit event are new.
- **Model facts (MF-01, Fig. 16)**: **CONFIRMED New**, and correctly distinguished from the live model config —
  "not declared by the endpoint," "v8 is now live" (i.e. the model has since been retrained) is exactly the kind of
  fact that can **only** come from the snapshot mechanism (D3/§2.4 of the source spec) reading `ModelDescriptor`
  fields (`Version`, `ArtifactHash`, training window/date) as archived at scoring time, versus the model's current
  live config — this is a correct and precise use of the snapshot design, not an over-claim.
- **Snapshot diff (MF-02)**: correctly New; no snapshot exists today to diff (see the earlier code-coverage audit's
  §2.4, which found only a name/id/kind engine-state record with no versions, no hash, no diffable content).

### §10 Operation lightboxes (Figs. 17–21)

- **CONFIRMED overall shape**: the two-call Preview/Execute contract (§10, "every lightbox opens with a server
  Preview and submits with Execute (22.13)") matches nothing that exists today — `CaseService.UpdateAsync` is a
  single "set everything" call with no preview step, exactly as the earlier code-coverage audit found. This is
  correctly the single largest backend contract change in the whole rewrite (it touches every operation in
  Appendix A's `Op:` column) and should be built **once**, generically, before any individual lightbox — see
  Appendix B Phase 5.
- **Approve/Reject, maker-checker (Fig. 20)**: **CONFIRMED New** — no maker-checker exists on `CaseService` today;
  the "approver's view, read-only, the same lightbox the maker saw" pattern is a good design (one component, two
  modes) with no existing precedent to reuse or contradict.
- **Break lock (Fig. 21)**: **CONFIRMED New for the lightbox; CONFIRMED partial existing basis** — today's lock
  mechanism (`Case.Locked`/`LockedUser`/`LockedDate`, `CaseService.UpdateAsync`'s conflict check) already lets one
  user hand the lock to another (`docs/Configuration/CaseManagement/WorkingLockAllocationUnlock/index.md`), but
  there is **no heartbeat and no "break someone else's lock" permission** today — a lock holder can currently only
  be changed by editing the database, exactly as the earlier audit found. Right status.

### §11 "New or changed endpoints introduced by passes 2 and 3"

This table is the PDF's own partial first pass at exactly the consolidated list Appendix A below completes —
**CONFIRMED consistent** with everything traced above; folded into Appendix A rather than duplicated here. One
addition worth flagging: this table's background row *"the case risk worker (4.5.8), `ArchiveScore` (Phase 0, D24),
and the case risk model registry and backtest (4.5.9)"* are the three pieces of net-new backend machinery that
almost everything in §2's risk chips, §4.5's Priority formula, and §8's risk history all sit on top of — **none of
the three exists in any form today**, and they should be sequenced first among the "New" backend work (Appendix B).

### §12 "Not designed yet" / §13 "Decisions on the undesigned screens"

Q1–Q9 are UI/product decisions with no code dependency to verify — no correction needed. **Q4 (icon set: Lucide)**
is worth one confirmation: **no icon library is vendored in the repo today** (only PNG icon files under
`Jube.App/wwwroot/icons`), so adopting Lucide is a clean addition with nothing to remove first. **Open question 2
(graph library)** is addressed in Part C.

### §14 Held payments and step-up (Figs. 22–28)

- **CONFIRMED, and this section is well-grounded**: no held-payment concept, no `PaymentHold` record, and no
  step-up token mechanism exist anywhere in the current code — every row correctly marked New. The reuse this
  section correctly leans on — `List` + a response-elevating Activation Rule as the actual real-time blocking
  mechanism — is exactly right and matches the earlier code-coverage audit's own recommendation for J5/J6 (holding a
  payment needs no new blocking primitive, only a new UI over the existing one).
- **H5 "synchronous HTTP... the answer to the call is the confirmation"**: **CONFIRMED as consistent with the
  existing HTTP Endpoint dispatch model** — today's `SendHttpEndpoint.PostAsync`/`GetAsync`
  (`Jube.Case/SendHttpEndpoint.cs`) already treats the HTTP response as the only signal of outcome, with no separate
  confirmation channel; extending it with an idempotency key and an execution log (as this section specifies) is a
  natural extension of the existing shape, not a new paradigm.
- **H6 step-up**: see A.8 above — `IMfaProvider` is the seam.

### §15 Tagging transactions in the Journal (Figs. 29–31)

- **CONFIRMED + RESOLVED the ⟨verify route⟩ placeholder.** `ArchiveTag` is a real, existing, tested write path
  today: `PUT /api/Invoke/Archive/Tag` (`Jube.App/Endpoints/InvokeEndpoints.cs`, `MapPut("Archive/Tag", TagAsync)`)
  calling `ArchiveService`'s `[ServiceOperation("ArchiveTag", OperationKind.Write)]` method
  (`Jube.Service/Repository/Archive/ArchiveService.cs`). This is already consumed by Exhaustive training and the
  rule backtest as label data, exactly as the doc says ("training is a separate process that reads tags on its own
  schedule"). The tag-configuration lookup itself (the AML typology / Fraud typology groupings shown in Fig. 30's
  menu) — the doc's other ⟨verify route⟩ — is the existing `EntityAnalysisModelTag` area
  (`Jube.App/Endpoints/EntityAnalysisModelTagEndpoints.cs` if present under that name; confirm exact path — this is
  configured per model, not globally, so "the tenant's configured tags" in Fig. 30 needs to resolve per the case's
  model, which the endpoint should already scope correctly since `EntityAnalysisModelTag.EntityAnalysisModelId` is
  the existing foreign key).

### §16 "Open before build"

Every item in this section's own two lists ("to verify against the code" / "to add to the spec") is now answered
somewhere in this Part B or in Part C — cross-referenced rather than repeated:
- Route verification for `ArchiveTag` and tag configuration → resolved above under §15.
- "That archived scores exist on every transaction, before `ArchiveScore` is built (D24)" → **CONFIRMED**: every
  `HttpAdaptation`/`ExhaustiveAdaptation` entry in `EntityAnalysisModelInstanceEntryPayload` already carries a score
  per transaction in `Archive.Json` today; what's missing is a **queryable projection** across transactions (an
  indexed, narrow table), not the underlying scores themselves — so `ArchiveScore` is a materialisation job over
  data that already exists, not a new data-capture requirement. This de-risks D24 meaningfully.
- "The Jube.Charting API for drawing a supplied series (S3-15)" → resolved in A.7: `Chart.Line()`/`Chart.Column()`
  are exactly this, already built on the `accord-analytics` branch.
- CP-18, held-payments section, transaction tags, and the jurisdiction-pack fields (`MaxHold`, `HoldClock`,
  `OnMaxHold`) → correctly flagged as needing addition to the 0.12 spec; the values given for Egypt are correctly
  marked unknown, UK correctly cited as 4 business days (Payment Services (Amendment) Regulations 2024) — I have no
  further correction to this citation.

### §17 Critical review: gaps (G1–G12)

All twelve gaps are real and each is checked against a genuinely absent mechanism (no inbound-hold direction exists,
no recall/return-request operation exists, no Verification-of-Payee field exists anywhere in the payload model, no
reimbursement-clock pair exists, no vulnerability flag exists on the case key panel, no payer-notification record
exists, no "payment system unavailable" banner state exists, step-up is not bound to a specific amount/payee today
because no step-up exists at all yet, bulk tagging and bulk release both correctly don't exist, and G11's timer
accessibility concerns are new UX requirements with no current implementation to check against). **G12 is addressed
in Part C.**

### §18 Pass 4: the remaining screens (Figs. 32–49)

No code-verifiable claims here beyond what's already covered above (this section is UI-only decisions applying Q1–Q9's
answers) — nothing to correct.

### §19 Journeys (J1–J8)

Confirms rather than introduces new claims; each journey composes elements already audited above. One note: **J1's
"flagged for reimbursement workflows [verify]"** is the same UK APP-reimbursement gap as G4, now with a concrete
build ("Op: StopClock, NotifyReceivingPSP, Reimburse") — good, consistent, no correction needed.

---

## Part C — Remaining open items from the PDF itself

### Open question 2 (graph library at the 150-node cap) — DECISION NEEDED

Neither of the two existing renderers is a true force-directed layout at scale (see §6 above): Kendo Diagram is
proven for this exact shape today (the Model Integrity page) but is licensed and being removed elsewhere; `Jube.
Charting`'s `GraphDiagram` is AGPL and consistent with the rest of the design but caps at 150 nodes with no force
layout, and this design's own render cap is also 150 nodes — **so `GraphDiagram`'s cap and this design's cap already
match by coincidence, which is worth exploiting rather than fighting.**
**Recommendation:** use `Jube.Charting`'s `GraphDiagram` for the **Flow** and **Hierarchy** layouts (money tracing,
ownership trees — both are already Sugiyama-layered use cases it's designed for) and for **snapshots** (deterministic
SVG is exactly right for evidence). For the **Force** layout specifically (Fig. 10's clustering view), add a small,
purpose-built client force-directed layout (a from-scratch, dependency-free physics simulation is well within scope
for a bounded 150-node graph — this does not need a third-party graph library at all, only a force-layout algorithm,
which is a self-contained, testable piece of maths). This avoids both "adopt a new AGPL-incompatible dependency" and
"stretch `Jube.Charting` into a shape it wasn't designed for."

### G12 ("Who looked at the case") — DECISION NEEDED

**Recommendation: build it, and place it beside conflict-of-interest, not beside field-level reveal.** Field-level
reveal audit (masked-field unmask events) already exists as a distinct concept in this spec family and is the wrong
model to copy, because it's about *sensitive data*, not *case access*. What G12 is really asking for is closer to
"who opened this case record at all," which the spec's own §12.2 already half-covers for AskJooby ("conversations
started on a case are attached to the case, visible to later reviewers, and audited") but not for a plain human
`Fetch`/`Skim` open. A generic **case-view audit** (case id, user, timestamp, written on every `Fetch`/`Skim`/`Peek`
that is not already covered by an existing `CaseEvent` type) is a small addition to the same hash-chained `CaseEvent`
mechanism already being built for §8 — it does not need its own table. Recommend building it in the same phase as
the `CaseEvent` type-enum/hash-chain work (Appendix B), since it's the same mechanism, one more event type.

---

## Appendix A — Consolidated endpoint & operation changes

One row per distinct call. The **Ref** column keeps every internal code the PDF used (`CP-`, `CH-`, `S1`–`S6-`,
`TC-`, `MF-`, `KP-`, `DC-`, `LB-`, `H-`, `G-`), so this table stays traceable back to the PDF's own tables and
figures. **Status**: Exists (works today, unchanged) / Extend (existing endpoint, new fields or behaviour) / New
(nothing today) / Proposed (named in the PDF but explicitly not yet folded into the spec).

### A1. Cases page, lanes, risk, filters

| Ref | Method & path | Purpose | Status |
|---|---|---|---|
| CP-01 | `GET /api/CaseWorkflowFilter` + `/Counts` | Filter hub cards with counts | Exists + New (counts) |
| CP-02 | `GET /api/CaseSearch?filterId=&sort=lane,priority` | Cases list, extends `GET /api/SessionCaseSearchCompiledSql/ByGuid/{guid}` | Extend |
| CP-03 | (part of CP-02) | Server sort, lane then Priority default | Extend |
| CP-06 | `GET /api/Case/{id}/Summary` | Peek panel | New |
| CP-09 | `POST /api/Case/Skim` | Extends today's Skim (lock + top-of-filter fetch) | Extend |
| CP-10 | Op: `BreakLock` | Break-lock lightbox | New |
| CP-11–14 | Op: `Assign` / `ChangePriority` / `Merge` / `Close` (bulk, with reason) | Bulk bar | New |
| CP-17 | `GET /api/Case/{id}/Risk` | Risk chip hover / popover | New |
| CP-18 | `GET /api/CaseWorkflowFilter/{id}/RiskMatrix` | Risk matrix header counts | Proposed (add to spec) |
| CP-19 | `POST /api/CaseWorkflowFilter/SaveFromSession/{sessionGuid}` | Save a session-modified filter as a personal named filter | **New (this audit, A.15)** |
| — | Op: `AssessRisk` | Analyst's own likelihood override, recorded beside the computed one | New (CH-14) |

### A2. Case page first paint, live state

| Ref | Method & path | Purpose | Status |
|---|---|---|---|
| CH-00 | `GET /api/Case/{id}/Anchor` | Composite first paint: header, case key panel, detail views, Activations, risk summary | New (D18) |
| CH-02 / CH-10 | `CaseHub.Join`, `CaseHub.Presence` | Who else is viewing | New (reuse `WatcherHub`/SignalR pattern, A.2) |
| CH-11 | `CaseHub.CaseRiskChanged`, `CaseHub.CaseChanged` | Live refresh | New |
| CH-03 | `GET /api/Case/{id}/Risk` (same as CP-17) | Risk popover | New |
| — | `GET /api/CaseOperation/Available` | Cockpit: valid operations | New |
| DC-01 | `GET /api/Case/{id}/Checklist`, `GET /api/CaseDiary` | Cockpit: checklist, diaries | New |

### A3. Activations & explainability

| Ref | Method & path | Purpose | Status |
|---|---|---|---|
| S2-01 | `GET /api/Case/{id}/Activations` (inside CH-00) | Anchor, version, archived explanation, score metadata, band, percentile, precision, health, baselines | New |
| S2-04 / MF-01 | `GET /api/ModelFacts` | Model card as at the snapshot | New |
| MF-02 | `GET /api/Snapshot/Diff` | Diff between two snapshots | New |
| S2-07 / S1-08 | `POST /api/ExplanationFeedback` | Flag a contribution / model owner feedback | New |
| S2-08 | `GET /api/Activation/{id}/Sensitivity` | Deterministic replay, Exact tier only | New |
| S2-09 | `GET /api/Activation/{id}/RuleDependencyPath` | Dependency graph + archived values — **reuses `ModelDependencyGraphBuilder` + `ModelGraphBuilder`, already built for Model Integrity** | New (proven pattern) |
| S2-12 | Op: `AddNarrativeFact` | Add to narrative | New |
| KP-04 | `GET /api/Invoke/Sanction` (existing) + `GET /api/SanctionsMatch` | Sanctions search (exists) + match/token/confidence detail (new) | Exists + New |

### A4. Journal, transaction card, graph

| Ref | Method & path | Purpose | Status |
|---|---|---|---|
| S3-01 | `GET /api/Journal` | Rows for a key over a window — extends `GetCaseJournalQuery` | Extend |
| S3-06 / S4-09 | `GET /api/Journal/Explode?by=linkedKey` | 1-hop graph as a table (Explode / list view) | New |
| S3-08/S3-09 | `GET /api/Journal` (pivoted) | Pivot the Journal to another key | Extend |
| S3-10 | `GET /api/Journal/Around` | Same-day activity around a transaction | New |
| S3-14 | `GET /api/Journal/Aggregate` | Aggregation strip (baselines) | New |
| S3-15 | `GET /api/Journal/Series`, `GET /api/Chart/JournalSeries` | Daily chart — **use `Chart.Column()`, `Jube.Charting`** | New (depends on Charting merge) |
| S3-16 | `GET /api/Journal/ScoreHistory` | Sparkline, reads `ArchiveScore` — **use `Chart.Sparkline()`** | New (depends on D24) |
| TC-01 | `GET /api/Archive/{id}/Card` | Transaction card | New |
| TC-05 | `GET /api/Archive/{id}/Payload` | Raw payload, audited | Extend |
| S4-01 | `GET /api/Graph/Neighbourhood` | Network section load | New |
| S4-05 | `GET /api/Graph/Edge/{id}/Transactions` | Edge click → Journal filtered | New |
| S4-07 / S4-08 | Op: `LinkCaseKey`, Op: `BlockIdentifier` | Link to case / block identifier | New |
| — | `POST /api/Graph/Capture` | Save graph capture as Case Upload | New |

### A5. Notes, action codes, events, risk history

| Ref | Method & path | Purpose | Status |
|---|---|---|---|
| S5-03 | `GET /api/CaseWorkflowAction` | Activity select, last-used first | Extend |
| S5-04 | `GET /api/CaseWorkflowActionResult` | Result chips, diary effect | New |
| S5-06 | Op: `RecordAction` | Add coded note; follow-up; hooks via the single dispatcher | New |
| — | `POST /api/CaseWorkflowChecklistItem` (config) | SQL-led checklist predicate (A.15 recommendation) | New |
| S6-02 | `GET /api/CaseEvent` | Typed events, operation ids | Extend |
| S6-03 | `GET /api/CaseEvent/Verify` | Hash-chain verification badge | New |
| S6-05 | `GET /api/Case/{id}/Risk/History` | Every `CaseRiskAssessment` kept | New |

### A6. Operation lightboxes (generic contract + named operations)

| Ref | Method & path | Purpose | Status |
|---|---|---|---|
| — | `POST /api/CaseOperation/{op}/Preview` | Generic preview: consequences, unmet preconditions, defaults, preview hash | New (the foundational contract, build first) |
| — | `POST /api/CaseOperation/{op}/Execute` | Generic execute: idempotent, optimistic-concurrency via `caseVersion`, one transaction | New |
| LB-A | `GET /api/CaseOperation/Approval/{id}`; Op: `Approve`/`Reject` | Maker-checker approver view | New |
| LB-B | Preview with `caseIds[]` | Bulk close | New |
| LB-BL | Op: `BreakLock` | Break lock (typed reason) | New |
| LB-C | (Close operation's rationale templates) | Close lightbox | New |
| LB-F / LB-RA | Op: `RequestApproval`; blocked until approved | File STR | New |
| LB-04 | (Execute carries the step-up token) | Step-up: lightbox proceeds without re-entry | New |
| LB-06 | `POST /api/Authentication/StepUp/Begin`, `/Complete` | Step-up panel | New |

### A7. Payments, holds, step-up

| Ref | Method & path | Purpose | Status |
|---|---|---|---|
| — | `GET /api/Case/{id}/PaymentHolds` | Held payment card | New |
| — | Op: `ReleasePayment` / `RefusePayment` / `ExtendHold` / `RequestRelease` | Payment decision lightbox | New |
| — | `CaseHub.PaymentHoldChanged` | Hold countdown live update | New |
| H6 | `POST /api/Authentication/StepUp/Begin`, `/Complete` | Step-up enrolment (TOTP first, WebAuthn second) — **extend `IMfaProvider`, don't replace it** | New |
| G2 | Op: `RequestRecall` | Payment recall on an already-executed transaction | New |
| G3 | (field on held-payment card + transaction card) | Verification/Confirmation of Payee result | New |
| G4 | Op: `StopClock`, Op: `NotifyReceivingPSP`, Op: `Reimburse` | APP reimbursement clocks (UK) | New |
| G1 | (direction flag on the held-payment card) | Inbound holds | New |
| — | Configuration: `User.PaymentReleaseLimit`; pack `MaxHold`, `HoldClock`, `OnMaxHold` | Payment rules | New |

### A8. Tagging

| Ref | Method & path | Purpose | Status |
|---|---|---|---|
| — | `GET /api/Journal` + `tags[]`; `GET /api/Archive/{id}/Card` + `tags` | Tags column, transaction card | Extend |
| — | Op: `TagTransaction` / `UntagTransaction` | Confirmed route: **`PUT /api/Invoke/Archive/Tag`** (this audit, §15) | Extend (existing write, new operation wrapper) |
| G9 | (multi-select + `TagTransaction` batch) | Bulk tagging in the Journal | New |

### A9. Inbox, presence, team

| Ref | Method & path | Purpose | Status |
|---|---|---|---|
| — | `GET /api/Inbox`, `POST /api/Inbox/{id}/Done` | Inbox drawer | New (reuses existing notification queue, A.2) |
| — | `GET /api/Team/Desk`, `/Wall` | Team view desk/wall | New |
| — | (referral form, tenant-branded) `POST /api/Referral` | Staff referral | New |
| G12 | (new `CaseEvent` type) | Case-view audit ("who looked") | New (same mechanism as the hash chain, this audit's Part C) |

**Background jobs, not called directly by the UI** (named across the PDF, listed once here): the case-risk worker,
`ArchiveScore` projection (Phase 0, D24), the case-risk model registry and backtest, the graph-edge outbox from
`ArchiveKey`, and the single hook dispatcher with its execution log.

---

## Appendix B — Build plan

Ordered as requested: layout → login → generic CRUD pages → then the complex, bespoke pages. Each phase names its
gating dependency so the order is enforceable, not just suggested.

### Phase 0 — Branch consolidation (before any UI work starts)

This design assumes features that exist only on four separate unmerged branches today: `Jube.Charting` and the
model-diagram renderer (`accord-analytics`), the model dependency graph and Model Integrity page
(`service-layer-llm-utilities`, current branch), the invocation-context/history-simulation/backtest services
(`llm-tool-offload`), and all of AskJooby (`ask-jooby-rag-agent-chat-scaffolding`). **None of these can be assumed
present until merged.** This phase is: merge order, conflict resolution, and a single green build with all four
present together — a prerequisite, not a nice-to-have, since Appendix A's endpoints reference all four.

### Phase 1 — Shell and layout

- Vendor Vue 3, daisyUI, Tabulator per A.1's decision (build in CI, vendor the output).
- Global shell: header, theme (`data-theme` light/dark), i18n runtime reading the existing `.resx`/`.fr.resx` pairs
  (A.5), the command palette shell (empty of real commands at this point).
- The generic **hub card** component (A.3) — build it once, against the configuration tree it's replacing
  (`Tree.js`), before any specific configuration area is ported.
- Pick and fix the `colour`/`color` widget-name split (A.4) before the next phase depends on it.

### Phase 2 — Login and authentication journey

- Port Login → forced ChangePassword → RSA MFA challenge into one Vue stepper over the existing three
  `Jube.App/Pages/Account/*` endpoints (A.8) — no new backend needed for this slice.
- Build the `IMfaProvider`-based TOTP provider (H6 phase 1) here, since step-up (Phase 8) and this journey share the
  same enrolment screen.

### Phase 3 — The generic CRUD/grid renderer (DTO-led)

- Build the Vue form renderer against `[FormEndpoint]`/`[FormGroup]`/`[FormField]`/`[VisibleWhen]`-family metadata
  (A.4), targeting the four already-migrated DTOs that between them exercise every marker interface:
  `EntityAnalysisModelDto` (plain CRUD), `EntityAnalysisModelRequestXPathDto` (`ITreeChild`),
  `EntityAnalysisModelInlineScriptDto` (`[Lookup]`), `EntityAnalysisModelGatewayRuleDto` (`IRuleBuilderJson`).
- Build the generic list/grid renderer against `[ListColumn]`, using Tabulator.
- Build the generic role-allocation component (A.12) as a `[Lookup]`-style dual-list widget, replacing
  `RoleAllocation.js` everywhere it's used.
- **This phase has no dependency on Phase 0's branches** and can run in parallel with it.

### Phase 4 — Roll the generic renderer out to every remaining configuration DTO

- Every DTO not yet migrated to the `[FormField]` shape gets migrated (this is mechanical, DTO-by-DTO work, not
  design work) and then simply renders through Phase 3's components — no bespoke page per area.
- Retire `Tree.js` and every per-entity `wwwroot/js/Model/Frame/*.js`/`Administration/Frame/*.js` file as its DTO is
  migrated.
- ACE removal (A.14) happens here for the three files that are pure config CRUD once their DTOs carry
  `code`/`code-editor` widgets; the two bespoke rule-coder/SQL-editor files wait for Phase 7.

### Phase 5 — Backend foundations for the Case page (no new UI yet)

Everything the Case page family depends on, built and tested before any case screen starts, matching the earlier
code-coverage audit's own "Phase 0" recommendation:
1. `CaseActivation`/`CaseAlert` — stop overwriting a case on a second activation.
2. Snapshots (engine + case-configuration, per the earlier code-coverage audit's rewritten §2.4) —
   `Archive.SnapshotId`, `CaseEvent.CaseConfigurationSnapshotId`.
3. `CaseEvent` type enum + per-case hash chain + explicit transactions (closes G12 too, Part C).
4. The single hook dispatcher + execution log, replacing today's five duplicated dispatch paths.
5. **The generic operation contract**: `POST /api/CaseOperation/{op}/Preview` and `/Execute`, `CaseVersion` on
   `Case`, before any single named operation is built.
6. Persist sanctions match detail (KP-04's new half).
7. `ArchiveScore` projection (D24) — a materialisation job over data that already exists (Part B, §16 finding).
8. The case-risk worker and model registry (§4.5.8/4.5.9) — the largest net-new piece, gates CP-17/CH-03/S6-05.
9. `CaseDiary` (typed diaries, replacing the single `Diary`/`DiaryDate` pair).
10. Merge `Jube.Charting` (Phase 0) and reuse `ModelDependencyGraphBuilder`/`ModelGraphBuilder` for the
    activation-rule dependency-path service (S2-09) — proven pattern, cited in Part B §4.

### Phase 6 — Cases page (lanes, risk chips, peek, skim)

Vue port of §2, on top of Phase 5's risk worker and Phase 3's generic components (the filter hub-card sidebar reuses
Phase 3's hub card).

### Phase 7 — Case page: first paint, cockpit, Activations

`CH-00` composite read; case key panel; cockpit with checklist (the SQL-led predicate mechanism, A.15) and diaries;
Activations section with the exact/honest-null waterfall (`Chart.Waterfall()`, needs Phase 0's `Jube.Charting`) and
the rule dependency path (needs Phase 5 item 10).

### Phase 8 — Journal, transaction card, network/graph

Journal grid (extends existing `GetCaseJournalQuery`), transaction card, then Network/Explode — gated on the graph
library decision (Part C) for the Force layout specifically; Flow/Hierarchy can start immediately on `Jube.Charting`.

### Phase 9 — Notes, action codes, events, risk history

Action-code composer with Action Results; system-written notes (needs `CaseAutomationStarter` to gain a note-write
path, Part B §7); Case Events Journal with the hash-chain badge; risk history.

### Phase 10 — Operation lightboxes

Build the lightbox shell component once (Preview → confirm → Execute, with the "what will happen" block and the
precondition "do it now" pattern), then wire each named operation from Appendix A6 through it — Close, File/Approve
(maker-checker), Break lock, Bulk close, in that order (each is a superset of the previous one's UI needs).

### Phase 11 — Payments, holds, step-up

Held payment card, decision lightbox, step-up panel (finishing H6 phase 2, passkeys) — gated on Phase 5's dispatcher
and Phase 10's lightbox shell. G1–G8 fold in here as they're each a variant of the same card/lightbox.

### Phase 12 — Tagging, bulk operations

Tag pill + menu on Journal rows and transaction card (extends the confirmed `PUT /api/Invoke/Archive/Tag` route);
bulk tagging (G9) and the bulk-close/bulk-assign lightboxes from Phase 10.

### Phase 13 — Team view, Inbox, AskJooby panel, Compose

Team view desk/wall; Inbox (reuses the existing notification queue, A.2); AskJooby docked panel — this is genuinely
new UI with no current page to port (A.11) and depends on at least the `researcher` persona plus one or two of the
five named personas being built on the AskJooby branch first; Compose mode and the narrative pipeline.

### Phase 14 — Visualisation and Exhaustive relayout

Port Visualisation to `Chart.*()` builders (**not Radzen**, A.7) and add the remote-web-call datasource kind;
reflow the Exhaustive pages into the case-management-style layout (A.10), carrying forward the diagram-under-sliders
change already made on `accord-analytics`.

### Phase 15 — Hardening

Playwright suite for the complex pages (Case page, Compose, Network); wire CodeQL into `.github/workflows/` (it does
not exist today, A.13); confirm the pen-test/functional test split with an actual `dotnet test --filter
Category=PenTest` run; accessibility pass against G11's timer/screen-reader requirements; mobile "read-and-act" tier
(A.6) for the held-payment lightbox specifically.

---

*End of audit. Cross-reference: `jube_case_management_v2_spec_0.11.md` (the product/technical spec this design
implements), `check-code-coverage.md` (the original code-coverage audit this one extends), `jube-maplibre-map-
component-spec.md` (the separate, already-scoped mapping pre-work referenced but not re-covered here).*
