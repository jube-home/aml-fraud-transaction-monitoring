# Jube v2: AML and fraud transaction monitoring platform

**The design foundation for the whole platform, then what is being built, screen by screen.** Status: build handoff, 4 October 2026.

**Read this first: most of this already exists**

This document looks like a large build. It is not. Jube's backend is modern, complete and in production, and much of it is already in the current interface, just hidden and hard to use. Version 2 is mostly a new face on capability that is already there, and much of the work is mechanical.

Jube challenges billion-dollar vendors by shipping a feature in less time than they take to agree a roadmap. The method is to plan hard and execute easy. Agentic AI shows it: Jube researched it for three years and waited until the conditions were right, chiefly the Microsoft Agent Framework, a flash model that made the project practical, and the working patterns that only settled in 2026. Then it shipped.

This document is the plan hard part. Each piece will be challenged and reworked as it is built, and the final version may look very different, or not. It still matters: the front end is the simple, mechanical part, but the decisions made here last five years or more.

- **Feature complete.** Real-time detection, models and Exhaustive adaptation, rules, sanctions, reprocessing, case management and visualisation all work today. Version 2 does not build a detection platform; it makes the one that exists usable all day.
- **New endpoints are needed, but they are small.** Configuration screens are generated from the existing DTOs. Where a screen needs a call that does not exist yet, it follows the same pattern as every other part of Jube: a minimal API endpoint over the semantic service layer, a repository and the ORM. Most add an endpoint and a service method over data Jube already holds; the interaction tables mark each one Exists, Extend or New.
- **AskJooby is a reality, and it is the future surface of Jube.** The full page and the pop-up are in use today, and agentic AI is in master and open source from 18 October 2026. Jube was a platform first and added agentic AI on top of service layers and an architecture that already existed. That order is the advantage. Agents only became dependable in 2026, through retrieval (RAG) and tightly controlled agent workflows, and only affordable through the new flash models, and every one of them needs a real platform underneath. AI vendors that started without one are now rushing to build a platform, or retreating to managed services. Jube already has the platform.
- **Agentic AI first, and safe.** Jube Holdings owns the copyright in Jube's code, documentation, schemas and artefacts, so Jube can describe the whole platform to an agent: every service-layer method carries a rich, typed description, including methods and RAG resources never exposed as an endpoint. Anything a person can do in Jube, an agent can do through that same layer, with the same permissions, audit trail and maker-checker. That description is itself part of the work. An index built by chunking and embedding Jube's service layer, schemas or documentation carries Jube's structure with it, and an agent or interface served from it over a network falls within the AGPLv3, wherever it runs and whatever API sits in between.
- **What is genuinely new is concentrated:** the case workspace, held payments and recovery, claims, assurance and similar cases.

The hard part was built first. What remains is to let people see it.

# Part I · The platform

What Jube is, the decisions that shape every screen, the AI layer, and the order of the build.

## 1. The platform and its design foundation

### 1.1 What Jube is, and what version 2 changes

Jube is an open-source, real-time anti-money-laundering and fraud transaction monitoring platform. Every transaction passes through a model: request XPath extraction, inline functions and scripts, lists and dictionaries, gateway rules, TTL counters, abstraction rules and calculations, HTTP adaptations (external machine learning), and activation rules that can elevate the response, notify, watch or open a case. Around that engine sit case management, reprocessing, Exhaustive adaptation (Jube's own model training), sanctions screening, visualisations, administration, multi-tenancy and preservation.

**Version 2 rebuilds the interface to all of it on one design foundation.** Moving every configuration screen is largely mechanical, because each one becomes a form and a grid generated from its existing DTO. It is still high-impact: the whole product changes stack, security model, live-update model and governance at once. Case work, payments and claims are redesigned outright (sections 6 to 19). The decisions below apply everywhere and come before any screen.

**AI-first, on its own platform.** Jube is moving to an AI-first stance. AskJooby is present on every page and inside case work, and its agents (open source in master from 18 October 2026) investigate, summarise and draft changes over the same models, archive and network that make the decisions. Most AI-first case tools sit on someone else's detection; Jube owns both layers (section 2).

### 1.2 Design decisions for the whole platform

**Application**
- **One Vue 3 single-page application** replaces every Razor page. The server serves the app for every non-API path; Vue Router owns navigation. The new interface replaces the current one in a single release, never page by page.
- **Built in CI, vendored in the repository.** Components are compiled with Vite and the output is checked in and served by `Jube.App`, so the product still installs and runs without Node, and no template is compiled in the browser.
- **Types come from the C# DTOs,** generated from the OpenAPI document on every build. No API type is written by hand.
- **One component library:** daisyUI 5 (light and dark themes, comfortable and compact density), Tabulator for every grid, Lucide icons, `Jube.Charting` server-rendered SVG for every chart. Kendo, ACE and Radzen are removed. jQuery QueryBuilder stays, isolated in one rule-builder component; Jube's own lightweight coder replaces ACE for rule, SQL and JSON editing.
- **Two screen tiers:** the desk (1280 px and wider) has everything; smaller screens get read-and-act only (summary, activations, held payments, Inbox, approvals).

**Configuration: one generic renderer**
- **Every configuration object is described once, on its DTO,** with form-field and list-column attributes. A schema endpoint serves that description, and one form renderer and one grid renderer draw every model, case workflow, administration and visualisation screen from it. Adding a field to a DTO adds it to the screen.
- **Hub cards replace trees** for navigating parent and child objects, with breadcrumbs and counts.
- **Model governance:** lists and dictionaries become model artefacts, changed through versioned change sets with a diff, a backtest and approval, and recorded in the model snapshot. Suppression is the only override, always time-bound and recorded on every match it affects (section 21).
- **Legal limits are data:** hold ceilings, recall windows and reimbursement rules are versioned tables per market and scheme, published with maker-checker (section 22).

**Data and live updates**
- **TanStack Query holds all server data;** Pinia holds only client state.
- **The existing `ServiceChangeHub` is the single live channel.** Every service-layer write already publishes a change; the client uses it only to mark cached data stale, which then refetches. No state is copied over the socket. A small new `CaseHub` carries presence only. Money-critical views also poll every 30 seconds as a safety net.
- **An update never overwrites someone's edit:** they see "Changed by X · Reload" instead.

**Platform engineering**
- **Tests:** xUnit coverage of the service layer; an integration suite that runs real invocations end to end through model execution; component tests for the Vue screens and Playwright journey tests.
- **Master is the release:** changes merge to master continuously; the new interface is the exception and replaces the current one in a single release (section 3).

**Security**
- **The session token lives in a tab-scoped secure store** and is sent as an `Authorization: Bearer` header. Cookies are removed from the authentication pipeline, so there is no CSRF surface; a new tab signs in again.
- **Strict content security policy:** no `unsafe-eval`, no inline script, Trusted Types, and no HTML from data (`v-html` is banned). Same origin only.
- **Step-up re-authentication** for irreversible actions (file, approve, break lock, release, refuse, reimburse): an authenticator code first, then passkeys; no SMS. For payment decisions, the step-up is bound to the exact decision previewed.
- **Audit by construction:** every action is a previewed operation that writes a case event to a hash-chained journal; views of cases are recorded too.

**Case and action foundations**
- **Every action is Preview then Execute:** the server states the consequences before anything happens, and executes exactly what was previewed.
- **Every clock is a diary:** SLAs, payment holds, recall answers and claim deadlines sort the work, escalate and appear on the Team view the same way.
- **External calls are honest:** a call that leaves Jube shows its steps and says "Outcome unknown" rather than guessing; retries reuse the same idempotency key.

**AI**
- **AskJooby is part of every screen:** a full chat page and a corner pop-up with page context, both existing today; Bites (short grounded narratives) in case work; agents that investigate with read-only tools.
- **It assists and cites; it never decides.** It uses the asking user's permissions, logs every tool call and links every claim to a fact. Generated narratives are stateless calls built by the server from a registered, versioned frame.
- **Changes go through snapshots and maker-checker:** an agent drafts a change set on a draft model snapshot with a backtest; a person submits it and a second person approves it with step-up. Agents never publish, never create a suppression and never choose a claim exception.

**Language and accessibility**
- **Localisation through locale files in the Vue project,** English as the source; one language per interface, with Latin transliteration beside Arabic names. Tenant-configured labels carry their own translations.
- **WCAG 2.2 AA:** keyboard first (every action has a key and the command palette), timers that warn before they expire, nothing signalled by colour alone.

### 1.3 Moving the whole product

| Area today | In version 2 | Kind of change |
|---|---|---|
| Model configuration: request XPath, inline functions and scripts, gateway rules, tags, TTL counters, abstraction rules and calculations, HTTP adaptations, activation rules, watcher, notifications, sampling | Generic forms and grids from the DTOs; rule builder and Jube's coder for rules and scripts | Mechanical |
| Lists and dictionaries | Versioned change sets with diff, backtest and approval | Behaviour changes |
| Model integrity and backtest | Same functions on Tabulator grids and `Jube.Charting` charts | Mechanical |
| Case workflow configuration: workflows, statuses, filters, XPath, forms, actions | Generic forms and grids | Mechanical |
| Case workflow displays and macros | Displays become components; macros become sequences of previewed operations | Behaviour changes |
| Case work: skim, lock and next, journals, notes, files, automation | Redesigned: sections 6 to 15 and 20 | Redesigned |
| Payments and claims | New: sections 16 to 18 | New |
| Reprocessing | Generic forms and grids; progress through live updates | Mechanical |
| Exhaustive adaptation | Training configuration on generic forms; variable statistics and model performance on `Jube.Charting`; relayout in the last stage | Mechanical, then relayout |
| Sanctions: loader, searching, model invocation | Generic grid and search page; match detail persisted and shown with the activation | Mechanical, plus match detail |
| Visualisations: registry, parameters, datasources | Generic forms; SQL in Jube's coder; templated chart definitions with parameters and series instead of raw JSON; a remote web call as a datasource beside SQL; charts and maps as server SVG; HTML output shown in a sandboxed frame, never injected into the page | Mechanical, plus sandboxing |
| Administration: users, roles, permissions, performance counters | Generic forms; users gain a release limit and step-up enrolment | Mechanical, plus new fields |
| Authentication and multi-tenancy | New sign-in journey with step-up; bearer token; tenant switcher in the shell | Redesigned |
| Preservation (export and import) | Generic page | Mechanical |

## 2. AskJooby: the AI layer

Jube is AI-first. AskJooby is on every page and inside the case work itself, and unlike AI-first case tools it sits on Jube's own detection platform: the models, rules, archive, network and audit trail it reasons over are the same ones that scored the transaction. Agentic AI is a delivered milestone: it is in the master branch and fully available as open source from 18 October 2026.

Four rules hold everywhere: AskJooby uses the asking user's permissions and nothing more; every tool call is logged; every claim cites its source; and it never commits anything. Changes come back as proposals that people submit and approve.

### 2.1 Two surfaces, one conversation history

![Full page: conversations, search, personas and an agent run](img/60-askjooby-full-page.png)
![Corner pop-up with page context](img/61-askjooby-popup.png)

- **The full page** (exists today, built on DeepChat) is for longer work: a conversation list with search, pinned chats, personas, an agent run shown step by step, tables, sources and Finding cards. Context can be added with @ (a case, model, rule or list).
- **The corner pop-up** (exists today) is on every page. It starts or continues the same conversations, with the page the user is looking at as context. "What Jooby sees" lists exactly what was sent; masked fields and raw payloads are never sent. "Show on page" highlights the rows an answer refers to, and a conversation opens in the full page with one click.
- **On the case page,** AskJooby also takes the Decide column's place (Alt+J), with the suggested next step kept in its header (section 13.3).
- **Answers come in three kinds:** a **Finding** (with evidence, can be added to the case as a fact or opens the matching lightbox), an **Opinion** (no evidence, can be challenged or dismissed), and a **Proposal** (a draft change, section 2.4).

### 2.2 Personas and agents

- **Researcher** (exists): answers questions over the platform's data and documentation.
- **Analyst Agent:** pre-investigates cases with read-only tools and recommends a disposition (agent triage, section 20.1). Built first.
- **Rule Writer:** drafts rule and list changes, uses the rule parser and Model Integrity's dependency tools, and backtests before proposing. Built second.
- **Later personas:** **Data Analyst** (explains query and chart output, model statistics and Exhaustive results) and **DevOps Support** (reads cluster, PostgreSQL and Redis health and logs) are read-only. **General Administrator** guides entity, tenant, user and role setup, and its changes go through maker-checker like the Rule Writer's.
- **Agent mode** in the composer lets a persona run tools on its own; each run lists its steps, inputs and timings and has a run number for audit.

### 2.3 Bites

![Bites in the Cases page peek, the case page, an activation and the Journal](img/62-askjooby-bites.png)

A Bite is a one- or two-sentence narrative dropped into case work, outside chat: in the Cases page peek, at the top of the case page ("since you last looked"), beside an activation's waterfall, and over the Journal's focus window.
- **One framed call each** (section 2.5), cached until the facts behind it change.
- **Every claim links to a fact.** With no supporting fact, no Bite is shown.
- **Never more than two sentences,** labelled "Bite", with the frame, version and facts on hover, "Expand in AskJooby" and "Not useful".
- **Never written into a note or report** unless someone adds it.

### 2.4 Proposals through model snapshots and maker-checker

![An agent proposal and the approver's review](img/63-askjooby-proposal.png)

When a persona proposes a change to a model (a rule, list, dictionary or threshold), it writes a **draft change set on a draft snapshot** of the model, never to the live model.
- The proposal shows the diff, the snapshot it starts from, and a **backtest** (cases created, confirmed fraud kept, period), with the cases it would have missed one click away.
- **A person submits it;** a **second person approves** it with step-up, and nobody can approve their own change. The change set records that it was drafted by an agent and for whom.
- Publishing creates the next snapshot version; rolling back publishes the previous snapshot.
- The same path serves list and dictionary changes (section 21.5) and rule tuning from outcomes (section 20.6).

### 2.5 Generated narratives use framed context, not chat

Every narrative the backend writes is a **direct AskJooby call with a framed context**. That covers the case summary, Compose section drafts, the agent triage summary, the suggested close rationale, report narratives, and activation or journey explanations. Chat context is never used.

- **The server builds the context:** from a registered, versioned **frame** (for example `CaseSummary`, `NarrativeSection`, `CloseRationale`, `ActivationExplain`) and the subject's facts. The browser names only the frame and the subject, never prompt text.
- **Stateless:** no chat history, and nothing said in the AskJooby panel. To use a chat answer, the analyst accepts its Finding card as a fact, and then it is part of the frame like any other fact.
- **Grounded:** every sentence cites the facts it came from. A sentence with no fact is marked an inference (amber) until a person accepts it. Grounds for suspicion stays human-written; Jooby only offers framings.
- **Auditable and reproducible:** each draft records the frame version, a hash of the context, the model and the output, and writes a case event. The same frame version and context can be re-run to compare.
- **Never overwrites a person:** when the facts change, the draft is marked "facts changed since this was drafted · Regenerate · Keep". Nothing is regenerated automatically over edited text.
- **Visible:** each generated text carries "Drafted by Jooby · frame and version · from N facts".
- **Model-supplied text:** the HTTP Adaptation `Narrative` and `HumanLabel` come from the model endpoint's curated templates. They are shown as-is, labelled "from the model", and are not AskJooby output.

| Interaction | Call | Status |
|---|---|---|
| Draft or regenerate any narrative | CG-37 `POST /api/AskJooby/Narrate` `{ frame, subject, params }` | New |
| Frame catalogue | CG-38 `GET /api/AskJooby/Frame` | New |
| Provenance popover | CG-39 `GET /api/AskJooby/Run/{id}` | New |

### 2.6 Similar cases

**Why:** the Network links cases that share an account, device or payee. Many laundering patterns share none: the same shape of activity recurs across different customers, months apart. Similar cases finds those by comparing cases as a whole, across time, entity and typology. It matters most for layering and integration, where each case alone looks ordinary.

**What a case is compared on.** Each case keeps two fingerprints, refreshed when its facts change:
- **Story:** the text of the case (notes, action codes, tags, close rationale, report narrative and the activations' rule names), held both as an embedding and as a full-text index. Masked fields are left out.
- **Behaviour:** a fixed set of features computed from the Journal, never by a language model: amounts and their spread, velocity, fan-in and fan-out, time from money in to money out, number of hops, new payees, channels and countries. It is normalised so no single feature dominates.

**How it is found.** Both live in PostgreSQL next to the cases (pgvector with an HNSW index, plus a GIN full-text index), so a search runs in the same database and in real time.
- **Hybrid retrieval:** a meaning search on the embedding and a word search on the full text run side by side, and their two rankings are merged by reciprocal rank fusion. Word search catches exact references (a payee name, a rule, an IBAN fragment) that meaning search misses; meaning search catches the same story told in different words.
- **Filters first:** tenant, the user's case permissions and the period are applied inside the search, so results are never thinned out afterwards. Cases the user cannot open are left out entirely, not counted.
- **Look alike by:** Story, Behaviour, or Both (the default).

**Every match explains itself.** A score alone is not evidence.
- Each match shows its outcome (Confirmed fraud, Report filed, False positive, Open), the score as "Similarity 0.84, not a probability", and **why**: the three behaviour features that match most closely ("9–12 cash deposits under EGP 10,000 in 48 h; out to a new payee within 2 h") and the shared words highlighted.
- **Compare** opens the two cases side by side: timelines aligned at the first activation, behaviour features in one table, outcomes and the rules that fired in each.
- Similarity is advisory. It never scores a transaction, opens or closes a case, or appears in a report unless a person adds it: **Add as fact** records "Similar to C-8120 (confirmed fraud): fast pass-through of cash deposits" with the comparison attached; **Link** uses the normal link operation.

**Where it appears**
- **Case page:** a Similar cases tab in the investigation drawers, with the ten closest matches, outcome counts at the top ("10 similar · 6 confirmed fraud · 3 false positive") and the controls above.
- **Cases page peek:** one line when strong matches exist ("3 similar cases, 2 confirmed fraud").
- **Patterns no rule covers:** a Patterns view under rule tuning (section 20.6) groups recent confirmed cases into clusters by behaviour and shows, for each, how many were caught by which rules. A cluster that mostly came from manual referrals or late detection offers **Draft a rule with Rule Writer**, which follows the proposal path (section 2.4) with a backtest.
- **AskJooby:** the Analyst Agent has a read-only `FindSimilarCases` tool and cites matches as Findings; a similar case's outcome reaches a narrative only once it has been added as a fact.

**Shared with AskJooby retrieval.** The same PostgreSQL store and the same hybrid search serve AskJooby's retrieval: chunked documentation, the source code tree, generalised example rules, and the descriptions on service-layer methods, which are used to route a request to the right tool.

**Guardrails**
- Embeddings are computed inside the deployment; no case text leaves it.
- Each vector records its embedding model and version. Changing the model re-embeds into a new column in the background and switches over only when complete, so two models' vectors are never compared.
- Every similarity search made from a case is written to the read audit, with the matches shown.

| Interaction | Does | Call | Status |
|---|---|---|---|
| Similar cases tab | Ten closest cases with outcomes and reasons | CG-54 `GET /api/Case/{id}/Similar?by=story,behaviour,both&period=` | **New** |
| Compare | Side-by-side timelines, features and outcomes | CG-55 `GET /api/Case/{id}/Similar/{otherId}` | **New** |
| Add as fact | Records the match and comparison as a case fact | CG-56 `POST /api/Case/{id}/Fact { kind: "Similar", caseId }` (Preview/Execute) | **New** |
| Link | Links the two cases | Existing Link operation | Exists |
| Patterns view | Clusters of confirmed cases with rule coverage | CG-57 `GET /api/RuleTuning/Patterns?period=` | **New** |
| Re-embed | Background re-embedding after a model change (administrators) | CG-58 `POST /api/Similarity/Reindex` | **New** |

---

### 2.7 Interactions

| Interaction | Does | Call | Status |
|---|---|---|---|
| Full page and pop-up chat, chat search | Conversations, history and search | Existing AskJooby chat endpoints (DeepChat) | Exists |
| Page context | Sends the current page's visible context with the question | Existing chat endpoint, with a `pageContext` object | **Extend** |
| Agent run | Runs a persona's read-only tools and logs each call | Agent endpoints in master from 18 Oct 2026; `AskJoobyRun` log | Exists (from 18 Oct) |
| Bite | One framed narrative for a subject, cached by facts hash | CG-52 `POST /api/AskJooby/Bite { frame, subject }` | **New** |
| Proposal | Creates a draft change set on a draft snapshot, with backtest | CG-53 `POST /api/AskJooby/Proposal` → existing change-set and backtest endpoints | **New** |

## 3. What is being built and in what order

**The parts of the system**

| Area | What people can do | Sections |
|---|---|---|
| Ask and automate with AI | Chat on every page with page context, agents that investigate and draft changes, Bites in case work, similar cases | 2 |
| Configure detection | Build and govern models, rules, lists, adaptations and case workflows on one generic set of screens | 1, 22 |
| Find the work | See cases ordered by urgency (live money first, then deadlines, then risk), preview without opening, supervise the team | 6, 7 |
| Understand the alert | Read why each model and rule fired, with exact contributions and baselines; follow the money in the Journal and the Network | 8–12 |
| Record and write | Coded notes, drafts grounded in facts, AskJooby as an assistant that cites its sources | 13 |
| Act | Close, escalate, file, link, break a lock, with a preview of every consequence | 15 |
| Stop and recover money | Release, refuse or extend held payments with step-up; hold inbound payments to mule accounts; recall payments that have left; use payee name checks as evidence | 16, 17 |
| Reimburse | Run APP scam claims against their legal clocks; adapt to customers who need support | 18 |
| Report | File reports with approval; take referrals from staff | 19 |
| Assure | Audit trail with a hash chain, read audit, QA sampling, outcome monitoring | 14, 20 |
| Improve and control | Tune rules from outcomes, automation recipes, agent triage, time-bound suppressions, versioned lists | 20, 21 |

**Build order**

Each stage is usable on its own and depends only on the stages above it. Detailed phases are in the Vue blueprint (`SKILL.md`, section 4).

| Stage | What it delivers | Needs |
|---|---|---|
| 1 · Foundation | The application shell, sign-in with step-up, live updates, localisation, and the generic renderer with every configuration screen moved onto it (models, case workflows, reprocessing, sanctions, visualisations, administration) | — |
| 2 · Case engine | No new screens: previewed operations, the case event chain, diaries, the case risk worker and archived scores | 1 |
| 3 · Investigate | Cases page, case page, activations and explanations, Journal with tagging, Network, drawers, notes, case history and read audit | 1, 3 |
| 4 · Act | All operation lightboxes, maker-checker, held payments with step-up, inbound holds, recall and payee checks | 5 |
| 5 · Supervise and assist | Team view, Inbox, AskJooby, Compose and generated narratives | 5 |
| 6 · Assure | QA sampling, requests for information to the customer, suppressions and versioned lists | 6 |
| 7 · Claims | APP reimbursement claims and support needs | 6, 8 |
| 8 · Automate and tune | Agent triage, rule tuning from outcomes, automation recipes, web research, similar cases and patterns | 7, 8 |
| 9 · Finish | Visualisation and Exhaustive relayout, then hardening: journey tests, accessibility, performance budgets | all |

The new interface replaces the current one in a single release once stages 1 to 4 pass their gates; until then the current interface stays in production unchanged.

**How to read this document**
- Each screen has a figure and an interaction table: what each click, key or search does, the call it makes, and its status: **Exists** (call as is), **Extend** (existing endpoint, new fields), **New**, or **Client** (no call; state in the URL).
- Interaction ids (GH-, CP-, CH-, KP-, S1–S6-, DC-, LB-, TC-, MF-) refer to section 22 of the product specification (`jube_case_management_v2_spec_0.11.md`); `CG-` ids refer to the endpoint list in section 23.
- Figures are captured from the HTML prototype in the light theme. They are design references, not production code. Names, amounts, references and limits in them (C-9041, ACC-02 Alpha LLC, EGP 25,000) are example data.
- Inbound holds, recall, payee checks, claims, support needs, bulk tagging and the read audit are specified in text and not yet drawn.
- **Stack:** Vue 3 single-page application, daisyUI 5 (light and dark, two densities), Tabulator grids, Lucide icons, `Jube.Charting`, SignalR for live updates.

---

# Part II · How people work

The workspace in one page, then eight journeys that walk through it.

## 4. The workspace at a glance

Before the detail, the shape of the place. A user spends the day in two screens and a few overlays; everything in Part III sits inside one of them.

![Cases page](img/01-cases-page.png)
![Case page](img/04-case-page.png)

- **Cases page:** filter cards on the left, the case list in the middle ordered by lane then Priority, and a peek drawer on the right (Space). Section 6.
- **Case page:** a header with lane, risk and lock; the case key panel on the left; six sections in the main column (Summary, Activations, Journal, Network, Notes, Case history); and the Decide column on the right with the suggested next step, diaries and AskJooby. Sections 8 to 14.
- **Drawers** open beside the page for a transaction, its raw payload or the model's facts. Section 12.
- **Lightboxes** carry every action: preview, step-up when needed, then the result. Sections 15 to 18.
- **AskJooby** is the corner pop-up on every page and a tab in the Decide column; Bites appear in place. Section 2.
- **Team view and Inbox** serve supervisors and the MLRO; the **command palette** (Ctrl+K) works everywhere. Section 7.
- **Configuration** screens are generated from the DTOs. Section 22.

## 5. Journeys

Each journey is a path through the screens. **↓** marks what the user does to move to the next state, and **↳** marks a branch.

### J1. Triage and open a case (analyst)

![1 · Cases page: lanes first, then Priority](img/01-cases-page.png)
↓ **Space** on a row: read-only, no lock taken
![2 · Peek](img/03-peek.png)
↓ **Enter**: opens the case and takes the lock (or **W** Skim takes the top case)
![3 · Case page, first paint from one Anchor read](img/04-case-page.png)
↓ **R** or hover the risk chip
![4 · Case risk popover: why this case, and why now](img/05-case-risk-popover.png)
↳ Disagree? Record your own assessment (Op: AssessRisk). It sits beside the computed one and never replaces it.

### J2. Investigate: from explanation to evidence

![1 · Activations: what fired and why](img/06-activation-waterfall.png)
↓ **Journal ▸** on a driver chip
![2 · Journal on the case subject, anchors pinned](img/08-journal.png)
↓ Click **DEV-8831A** → **Pivot**
![3 · Journal pivoted: "Not the case subject"](img/09-journal-pivot.png)
↓ **4**: Network; click an edge to filter the Journal to its transactions
![4 · Network with shared selection](img/10-network.png)
↓ **Enter** on a Journal row
![5 · Transaction card: what Jube knew then](img/14-transaction-card.png)
↓ **+ Tag**
![6 · Tag the transaction (ArchiveTag, undo 10 s)](img/30-tag-menu.png)
↳ **Alt+←** or the breadcrumb returns to the case subject at any point.

### J3. Contact the customer, then close as false positive

![1 · Note with an action code and result; follow-up diary set](img/12-composer.png)
↓ **D**
![2 · Close lightbox: preview of consequences](img/17-close-lightbox.png)
↓ Choose outcome and typology, then pick or write the rationale
![3 · Ready to close](img/18-close-ready.png)
↓ **Ctrl+Enter**: closed, with an undo toast (hooks wait for it); **⇧J** goes to the next case
↳ The case changed while the lightbox was open? The lightbox shows the change inline and waits for your acknowledgement (8.5).

### J4. Write and file an STR with approval

![1 · Compose: facts into the narrative, scorecard to green](img/37-C1-compose-scorecard.png)
↓ **Ctrl+S** saves the version, then **F**
![2 · File STR: blocked until approved](img/19-file-str.png)
↓ **Request approval**: the MLRO gets an Inbox item (email if away)
![3 · MLRO Inbox](img/40-D2-inbox.png)
↓ **Review**
![4 · Approver’s view](img/20-approver.png)
↓ **Approve** → step-up
![5 · Step-up inside the lightbox, nothing lost](img/25-step-up-code.png)
↓ The maker files (**Ctrl+Enter**)
![6 · Report status: Submitted → Acknowledged, or Rejected → v2](img/46-F2-report-status.png)
↳ Rejected: the FIU's message is shown against the field, the resubmission diary starts, and a new version is created, never an edit.

### J5. Decide a held payment

![1 · C-9044 sorts first: Live money](img/01-cases-page.png)
↓ **Space**
![2 · Held payment card: time left, what happens if nobody decides](img/22-held-payment-card.png)
↓ **H**
![3 · Decision lightbox: 1 Release · 2 Refuse · 3 Extend](img/23-payment-lightbox.png)
↓ Pick a decision and enter the reason, then **Ctrl+Enter** (Release and Refuse need step-up)
![4 · Step-up](img/25-step-up-code.png)
↓ **Verify**
![5 · Sending to core banking](img/26-payment-sending.png)
↓ Synchronous answer
![6a · Confirmed: diary stops, lane → Standard](img/27-payment-confirmed.png)
![6b · Outcome unknown: Retry safely with the same key](img/28-payment-timeout.png)
↳ **Above your limit:** Release becomes Request release, and a second person approves it:
![Approver’s view of Request release](img/48-F4-approve-release.png)
↳ **Nobody decides:** one hour before expiry the status becomes Hold expiring, and the supervisor is escalated by email and Inbox:
![Hold expiring email](img/47-F3-hold-email.png)

### J6. Supervise the team

![1 · Team view: people, filters, next to breach, risk matrix](img/43-E1-team-view.png)
↓ Select R. Lund's cases → **Reassign selected**
↳ A case is locked by someone who is away → **Break lock** (typed reason, step-up):
![Break lock](img/21-break-lock.png)
↳ On the wall screen, the same data at a glance:
![Wall screen](img/44-E2-team-wall.png)

### J7. A staff referral reaches the MLRO

![1 · Referral form, then receipt with the time of disclosure](img/45-F1-referral.png)
↓ **Send to MLRO**: a referral case is created with the MLRO's regulatory diary
![2 · MLRO Inbox: "Referral submitted"](img/40-D2-inbox.png)
↓ **Open** → link it to an existing case, open an investigation, or close it with a reason
![3 · Case page (the referrer is visible only to the referral workflow)](img/04-case-page.png)

### J8. First-time step-up

![1 · Enrol an authenticator, store recovery codes](img/49-F5-step-up-enrolment.png)
↓ The first protected operation (File, Approve, Break lock, Release, Refuse)
![2 · Step-up inside the lightbox; valid 5 min (payment decisions: bound to that decision)](img/25-step-up-code.png)

---

---

# Part III · The screens in detail

Every screen in the order a user meets it. Each section opens with what it is for, then figures and an interaction table: what each click, key or search does, the call it makes, and its status.

## 6. Finding work: the Cases page

Where every working day starts: the cases a user can work, ordered so that live money and deadlines come before risk, with a preview that saves opening each case.

### 6.1 List, lanes and risk chips

![Cases page](img/01-cases-page.png)

Filter hub cards on the left; the Tabulator list ordered by **lane, then Priority**. C-9044 is first (⚡ Live money: a payment hold decides in 6 h), then C-9047 (◷ Deadline near), then the Standard lane by Priority. The "Why" column shows the top case-level driver, with the AskJooby-prepared marker on C-9041.

| Interaction | Does | Call | Status |
|---|---|---|---|
| Page load | Filter cards with counts and meta line | `GET /api/CaseWorkflowFilter` + `/Counts` (CP-01) | Exists + New |
| Click a filter card | Load its cases, with lane, likelihood, band, impact, Priority, Why, AskJooby flag, due diary, lock, value | `GET /api/CaseSearch?filterId=&sort=lane,priority` (CP-02) | **Extend** |
| Hover a risk chip | Lane, likelihood, impact, urgency, Priority formula | `GET /api/Case/{id}/Risk` (CP-17) | **New** |
| Sort select | Server sort (lane then Priority by default) | CP-03 | Extend |
| `W` Skim | Lock the top case in lane order, assign if unassigned, prefetch next | `POST /api/Case/Skim` (CP-09) | Extend |
| Lock "Break…" | Break lock lightbox | Op: BreakLock (CP-10) | **New** |
| Bulk bar | Assign · Priority (override) · Merge · Close with reason | Op: Assign / ChangePriority / Merge / Close (CP-11–14) | **New** |

### 6.2 Risk matrix

![Risk matrix](img/02-risk-matrix.png)

Likelihood (rows) × impact (columns) for the loaded cases; the likely-and-high cells are tinted. On the Team view it covers the whole filter.

| Interaction | Does | Call | Status |
|---|---|---|---|
| "Risk matrix ▾" | Counts per likelihood band × impact band for the filter | Proposed: `GET /api/CaseWorkflowFilter/{id}/RiskMatrix` (not yet in 22; proposed CP-18) | **New** |

### 6.3 Peek (`Space`)

![Peek](img/03-peek.png)

Read-only drawer without taking the lock. For C-9044 it shows the data-quality warning: two inputs were missing and filled with defaults.

| Interaction | Does | Call | Status |
|---|---|---|---|
| `Space` | Summary, lane and risk chip, Activations, last note, diaries, health, lock | `GET /api/Case/{id}/Summary` (CP-06) | **New** |
| Open case (`Enter`) | Open without locking (Fetch) | CH-00 | New |

---

---

## 7. Supervising: Team view, Inbox and the command palette

What supervisors and the MLRO use to see the whole team's work and the items waiting for them, and what everyone uses to move anywhere by keyboard.

### 7.1 Team view: desk and wall

![Team view desk](img/43-E1-team-view.png)
![Team view wall screen](img/44-E2-team-wall.png)

- **Desk view:**
  - per-analyst table with availability;
  - absence offer ("R. Lund is out: 2 cases due");
  - per-filter panel with the next case to breach;
  - unapproved emergency changes;
  - the risk matrix (click a cell to open those cases as a filter).
- **Wall screen:** large type, refreshed every 60 s, no interaction and no customer names.

### 7.2 Inbox

![Inbox drawer](img/40-D2-inbox.png)

An item is marked done when it's acted on, not when it's read. Anyone who is away gets it by email instead (F3).

### 7.3 Command palette and shortcut sheet

![Command palette and shortcut sheet](img/39-D1-command-palette.png)

The palette lists operations on the current case first; greyed items say why they're unavailable. The shortcut sheet (`?`) shows only the current scope.

---

## 8. The case page

One page per case. A header, the case key panel and the Decide column frame six sections; the first paint shows enough to decide most cases without scrolling.

### 8.1 First paint: header, case key panel, Case detail views, cockpit

![Case page](img/04-case-page.png)

The header carries the risk chip `Standard · Likely 56% · High · 62`, the STR diary (4d 03h, business days, EG), the lock and presence (M. Karim also viewing). The case key panel shows provenance under each value. Case detail views answer first, now including the **Ranked** line, then the case-level drivers.

| Interaction | Does | Call | Status |
|---|---|---|---|
| Open the case | One payload for the first paint: header, case key panel, detail views, Activations, risk summary (< 300 ms) | `GET /api/Case/{id}/Anchor` (CH-00) | **New** |
| Presence | Join and receive who else is viewing | `CaseHub.Join`, `Presence` (CH-02, CH-10) | **New** |
| Live refresh | Risk changed, case changed | `CaseHub.CaseRiskChanged`, `CaseChanged` (CH-11) | **New** |
| Identifier DEV-8831A | Pivot the Journal to the device | S3-08 → `GET /api/Journal` | Extend |
| Identifier counts (4, 4,112) | Shared-identifier counts | `GET /api/Graph/KeyCounts` (KP-06) | **New** |
| Live KYC "3 min ago" | Live lookup at case open through the dispatcher | inside KP-01 (KP-02) | **New** |
| Ranked line | One sentence from the case risk | part of CH-00 (S1-09) | **New** |
| Driver chips / Journal ▸ / ⚑ | Jump to the Activation; filter the Journal; feedback | Client; S1-07; `POST /api/ExplanationFeedback` (S1-08) | Client / New |
| Cockpit load | Valid operations, checklist, diaries | `GET /api/CaseOperation/Available`, `/Case/{id}/Checklist`, `/CaseDiary` (DC-01) | **New** |

### 8.2 Case risk popover (`R`)

![Case risk popover](img/05-case-risk-popover.png)

Likelihood 56% (Likely, cold start, base rate 8%). Contributions after κ 0.7 sum to +2.70: StructNet +1.15, network +0.83, customer tier +0.44, Structuring 9d +0.20 (shares inputs, counted at 30%), sanctions +0.09. Impact High (4). Urgency: Standard lane, moves to Deadline near at 2 d. Priority 62 = ln(1 + 0.56 × 30) scaled.

| Interaction | Does | Call | Status |
|---|---|---|---|
| Hover or `R` | The full case risk | `GET /api/Case/{id}/Risk` (CH-03) | **New** |
| Your assessment: Low / Medium / High | Recorded beside the computed likelihood, with undo | Op: AssessRisk (CH-14) | **New** |

---

### 8.3 Case header: lanes, risk method, Priority override

![Case header variants](img/32-A1-case-header.png)

- **Live money** (lane 1) puts the hold countdown in the header, with Decide payment (`H`).
- **Mandatory** (lane 2) states which minimum-lane rule put it there. Neither its lane nor its impact can drop.
- **Cold start vs calibrated:** the risk chip carries its method. Once calibrated, it adds a reliability line ("of cases called Likely, 58% were TP").
- **Priority override:** the computed value is struck through, and the chip shows who set the override, why, and when it expires (Op: ChangePriority).

### 8.4 Diaries in the Decide column

![Diary states](img/33-A2-diaries.png)

The same meter serves every diary, in six states: normal, due soon, **breached** (it stays on the case and escalates), **paused** (with the reason and the time left once resumed), payment hold, and **Hold expiring** (escalated).

### 8.5 Live case state

![Presence, case changed, lightbox changed, connection lost](img/34-A3-live-case-state.png)

- The header shows who else is viewing the case.
- "1 new Activation" appears as a banner, and the changed section is highlighted.
- An open lightbox refreshes its preview, and submit is disabled until the analyst acknowledges the change.
- If the connection drops, operations are disabled and unsaved text is kept on the device.

---

## 9. Why it alerted: activations and explanations

Section 2 of the case page: each model and rule that fired, why it fired, and how much each input contributed.

### 9.1 StructNet v7: score strip, Exact waterfall, sensitivity, health

![Activation waterfall](img/06-activation-waterfall.png)
![Sensitivity, compared to normal and health](img/06b-waterfall-detail.png)

The score strip shows calibrated probability, Very high, top 0.4% (30 d), 0.11 above threshold, and "31% of 212 closed cases were TP". Below it: the tier label, the log-odds waterfall with the dashed threshold, a negative contributor, "4 other inputs ▸", the sensitivity replay (0.74 if the beneficiary were ≥ 30 days old), compared to normal (median and mean rate), and health.

| Interaction | Does | Call | Status |
|---|---|---|---|
| Section load | CaseActivations with anchor, version, archived explanation, score metadata, band, percentile, precision, health, baselines | `GET /api/Case/{id}/Activations` (S2-01) inside CH-00 | **New** |
| ⚓ Anchor ARC-771203 ▸ | Transaction card | `GET /api/Archive/{id}/Card` (TC-01) | **New** |
| Model name / Model facts ▸ | Model card as at the snapshot | `GET /api/ModelFacts` (S2-04) | **New** |
| Waterfall / Table; "4 other inputs" | Switch view; expand | — | Client |
| ⚑ on a contribution | Feedback to the model owner | `POST /api/ExplanationFeedback` (S2-07) | **New** |
| Sensitivity line | Deterministic replay, Exact tier only | `GET /api/Activation/{id}/Sensitivity` (S2-08) | **New** |
| Add to narrative | Narrative Facts (behaviour only) | Op: AddNarrativeFact (S2-12) | **New** |

### 9.2 Structuring 9d and the sanctions match

![Rule and sanctions](img/07-rule-and-sanctions.png)

Both anchor on ARC-771190. The rule shows its dependency path with archived values. The sanctions match reads "Similarity 0.82, not a probability" and "not enough closed cases yet".

| Interaction | Does | Call | Status |
|---|---|---|---|
| Rule dependency path | Parsed rule graph with archived values | `GET /api/Activation/{id}/RulePath` (S2-09) | **New** |
| Open sanctions drawer | Matches by source; match detail | Existing sanction search + `/SanctionsMatch` (KP-04) | Exists + New |

---

---

## 10. The Journal

Section 3 of the case page: the money. Every transaction for the case key, with anchors pinned, baselines, pivots to any identifier, and tags.

### 10.1 Case subject, anchors pinned

![Journal](img/08-journal.png)
![Journal grid with both anchors pinned](img/08b-journal-grid.png)

Aggregation strip (baselines labelled typical and normal rate), score-history sparkline, daily chart with the normal line and timezone note, and the grid with both anchors pinned and ◆ rules shown by weight.

| Interaction | Does | Call | Status |
|---|---|---|---|
| Rows | Rows for key + value in the window; per row keys, rules fired, anchor flag | `GET /api/Journal` (S3-01) | **Extend** (Case Key Journal) |
| Aggregation strip | Archived counters at the anchor; computed baselines | `GET /api/Journal/Aggregate` (S3-14) | **New** |
| Daily chart | Series + server SVG | `GET /api/Journal/Series`, `GET /api/Chart/JournalSeries` (S3-15) | **New** |
| Sparkline | Score per transaction | `GET /api/Journal/ScoreHistory` (S3-16, reads `ArchiveScore`) | **New** |
| Row click / `Enter` | Select (edges light up) / transaction card | Client / TC-01 | Client / New |
| View by (`Y`) | Explode lanes | `GET /api/Journal/Explode` (S3-06) | **New** |
| Around this (`W`) | Same-day activity on the transaction's keys | `GET /api/Journal/Around` (S3-10) | **New** |

### 10.2 Pivoted to DEV-8831A: not the case subject

![Journal pivot](img/09-journal-pivot.png)

A warning border, "⚠ Not the case subject", a breadcrumb (`ACC-441 Sterling › DEV-8831A`), Back (`Alt+←`) and "Back to ACC-441 Sterling". The case key panel, which sits to the left of this section and is not shown in this crop, does not change.

| Interaction | Does | Call | Status |
|---|---|---|---|
| Pivot / breadcrumb / Back | Move or restore the focus; kept in the URL | S3-08, S3-09 → `GET /api/Journal` | Client + read |

---

### 10.3 Tagging transactions

Today an analyst can tag each transaction in the case journal with a pill, and the tags are set up in configuration. v2 keeps that and puts it on every row of the Journal and in the transaction card.

![Journal rows with tag pills](img/29-journal-tags.png)
![Tag menu](img/30-tag-menu.png)
![Tags in the transaction card](img/31-card-tags.png)

- **A Tags column** sits beside Type. It shows the row's tags as pills and ends with "+ Tag".
- **"+ Tag"** opens a small menu of the tenant's configured tags, grouped as they are set up (here: AML typology and Fraud typology). A tick shows each tag already on the row. Clicking a tag adds or removes it.
- **Reversible:** each change is inline with a 10-second undo toast, so there is no lightbox (5.3). It is still a case operation, so it is audited.
- **The transaction card** shows the same tags with its own "+ Tag", so a transaction can be tagged wherever it is opened.
- **Meaning:** the pill writes an `ArchiveTag` on that archived row. Any row can be tagged, not only anchors or rows linked to Activations. Training is a separate process that reads tags on its own schedule. **No tag means genuine**, so there is no "Genuine" tag.

| Interaction | Does | Call | Status |
|---|---|---|---|
| Journal rows | Each row carries its tags | `GET /api/Journal` (S3-01) returns `tags[]` per row | **Extend** |
| "+ Tag" menu | The configured tags for the case's model, grouped, active only | Existing `EntityAnalysisModelTag` configuration, scoped per model by `EntityAnalysisModelId` | Exists |
| Toggle a tag | Adds or removes the `ArchiveTag`; undo toast; `CaseEvent` | Op: TagTransaction / Op: UntagTransaction over today's write, `PUT /api/Invoke/Archive/Tag` (`ArchiveService`, operation `ArchiveTag`) | Exists → **Extend** into an operation |
| Undo | Reverse within 10 s | Op: Undo | **New** |
| Transaction card tags | Same tags and menu | `GET /api/Archive/{id}/Card` returns tags (TC-01) | **Extend** |

---

### 10.4 Bulk tagging

**Design:**
- **Selection:** a checkbox column, Shift-click for a range, Ctrl-click to add, and "Select all in view" for the current focus window (for example the 11 deposits of Structuring 9d). A lane header in the Journal offers "Select lane".
- **Bulk bar:** "11 selected · Tag… · Untag… · Clear". `T` opens the tag menu for the selection.
- **Mixed state in the menu:** ✓ on all, – on some, empty on none. Clicking a – tag adds it to the rest.
- **One operation, one undo:** the whole change is one `CaseEvent` and one 10-second undo, as for a single tag (10.3).
- **Limit:** 500 rows per change. Above that, the bar offers "Tag all 2,340 matching rows", which shows the count in a preview and runs as a background job with progress in the Inbox.

| Interaction | Does | Call | Status |
|---|---|---|---|
| Tag the selection | Adds and removes tags on many rows at once | Op: TagTransactions {archiveIds[], add[], remove[]} → CG-49 `PUT /api/Invoke/Archive/Tag/Bulk` | **New** |
| Tag by filter | Preview count, then a background job | CG-49 with `filter` and `previewOnly` | **New** |
| Undo | Reverses the whole change | Op: Undo | **New** |

### 10.5 Journal: Merge and Pin lens

![Merge lens and Pin lens](img/35-B1-merge-pin-lens.png)

Up to 4 keys on one timeline. Each lane has a letter and a low-chroma colour, used as a label only and never as a risk signal. Pinned transactions stay at the top in every lens and travel to Compose as facts.

---

## 11. The Network

Section 4 of the case page: who and what is connected to the case subject, as a graph and as a list.

### 11.1 Graph with shared selection

![Network](img/10-network.png)

The selected transaction ARC-771203 highlights its edges. Clicking a node fills the panel (Pivot, Link, Block). Clicking an edge filters the Journal to the transactions behind it. The IP hub is hidden.

| Interaction | Does | Call | Status |
|---|---|---|---|
| Section load | Neighbourhood; hubs suppressed; cap 150 | `GET /api/Graph/Neighbourhood` (S4-01) | **New** |
| Edge click | Transactions behind the edge → Journal | `GET /api/Graph/Edge/{id}/Transactions` (S4-05) | **New** |
| Link to case / Block identifier | Lightboxes | Op: LinkCaseKey, Op: BlockIdentifier (S4-07, S4-08) | **New** |

### 11.2 Explode: the Network's list view

![Explode](img/11-explode.png)

The 1-hop graph as a Tabulator data tree with count, total, first and last, and flags. The hub is one collapsed lane.

| Interaction | Does | Call | Status |
|---|---|---|---|
| Explode (list view) | Lanes by linked key | `GET /api/Journal/Explode?by=linkedKey` (S4-09) | **New** |
| Lane click | Select the node | — | Client |

---

### 11.3 Explode with sparklines

![Explode lanes with sparklines](img/36-B2-explode-sparklines.png)

One 30-day sparkline per lane, each on its own scale; the count column carries the magnitude. The hub lane is shown flat and faded.

---

## 12. Drawers

Detail that opens beside the page without losing your place: the transaction card, its raw payload and the model's facts.

### 12.1 Transaction card and raw payload

![Transaction card](img/14-transaction-card.png)
![Payload](img/15-payload.png)

The anchor ARC-771203: keys with Pivot and Link, rules that fired (◆ StructNet ≥ 0.80 creates cases; Velocity 1h does not), and what Jube knew then. Payload opens as the second drawer.

| Interaction | Does | Call | Status |
|---|---|---|---|
| Open | Archived row, keys, rules with versions, counters | `GET /api/Archive/{id}/Card` (TC-01) | **New** |
| Payload ▸ | Raw payload, audited | `GET /api/Archive/{id}/Payload` (TC-05) | Extend |
| Around this / Add to narrative facts | — | S3-10; Op: AddNarrativeFact | **New** |

### 12.2 Model facts

![Model facts](img/16-model-facts.png)

StructNet v7 as it was when it scored: gradient-boosted trees, base 0.04 (−3.18), "v8 is now live", "not declared by the endpoint", an unlabelled input flagged.

| Interaction | Does | Call | Status |
|---|---|---|---|
| Open | Model card from the snapshot | `GET /api/ModelFacts` (MF-01) | **New** |
| Snapshot diff ▸ | Diff between snapshots | `GET /api/Snapshot/Diff` (MF-02) | **New** |

---

---

## 13. Notes and writing

Section 5 of the case page: coded notes, the Compose workspace for reports, and AskJooby beside the case.

### 13.1 Notes and action codes

![Composer](img/12-composer.png)

Activity → result chips (digits) → text → `Ctrl+Enter`. "No answer" shows its follow-up rule (+4 h, capped by the STR diary). System notes sit quieter than analyst notes.

| Interaction | Does | Call | Status |
|---|---|---|---|
| Activity select | Action codes, last used first | `GET /api/CaseWorkflowAction` (S5-03) | Extend |
| Result chips | Results with diary effect | `GET /api/CaseWorkflowActionResult` (S5-04) | **New** |
| Add note with code | Coded note, follow-up, hooks via the dispatcher (after the undo window) | Op: RecordAction (S5-06) | **New** |

---

### 13.2 Compose and the quality scorecard

![Compose mode](img/37-C1-compose-scorecard.png)

The facts tray is on the left and the narrative on the right. The narrative has a gutter marking each sentence as observed, inference or missing. The scorecard sits in the right column: red blocks freeze, and amber needs one audited acknowledgement. The grounds for suspicion are always written by a person.

### 13.3 AskJooby in the Decide column

![AskJooby docked with proposal cards](img/38-C2-askjooby-panel.png)

`Alt+J` swaps Decide for AskJooby, and the suggested step stays visible at the top. Every answer shows the query it ran. A **finding** (with evidence) has one primary button that opens the normal lightbox. An **opinion** (no evidence) can only ask for evidence or be dismissed.

## 14. Case history and audit

Section 6 of the case page: everything that happened to the case, in a tamper-evident chain, including who looked at it.

### 14.1 Events and risk history

![Case Events Journal](img/13-events-risk-history.png)
![Case risk history](img/13b-risk-history.png)

Prior cases (empty state), the typed Case Events Journal with its hash-chain badge (now including `CaseRiskRecomputed`), and the **case risk history**.

| Interaction | Does | Call | Status |
|---|---|---|---|
| Events | Typed events, operation ids | `GET /api/CaseEvent` (S6-02) | Extend |
| Hash-chain badge | Verify the chain | `GET /api/CaseEvent/Verify` (S6-03) | **New** |
| Risk history | Every `CaseRiskAssessment` | `GET /api/Case/{id}/Risk/History` (S6-05) | **New** |

---

### 14.2 Read audit

Every view of a case is recorded as a `CaseViewed` event, with controls around it for sensitive accounts.
- **What is recorded:** case open, peek, transaction card, raw payload, customer data reveal, export, print, and opening a case from search. Each record has who, when, from where (screen) and why (if asked).
- **Sensitive accounts:** an account can be marked Sensitive (staff, related to staff, VIP, PEP). Opening a case on one, when it is not assigned to you or your team, asks for a reason first ("Assigned by supervisor", "Linked case", "QA review", other with text). This is break-glass: it never blocks, it records.
- **Viewed by:** in the case history (14.1), a "Views" filter lists who looked, when and why. Supervisors and the MLRO only.
- **Unusual viewing:** a Team view tile flags views outside assignment, after hours, or of many Sensitive accounts, for the supervisor to review.
- **Retention:** view records are kept for a tenant-set period (default 24 months), then aggregated.
- **Relation to conflict of interest (specification 8.1):** conflict of interest stops a related person from working a case; the read audit records anyone who looks. Both are needed.

| Interaction | Does | Call | Status |
|---|---|---|---|
| Views recorded | One event per view | `CaseEvent` type `CaseViewed` with `surface` and `reason` | **Extend** |
| Reason prompt | Asked on Sensitive accounts outside assignment | Returned by CH-00 as `requiresViewReason`; answer via CG-50 `POST /api/Case/{id}/ViewReason` | **New** |
| Viewed by | The views list for supervisors | `GET /api/CaseEvent?caseId=&type=CaseViewed` (S6-01 filter) | **Extend** |
| Unusual viewing tile | Flags for review | CG-51 `GET /api/Team/ViewAnomalies` | **New** |

---

## 15. Operations

Every change to a case is an operation: previewed, confirmed, then executed exactly as previewed.

### 15.1 Operation lightboxes

Every lightbox opens with a server Preview and submits with Execute (22.13). They re-preview only when an input that changes the consequences changes.

![Close](img/17-close-lightbox.png)
![Close ready](img/18-close-ready.png)
![File STR](img/19-file-str.png)
![Approver](img/20-approver.png)
![Break lock](img/21-break-lock.png)

| Lightbox | Opens from | Calls | Status |
|---|---|---|---|
| Close (with "Do it now") | `D`, cockpit | `POST /api/CaseOperation/Close/Preview`, `/Execute`; `/Case/{id}/TypologySuggestions`; rationale templates (LB-C). Close cites the `CaseRiskAssessment` in force. | **New** |
| File STR (typed reason) | `F`, suggested step | Preview/Execute; blocked until approved; Op: RequestApproval (LB-F, LB-RA) | **New** |
| Approver's view | M. Karim's Inbox | `GET /api/CaseOperation/Approval/{id}`; Op: Approve / Reject (LB-A) | **New** |
| Break lock (typed reason) | Lock column, peek | Op: BreakLock (LB-BL) | **New** |
| Bulk close | Bulk bar | Preview with `caseIds[]` (LB-B) | **New** |
| Step-up | Any, when required | `POST /api/Authentication/StepUp` (LB-06) | **New** |

---

### 15.2 State catalogue

![Loading, empty, failed, stale, restricted](img/41-D3-state-catalogue.png)

One pattern for each state, used by every section:
- **loading:** a skeleton the size of the final content;
- **empty:** says what to do next;
- **failed:** says how to fix it, with a reference;
- **stale:** shows the time of the last good values;
- **restricted:** a "restricted link", with no details.

---

## 16. Payments: holds, release and refuse

The design for a transaction whose state is **held**: how it appears, how an analyst decides it, how re-authentication sits inside the decision, and how the result is reported. The spec lists the operations (5.4.1 Mitigation, 11 Payment decisions, J1) and step-up (10.2), but neither had been designed.

### 16.1 The held payment card

![Held payment card](img/22-held-payment-card.png)

On a case in the **Live money** lane, the peek panel (and the top of Case detail views) shows the held transaction first, because it is the only thing on the case with a clock measured in hours. It shows the payment, who from and to, and what held it. It shows whether the payment system has confirmed the hold, the time left, and **what happens if nobody decides**.

| Interaction | Does | Call | Status |
|---|---|---|---|
| Card loads | The held transaction, its hold record and live status | Proposed `GET /api/Case/{id}/PaymentHolds` (inside CH-00 for the first paint) | **New** |
| Status line | Whether the payment system confirmed the hold, from its synchronous HTTP response, read from the execution log | Inside `GET /api/Case/{id}/PaymentHolds` | **New** |
| Countdown | Minutes diary (`CaseDiary`, type Payment hold) | CH-00 / `CaseHub.PaymentHoldChanged` | **New** |
| Decide payment… (`H`) | Opens the lightbox | Op: DecidePayment Preview | **New** |

### 16.2 The decision lightbox

![Payment decision lightbox](img/23-payment-lightbox.png)

One lightbox, three decisions, keys `1`–`3`: **Release**, **Refuse (return to sender)**, **Extend hold**. It asks only what the operation needs:

- the decision;
- the evidence behind it (a coded note, for example "Called customer → Reached");
- a typed reason;
- for Refuse, whether to block the beneficiary as well.

The "What will happen" block comes from the server and changes with the decision. It names the HTTP Endpoint and idempotency key, the List change, the diary and lane, the tipping-off check on any customer notification, and the case risk assessment the decision cites.

| Interaction | Does | Call | Status |
|---|---|---|---|
| Choose a decision / extend by | Re-preview (a consequence input) | `POST /api/CaseOperation/{ReleasePayment|RefusePayment|ExtendHold}/Preview` | **New** |
| Evidence select | Links the coded note that justifies it | `GET /api/CaseNote?caseId=` (already loaded) | Exists |
| Also block beneficiary | Adds Op: BlockIdentifier to the same execution (all-or-nothing) | Preview includes it | **New** |
| Submit (`⌃⏎`) | Step-up if needed, then Execute | `POST /api/CaseOperation/{op}/Execute` with `stepUpToken` | **New** |

### 16.3 Step-up inside the lightbox

![Step-up with passkey](img/24-step-up-passkey.png)
![Step-up with a code](img/25-step-up-code.png)

Submitting an operation that needs re-authentication does not open a new window. A panel appears inside the same lightbox, and everything entered stays visible but inactive above it. The panel offers the methods enabled for the user: authenticator code (phase 1) and passkey (phase 2). There is no SMS. "Back to the form" returns with every input intact. A successful step-up is valid for 5 minutes (15 minutes for payment decisions) for that user (so M. Karim approving, then A. Vance filing, each step up once) and is recorded on the operation's `CaseEvent`.

**Operations that ask for it:**
- from 8.2: File, Approve, Break lock;
- Release and Refuse payment. Extend hold does not ask, because no money moves.

| Interaction | Does | Call | Status |
|---|---|---|---|
| Panel opens | Server says step-up is required (Execute answers 401 with `step_up_required`, or Preview flags it) | LB-06 | **New** |
| Choose a method | Starts the challenge (TOTP prompt, or WebAuthn options) | Proposed `POST /api/Authentication/StepUp/Begin {method}` | **New** |
| Use passkey / Verify | Completes it; returns a short-lived `stepUpToken` | Proposed `POST /api/Authentication/StepUp/Complete` | **New** |
| Continue | Execute carries the token; the lightbox proceeds without re-entry | LB-04 | **New** |

### 16.4 Honest result

![Sending](img/26-payment-sending.png)
![Confirmed](img/27-payment-confirmed.png)
![Timeout: outcome unknown](img/28-payment-timeout.png)

Because the call leaves Jube, the lightbox does not simply close on Submit. It shows the steps as they happen:

1. preview checked;
2. re-authenticated;
3. sent to core banking;
4. the payment system's own confirmation and reference, then the diary and lane change and the system note.

If the payment system does not answer, the lightbox says **"Outcome unknown"**. It does not report failure, because the payment may already have moved. The case stays in Live money. "Retry safely" re-sends the same request with the same idempotency key, so the listener acts at most once.

| Interaction | Does | Call | Status |
|---|---|---|---|
| Confirmation arrives | The synchronous HTTP response is the confirmation | Dispatcher HTTP Endpoint + execution log | Exists + **Extend** |
| Retry safely | Re-sends with the same idempotency key | Op: {same op}/Execute, same key | **New** |
| Result recorded | System note, `CaseEvent`, lane change, `CaseRiskChanged` push | Execute + worker | **New** |

### 16.5 Rules for every hold

- **Nobody decides, it escalates.** Before the hold ceiling (default 60 minutes, set per market), the status becomes **Hold expiring**, the supervisor is emailed with a deep link, and the escalation repeats every 30 minutes until someone acts. Only at the legal maximum does the market's default apply (for the UK: release, unless there is evidence to refuse).
- **Step-up on Release and Refuse,** because both move money irreversibly. Extend hold does not need it.
- **Per-user release limit.** Each user has a release limit with a currency. Above it, Release becomes **Request release**, and a second user whose limit covers the amount approves it with step-up. Refuse has no limit, because it is the safe direction.
- **Extensions:** no count limit, but a time ceiling from the market's hold policy. Each extension records a reason and a next step.
- **Confirmation** is the payment system's synchronous answer. On timeout the outcome is unknown, and "Retry safely" re-sends with the same idempotency key; the listener treats a repeated key as the same request (default timeout 30 s).
- **Step-up methods:** an authenticator code first, then passkeys as the phishing-resistant option. Tenants signing in through their own identity provider step up with a fresh provider login. No SMS. A step-up lasts 5 minutes, and for payment decisions it is bound to the exact decision previewed: a changed amount, payee or decision needs a new step-up.
- **Payer told about the hold:** where the market requires it, the notice goes out automatically through the dispatcher, subject to the tipping-off check, and the card shows "Payer notified 08:55 by SMS" or "Not yet sent: due by end of 25 Sep".
- **Payment system unavailable:** the lightbox shows "Payment system unavailable since 08:41" and disables Submit. Decisions are never queued; the hold diary keeps running and the supervisor escalation starts early.
- **No bulk Release.** Refuse can be bulk only for payments to the same beneficiary, with one reason.
- **Timers are accessible:** step-up expiry warns 60 s before with "Keep verified"; hold countdowns announce politely at 1 h and 15 min, not every second; nothing relies on colour alone.

### 16.6 Hold expiring email, approver's release, step-up enrolment

![Hold expiring email](img/47-F3-hold-email.png)
![Approve release above the maker’s limit](img/48-F4-approve-release.png)
![Step-up enrolment and recovery codes](img/49-F5-step-up-enrolment.png)

- **The email** carries the case id and a deep link, never customer names or account details.
- **The approver** sees the maker's limit and reason, the evidence, their own limit and the hold time left.
- **Enrolment:** authenticator first, passkey later, no SMS, and six one-time recovery codes.

### 16.7 Payment hold policy: one row per market

Hold rules are data, not code. Each jurisdiction pack owns rows in a new configuration table, `PaymentHoldPolicy`, edited through the generic DTO form (`[FormEndpoint]`, maker-checker publish, versioned). The hold diary, the "Hold expiring" escalation, the Extend hold ceiling and the payer notice all read the active row for the payment's market. Nothing about any market is hard-coded.

| Column | Type | Meaning | UK row (example) | Egypt row |
|---|---|---|---|---|
| `Market` | ISO 3166 code + optional scheme (for example `GB`, `GB-FPS`) | Which payments the row applies to; the scheme-level row wins over the country row | `GB` | `EG`, values from compliance |
| `Direction` | Outbound, Inbound or Both | Lets inbound holds have their own rules | Both | from compliance |
| `MaxHold` | integer | The ceiling | 4 | from compliance |
| `HoldUnit` | BusinessDays or Hours | How `MaxHold` is counted; business days use the pack's holiday calendar | BusinessDays | from compliance |
| `HoldClockStart` | PaymentReceived or HoldPlaced | When the ceiling starts counting | PaymentReceived | from compliance |
| `OnMaxHold` | Release, Refuse or EscalateOnly | What happens at the ceiling with no decision. EscalateOnly means no automatic action, and the case stays escalated until someone decides | Release | from compliance; the safe default until known is **EscalateOnly** |
| `EscalateBefore` | minutes | When the status becomes "Hold expiring" and the supervisor is emailed | 60 | 60 |
| `ReEscalateEvery` | minutes | How often the escalation repeats | 30 | 30 |
| `PayerNoticeEnabled` | boolean | Whether a payer notice is sent. **Optional per market** | true | false until compliance says otherwise |
| `PayerNoticeDue` | integer + unit | How soon after the hold starts it must be sent | 1 BusinessDay (end of the next business day) | — |
| `PayerNoticeChannel` | SMS, Email, App or the customer's preferred channel | How it's sent, through the dispatcher | Customer preferred | — |
| `PayerNoticeTemplateId` | FK | The message, which is subject to the tipping-off check | "Payment paused" | — |
| `EffectiveFrom`, `EffectiveTo` | dates | Rule changes take effect on a date without a release; holds already running keep the row they started under | 30 Oct 2024 → | — |
| `Source` | text | Which regulation or internal policy the values come from, for audit | PSR 2017 as amended 2024; FCA guidance | — |

**Rules:**
- If no row matches a payment, the hold is still placed, and a config warning shows on the Team view. The hold defaults to **EscalateOnly**, with no ceiling and no notice. Jube never invents a legal limit.
- The held payment card shows the row that applies, for example "Pack GB · 4 business days from receipt · release at ceiling · notice sent 08:55".
- Changing a row is a configuration change: it needs maker-checker approval, is recorded as an event, and appears in Pending changes.

**New endpoints:** CG-24 `/api/PaymentHoldPolicy` (generic DTO CRUD with Schema), and CG-25 `GET /api/PaymentHoldPolicy/Resolve?market=&direction=&at=`, used by the hold worker and the held payment card.

---

## 17. Payments: inbound holds, recall and payee checks

The same card, lightbox, step-up and honest result as section 16, extended to money arriving at mule accounts, money that has already left, and the payee name check made before payment. Every external clock is a diary, so it sorts the lanes and escalates like any other.

### 17.1 Inbound holds

**Why:** UK payment-delay legislation covers outbound payments only. FCA guidance (FG24/6) explains how a PSP may delay crediting a suspicious inbound payment under its existing financial crime obligations, and expects it to talk to the payee unless that would be unlawful (tipping off). The mule account is where networks are caught.

**Design:**
- **Direction chip** at the top of the held payment card: "Inbound → ACC-02 Alpha LLC" or "Outbound ← ACC-01". The direction also shows in the Cases page Why column ("Inbound hold · 3 h left").
- **Decisions (keys 1–3):** **Credit to account**, **Return to sender**, **Extend hold**. A fourth, **Credit and hold outbound**, credits the payment but places a time-boxed outbound hold rule on the receiving account (default 24 h), so money that is onward-forwarded within minutes (the mule pattern) is caught without refusing a payment that may be genuine.
- **Return to sender** sends the return through the dispatcher and offers to notify the sending PSP with a minimised fraud notice. It needs step-up, like Refuse.
- **Payee contact:** for inbound holds, the payee notice is **off by default** because the payee is the suspect. Turning it on runs the tipping-off check (16.5) and records the result.
- **Consent regime:** where a suspicious activity report with a consent request is filed (for example a UK DAML), the hold diary follows the report's notice and moratorium periods from the jurisdiction pack, and the card says so ("Held under consent request · notice period ends 9 Oct").
- **Network link:** the card links the sender as a Network node. If the same sender has paid other held or flagged accounts, the card says "Sender also paid 3 accounts in this network".

| Interaction | Does | Call | Status |
|---|---|---|---|
| Card loads | Hold with `direction`, sender and receiver, policy row | `GET /api/Case/{id}/PaymentHolds` returns `direction` | **Extend** |
| Policy | The inbound row for the market (`Direction = Inbound`) | CG-25 `PaymentHoldPolicy/Resolve?direction=Inbound` | Exists (designed) |
| Credit to account | Releases the inbound hold | Op: CreditInbound Preview/Execute | **New** |
| Credit and hold outbound | Credits, then adds a time-boxed outbound hold rule on the account | Op: CreditInbound with `holdOutboundHours` | **New** |
| Return to sender | Returns the funds; optional sender-PSP notice | Op: ReturnInbound (+ Op: NotifyCounterpartyPsp) | **New** |
| Extend hold | Same as outbound, ceiling from the inbound row | Op: ExtendHold | Exists (designed) |

### 17.2 Recall: the payment has already left

**Why:** on instant rails the payment usually completes before anyone looks. In SEPA, the originator's bank sends a recall (camt.056). For the reason "fraudulent origin" it can do so for up to 13 months; for duplicates and technical errors, 10 banking days. The beneficiary bank must answer within 15 banking days, either returning the funds (pacs.004) or refusing with a reason (camt.029). UK and other schemes have their own channels, so the rules sit in a table per scheme.

**Outbound recall (we sent it):**
- **Request recall** appears on any executed outbound transaction: in the transaction card, in the Journal row menu, and as the suggested operation in the cockpit when a case's suspect payment has already settled.
- **Lightbox:** the reason, offered from the scheme row (for SEPA: Fraud, Duplicate, Technical); the amount (full, or partial where the scheme allows); the receiving bank (from the payment); the deadline to send ("Fraud recall possible until 4 Nov 2027"); and a coded note as evidence. "What will happen" names the message, the diary for the answer and any customer notice. It needs no step-up, because no money leaves.
- **Recovery card** in Case detail views, one per recall: Sent → Acknowledged → **Returned** (full or partial, with the amount) or **Refused** (reason) or **No answer by the due date** (escalates to the supervisor, offers "Chase" and records it). The card never says "recovered" until the return has been credited.
- **One recall per payment.** A second request on the same payment is blocked, as the schemes reject duplicates.

**Inbound recall (we are the receiving bank):**
- A recall received for a payment credited to one of our accounts **opens or joins a case on that account**, in the Deadline near lane, with a diary for the answer (SEPA: 15 banking days).
- **Recall request card:** who asked, the payment, the reason, the answer due, and the **available balance now**, with how much has already left the account and where (linked to the Journal).
- **Decisions:** **Return funds** (full, or partial up to the balance), **Refuse with reason** (no funds, account closed, legal reason, customer refused where consent is needed), **Ask the customer** (where national law requires consent, using the RFI flow, section 20.3). Return needs step-up.
- **Signal:** a fraud recall against an account feeds the models (`InboundFraudRecallCount`) and adds the account to the case's Network as a suspected mule.

| Interaction | Does | Call | Status |
|---|---|---|---|
| Request recall | Preview then send through the dispatcher | Op: RequestRecall Preview/Execute | **New** |
| Recall rules | Reasons, windows and response time for the scheme | CG-40 `GET /api/RecallSchemeRule/Resolve?scheme=&reason=` | **New** |
| Recovery card | Status of every recall on the case | CG-41 `GET /api/Case/{id}/Recalls` (inside CH-00) | **New** |
| Answer arrives | Status moves, diary closes, system note | Dispatcher inbound endpoint → CG-42 `POST /api/Recall/{id}/Response` | **New** |
| Chase | Records a chase and resets the reminder | Op: ChaseRecall | **New** |
| Recall received | Opens or joins a case on the receiving account | CG-42 (inbound message) + case creation rule | **New** |
| Answer an inbound recall | Return funds, refuse, or ask the customer | Op: AnswerRecall {return, refuse, askCustomer} | **New** |

### 17.3 Payee name checks (Verification of Payee, Confirmation of Payee)

**Why:** since 9 October 2025, euro-area PSPs must check the payee name against the IBAN before a euro credit transfer. The EPC scheme returns Match, Close match (with the account holder's name), No match, or Check not possible. The UK Confirmation of Payee codes are finer: they also report an account-type mismatch (business or personal), opted-out accounts and switched accounts. The EU Payment Services Regulation, agreed in November 2025, extends the check to all credit transfers and moves the loss to the payer's PSP if it fails to warn. A payer who goes ahead after a No match is one of the strongest scam signals.

**Design:**
- **One normalised record** on each transaction: `PayeeCheck { scheme, result: Match | CloseMatch | NoMatch | NotPossible, reasonCode, nameEntered, nameReturned, accountTypeMismatch, warningShown, payerProceeded, checkedAt }`. Raw scheme codes (EPC, or CoP codes such as ANNM, MBAM, BANM, PAMM, OPTO, CASS) are kept and shown on hover.
- **Payee check chip** on the held payment card, the transaction card and as an optional Journal column: an icon and words, never colour alone ("No match · payer went ahead 2 min later"). Close match shows both names side by side.
- **Case detail views line:** "Payee check: No match (warned 10:12, sent 10:14)" when the case's anchor payment has a non-match.
- **Model inputs:** `PayeeCheckResult`, `PayerProceededAfterWarning`, `SecondsFromWarningToSend`, `NameSimilarity`. They are offered to model authors as standard features, with their meaning documented in the model facts drawer.
- **Receiving side:** when we answer payee checks for our own accounts, the count of No match and Close match answers against an account in 24 h is a mule signal: "11 payers were warned about this account today" appears in the case key panel of a case on that account.
- **Evidence:** the claim view (18.1) shows the payee check and whether the warning was shown, because that decides liability under the EU rules.

| Interaction | Does | Call | Status |
|---|---|---|---|
| Chip and line | The payee check on the transaction | `GET /api/Archive/{id}/Card` (TC-01) and `GET /api/Journal` (S3-01) return `payeeCheck` | **Extend** |
| Capture | The payee check arrives with the transaction payload | `PayeeCheck` fields in the model's request XPath set | **Extend** (configuration) |
| Receiving-side count | Checks answered against an account | CG-43 `GET /api/Account/{id}/PayeeCheckStats?window=24h` | **New** |

---

## 18. Claims and customers who need support

When a customer reports a scam, the claim runs against its legal clocks; customers who need support get adjusted treatment throughout.

### 18.1 APP reimbursement claims

**Why:** the UK rule covers Faster Payments and CHAPS. The sending PSP must reimburse within **5 business days**, may **stop the clock** to get information it needs, and must reach an outcome within **35 business days**. The cap is £85,000 per claim; an optional excess up to £100 may not be applied to vulnerable customers; the receiving PSP pays half; claims can be made up to 13 months after the last payment. The exceptions are first-party fraud, gross negligence (the consumer standard of caution) and civil disputes. An independent review of the cap, the split and the caution exception runs through 2026/27. The EU Payment Services Regulation adds a refund duty where a fraudster impersonated the PSP and the customer reported it to the police, from about 2027–28.

**Claim case:** a case type **APP claim**, opened from a case, from Journal rows selected together, from an Inbox referral or from a customer call.
- **Two clocks in the header,** always both visible: **Decision** (5 business days, pausable) and **Outcome deadline** (35 business days, absolute). While paused, the Decision clock shows "Paused 2 d · waiting for customer's bank statements" and a resume condition. The absolute clock never pauses.
- **Stop the clock** needs a reason category and what is being waited for. Sending an RFI (20.3) with "Stop the clock" ticked pauses it, and the customer's reply resumes it automatically.
- **Payments in scope:** one or more outbound payments, picked from the Journal. The panel shows each with its scheme, receiving PSP, payee check, and the hold decision if there was one (so a payment released after a hold is visible as such). Payments outside the scheme (international, or same-firm) are shown greyed with the reason.
- **Assessment panel:** claimed amount, cap, excess (automatically 0 if a vulnerability record is active), and the three exceptions as a checklist. Choosing an exception requires linked evidence. **Gross negligence also requires a supervisor's approval** and can never be chosen by AskJooby or an agent.
- **Receiving PSPs:** one row per receiving PSP: notified at, acknowledged, contribution requested, contribution received. Notification is a diary of its own, set from the scheme row.
- **Decide claim:** **Reimburse in full**, **Reimburse in part** (reason per payment), **Decline** (exception and evidence). Reimburse moves money, so it needs step-up and respects the per-user release limit. Decline needs maker-checker.
- **Customer letter:** the outcome letter is composed in Compose (13.2) from the scheme's template and goes out through the dispatcher.
- **Receiving side:** a claim notice from another PSP about one of our accounts opens or joins a case on that account, with a diary for our contribution and the claim as evidence.

**Scheme rules (`ReimbursementScheme`, one row per scheme):** `Scheme` (GB-FPS, GB-CHAPS, EU-PSR-Impersonation), `DecisionDays`, `AbsoluteDays`, `DayUnit`, `Cap`, `MaxExcess`, `ExcessWaivedIfVulnerable`, `ReceivingShare`, `ClaimWindowMonths`, `NotifyReceivingWithin`, `Exceptions[]`, `EffectiveFrom/To`, `Source`. Edited like `PaymentHoldPolicy`; open claims keep the row they started under.

| Interaction | Does | Call | Status |
|---|---|---|---|
| Open claim | Creates the claim case with selected payments | Op: OpenClaim | **New** |
| Claim panel | Payments, clocks, receiving PSPs, assessment | CG-44 `GET /api/Claim/{caseId}` (inside CH-00 for claim cases) | **New** |
| Scheme rules | Clocks, cap, excess, share for the scheme | CG-45 `/api/ReimbursementScheme` (DTO CRUD) and `/Resolve` | **New** |
| Stop / resume the clock | Pauses or resumes the Decision diary | Op: StopClock {reason, awaiting} · Op: ResumeClock | **New** |
| Notify receiving PSP | Sends the notice, starts their diary | Op: NotifyReceivingPsp | **New** |
| Decide claim | Reimburse (step-up, limit) or Decline (maker-checker) | Op: DecideClaim | **New** |
| Contribution | Requests or records the receiving PSP's share | Op: RequestContribution · Op: RecordContribution | **New** |
| Claim register | Export of claims, clocks and outcomes | CG-46 `GET /api/Claim/Register?from=&to=` | **New** |

### 18.2 Customers who need support

**Why:** the FCA treats vulnerability as a spectrum of risk, driven by health, life events, resilience and capability. Firms are expected to record customers' needs so they can be found at the next contact, review the records, and monitor outcomes for vulnerable customers against others; its 2025 review found most firms weak at outcome monitoring. Under the UK reimbursement rules, no excess may be charged to a vulnerable customer.

**Design:**
- **Record:** `VulnerabilityRecord { driver: Health | LifeEvent | Resilience | Capability, characteristic (coded, optional), needs[] (for example "Call, not SMS", "Allow extra time", "Speak to authorised third party"), source: CustomerTold | ThirdParty | StaffObserved, consentRecorded, recordedBy, recordedAt, reviewBy }`. A review date is mandatory (default 6 months).
- **Shown as needs, not labels:** the case key panel shows a chip "Support needs · 2", which opens the needs. The driver and characteristic are hidden behind an audited reveal, like masked fields (8), because they can be health data.
- **Where it changes behaviour:**
  - the payment lightbox and RFI form show the needs at the top, and RFI uses the customer's preferred channel;
  - the claim sets the excess to 0 and requires a supervisor for gross negligence;
  - the Compose quality scorecard checks plain language when needs include capability;
  - the payer notice uses the preferred channel.
- **Not a risk input:** the record is excluded from case risk and fraud models, so it can never raise a customer's risk score. AskJooby may point out possible indicators in notes, but only staff create a record.
- **Outcome monitoring:** a Team view tile compares outcomes for customers with and without support needs: hold duration, claim time to decision, claims declined, complaints.

| Interaction | Does | Call | Status |
|---|---|---|---|
| Chip and needs | Active needs for the case's customer | CG-47 `GET /api/Customer/{id}/Vulnerability` (needs only; inside CH-00) | **New** |
| Reveal driver | Shows driver and characteristic, audited | `POST /api/Customer/{id}/Vulnerability/Reveal` (a `CaseEvent`) | **New** |
| Add or review | Records needs with source, consent and review date | Op: RecordVulnerability · Op: ReviewVulnerability | **New** |
| Outcome tile | Outcomes split by support needs | CG-48 `GET /api/Team/Outcomes?split=supportNeeds` | **New** |

---

## 19. Reporting and referrals

Filing suspicious activity reports with approval, tracking them with the authority, and taking referrals from staff.

### 19.1 Referral form and receipt

![Referral form and receipt](img/45-F1-referral.png)

Tenant branding only. The receipt gives the time of disclosure and never the outcome.

### 19.2 Report status

![Report lifecycle with rejection](img/46-F2-report-status.png)

The lifecycle runs as steps. The FIU's message is shown against the generic field it refers to, followed by the resubmission diary and the version chain (v1 is never edited in place).

---

## 20. Assurance and automation

Tools that let a team check its own work, ask customers for information, and spend analyst time where it matters. Agent triage and web research are off by default: agent triage is enabled per filter, web research per tenant with every query logged.

### 20.1 Agent L1 triage with a work log

![Agent triage card with work log](img/50-M1-agent-triage.png)

An agent pre-investigates each new case using read-only tools and recommends a disposition, citing the evidence it used. **Accept** opens the normal Close lightbox pre-filled, and the analyst still reviews and submits. **Override** records the disagreement with a reason. Every step is a CaseEvent, and the model and prompt versions are pinned in the run record. Where the agent and the analyst disagree, the case is more likely to be sampled for QA.

| Interaction | Call | Status |
|---|---|---|
| Card loads | CG-01 `GET /api/Case/{id}/AgentRun` | New |
| Re-run | CG-02 `POST /api/Case/{id}/AgentRun` | New |
| Accept | Opens Close Preview (existing lightbox) | Reuse |
| Override | CG-03 Op: `OverrideAgent` | New |

### 20.2 QA sampling and review

![QA sampling card and review lightbox](img/51-M2-qa-sampling.png)

The sampling plan (rate, schedule and weightings) sits on the Team view. Reviewers work a queue with a pass, fail or N/A checklist. A **Major** outcome can reopen the case, which needs approval.

| Interaction | Call | Status |
|---|---|---|
| Plan | CG-04 `GET/PUT /api/QaPlan` | New |
| Queue | CG-05 `GET /api/QaQueue` | New |
| Record review / Reopen | CG-06 Op: `RecordQaReview`, CG-07 Op: `Reopen` | New |

### 20.3 Request for information to the customer

![RFI lightbox, customer portal and thread](img/52-M3-rfi.png)

The analyst sends questions and document requests from the case. The **tipping-off check** blocks sending when a SAR is pending, unless the MLRO approves. The customer answers through a single-use, tenant-branded link, and the answers come back as case uploads and a note. An RFI diary runs until the customer answers.

| Interaction | Call | Status |
|---|---|---|
| Send / cancel | CG-08 Op: `SendRfi`, CG-11 Op: `CancelRfi` | New |
| Portal | CG-09 `GET /api/Rfi/{token}`, CG-10 `POST /api/Rfi/{token}/Answer` (public, rate-limited) | New |

### 20.4 Web research with citations

![Web research finding with citations](img/53-M4-web-research.png)

This is AskJooby's Researcher persona. Every claim cites its sources, showing the URL, retrieval time and the quoted passage. A **finding** needs at least two independent sources. **Save to case** stores a PDF of the sources. It is off by default for each tenant, because it sends data outside Jube.

| Interaction | Call | Status |
|---|---|---|
| Ask | CG-12 `POST /api/AskJooby/Research` | New |
| Save to case | CG-13 Op: `SaveResearch` | New |

### 20.5 Automation recipes

![Recipe builder with dry run and run log](img/54-M5-automation-recipes.png)

A recipe is **When** (a schedule or event), then **If** (a condition from the query builder, compiled on the server), then **Then** (registered operations only). **Test** replays the last 30 days to show what the recipe would have done. **Publish** needs maker-checker approval. Each run is logged with its result and links to its CaseEvents. Recipes replace separate macros, hooks and scheduled tasks with one audited mechanism.

| Interaction | Call | Status |
|---|---|---|
| Recipes | CG-17 `/api/AutomationRecipe` CRUD | New |
| Test | CG-18 `POST /api/AutomationRecipe/{id}/DryRun` | New |
| Run log | CG-19 `GET /api/AutomationRecipe/{id}/Runs` | New |
| Publish | Op: `PublishRecipe` | New |

### 20.6 Tune a rule from outcomes

![Rule backtest and shadow mode](img/55-M6-rule-tuning.png)

This opens from the rule path in Activations. It shows the proposed threshold, a backtest over the same 90 days (hits, true positives caught and missed, analyst hours), and a link to the missed true positives. **Shadow mode** runs the new version alongside the live one without creating cases. Promoting it needs approval.

| Interaction | Call | Status |
|---|---|---|
| Performance | CG-20 `GET /api/ActivationRule/{id}/Performance` | New |
| Backtest | CG-21 `POST /api/ActivationRule/{id}/Backtest` (reuses the `llm-tool-offload` backtest service) | New |
| Shadow / promote | CG-22 Op: `StartShadow` / `PromoteShadow` | New |

**Deliberately not built:**
- a cross-institution fraud consortium, which conflicts with self-hosting;
- agents closing cases on their own. Agents recommend and people execute.

---

---

## 21. Suppressions and lists

**Why:** Lists and Dictionaries are now used far more to build rules than to suppress alerts. Today anyone with the menu item can change them, and the change takes effect within moments, with no version or approval. Rule outcomes then depend on something outside the model's version, so a snapshot cannot replay a past decision, and the audit trail doesn't show why a rule matched.

**The strategy:**
- **Lists and Dictionaries become model artefacts:** versioned, approved, and captured in the model snapshot.
- **Suppression becomes the only override surface:** time-bound, with a required reason, recorded on every transaction it affects, and usable both from a case and on its own.

### 21.1 What Suppression does today

Suppression is keyed by a Request XPath field with Enable Suppression switched on (for example IP or AccountId), and applies either to the whole model or to one activation rule.
- **Still happens while suppressed:** the rule still matches, and TTL counter increments are not suppressed.
- **Stopped:** response elevations (declines), notifications, case creation and the activation watcher.
- **How it's set:** with a switch after Fetch. There is no reason, no expiry, and no record of which matches it stopped.

A separate mechanism shares the word. When an HTTP Adaptation returns an `Error`, its `Value` is null ("suppressed" in the protocol, §4), and rules that read it do not match. **This is model health, not an override.** The UI calls it **"unavailable"** so the two are never confused.

### 21.2 Suppress from an activation

![Suppress lightbox](img/56-S1-suppress-lightbox.png)

On any activation card: **Suppress…** opens a lightbox with these fields:
- **Key:** only the suppression-enabled fields present on this transaction.
- **Scope:** this rule, or the whole model. Whole model needs approval.
- **Duration:** required. 7, 14 or 30 days, or a date. At most 90 days by tenant policy, and never indefinite.
- **Reason:** a category plus text.
- **Evidence:** a note from the case.

"What will happen" is the server Preview:
- what still runs (scoring, archive, matching, TTL counters);
- which four actions stop;
- how many matches in the last 30 days would have been suppressed, and whether any were on cases later confirmed TP;
- when it expires, and the reminder.

**Guardrails:**
- **Never allowed:** floor rules (sanctions).
- **MLRO approval needed:** keys with a pending SAR, or a confirmed TP in the last 12 months.
- **Maker-checker:** model scope, or more than 30 days.

**Other entry points:**
- **Close lightbox:** when the outcome is FP, it offers "Also suppress {rule} for {key}", pre-filled.
- **Agent:** can recommend a suppression, but never create one.

| Interaction | Does | Call | Status |
|---|---|---|---|
| Suppress… | Preview with consequences and the 30-day impact | CG-27 Op: `CreateSuppression` Preview (impact from CG-30) | New |
| Submit | Create; approval if the policy requires it | CG-27 Execute | New (writes today's suppression rows + the new fields) |
| Key options | Suppression-enabled Request XPath fields on this transaction | Existing Request XPath `EnableSuppression` | Exists |

### 21.3 Suppressed and unavailable, as seen in the case

![Suppressed activation and unavailable adaptation](img/57-S2-suppressed-in-case.png)

- **Case key panel:** a "1 active suppression" chip.
- **Activation matched while suppressed:** the card is greyed and labelled "matched · suppressed by SUP-0142", with who, why, until when, and a Revoke button.
- **Journal:** rows affected by a suppression carry the same icon.
- **Unavailable adaptation:** an adaptation that returned an error shows its message and **which rules did not match because of it**.

| Interaction | Call | Status |
|---|---|---|
| Suppressions on this case's keys | CG-32 `GET /api/Case/{id}/Suppressions` | New |
| "matched · suppressed by" on each activation | CG-33 engine stamps `SuppressedBy[{id, version}]` into the archive | New (engine) |
| Revoke | CG-28 Op: `RevokeSuppression` (reason required) | New |
| Adaptation unavailable | Existing `HttpAdaptation.{name}.Error` in `Archive.Json` | Exists |

### 21.4 Suppressions page, with or without a case

![Suppressions page](img/58-S3-suppressions-page.png)

This is the top-level page, replacing the model and rule switch tree. It covers:
- **Search:** by key and value (today's Fetch).
- **Filters:** active, expiring within 7 days, without a case, model scope, pending approval, and legacy with no reason.
- **Detail:** scope, what it does not stop, owner, a history timeline, and Renew, Revoke and Open matches.
- **New suppression:** create one without a case, for example when a customer phones in. The same reason, expiry and guardrails apply.
- **Bulk:** bulk revoke is allowed. Bulk create is not: every suppression gets its own reason.

**Migration:** each existing suppression becomes a row with the reason "Legacy, no reason" and an expiry of migration date + 30 days, and its owner is told. Nothing is left open-ended.

| Interaction | Call | Status |
|---|---|---|
| List and search | CG-26 `GET /api/Suppression?key=&value=&status=&scope=` | Extend (today's Fetch) |
| History | CG-31 `GET /api/Suppression/{id}/History` | New |
| Renew | CG-29 Op: `RenewSuppression` (new reason) | New |
| Expiry reminder | Inbox item 3 days before expiry; `ServiceChange` area `Suppression` | New |

### 21.5 A list is a model artefact

![List change set](img/59-S4-list-change-set.png)

A list or dictionary sits under its model's configuration.
- **Change sets:** edits and CSV uploads create a **draft version** of the whole list, shown as a diff (additions, removals and their source).
- **Test and approve:** a backtest compares the new version with the live one, then it is submitted for approval.
- **Snapshots:** the model snapshot records the list version and hash, so every activation can be replayed exactly.
- **Urgent changes:** **emergency publish** goes live now and must be approved within 24 hours. It shows in Pending changes on the Team view.

**From a case, lists can't be written.**
- "Block identifier" and "Also block beneficiary" (on Refuse payment) become **Propose list change**: a line in the model's draft change set, with the case as its source.
- To stop actions for one customer, use a suppression.

| Interaction | Call | Status |
|---|---|---|
| Change set: draft, diff, CSV | CG-34 `/api/EntityAnalysisModelList/{id}/ChangeSet` (the same for Dictionaries) | New (replaces direct value writes) |
| Propose from a case | CG-35 Op: `ProposeListChange` (replaces Op: `BlockIdentifier`) | New |
| Backtest a version | CG-21 backtest service with `listVersion` | Extend |
| Version in the snapshot | CG-36 model snapshot includes list and dictionary version + hash | New (engine) |

### 21.6 Rules

1. **Expiry is mandatory:** default 14 days, maximum 90 days per tenant, and no indefinite suppressions. Anything longer-lived is a rule-tuning or list problem.
2. **Maker-checker** for model scope or more than 30 days. MLRO approval for keys with a pending SAR or a confirmed TP in the last 12 months. Floor rules can never be suppressed.
3. **No step-up:** a suppression is reversible and time-bound, and approval covers the risky cases.
4. **Tuning signal:** a rule with more than 20 active suppressions is flagged in rule tuning and on the Team view, because suppressing one key at a time hides a badly tuned rule.
5. **Lists live in model configuration,** not the main menu. Customer-service roles keep Suppressions and lose direct list editing.

---

## 22. Configuration

Configuration screens are generated from the DTOs (section 1.2). This section covers the hub navigation and the tables that hold legal limits.

### 22.1 Configuration hub cards

![Hub cards](img/42-D4-config-hub.png)

Cards replace trees at every level. Each card has an icon, name, count, one line on what's inside and one line of state. The breadcrumb is the only hierarchy shown.

### 22.2 Rule tables per market and scheme

Legal limits are data, never code. Each table is edited through the generic configuration form, versioned, published with maker-checker approval, and dated so a change takes effect without a release; work already running keeps the row it started under.
- **Payment hold policy** (section 16): hold ceilings, escalation and payer notices per market and direction.
- **Recall scheme rules** (section 17): recall reasons, windows and response times per payment scheme.
- **Reimbursement schemes** (section 18): claim clocks, cap, excess, receiving-PSP share and exceptions per scheme.
- **Release limits** on each user (section 16).

---

# Part IV · Reference

Endpoints, the build handoff and the regulatory basis.

## 23. Endpoints

| Endpoint | Used by | Status |
|---|---|---|
| `GET /api/Case/{id}/Anchor` | First paint (CH-00) | New |
| `GET /api/Case/{id}/Risk` | Risk chip hover, popover (CP-17, CH-03) | New |
| `GET /api/Case/{id}/Risk/History` | Section 6 (S6-05) | New |
| `GET /api/CaseSearch` + lane, likelihood, band, impact, Priority, `sort=lane,priority` | Cases list (CP-02) | Extend |
| `GET /api/Case/{id}/Summary` + lane and risk chip | Peek (CP-06) | New |
| `GET /api/CaseWorkflowFilter/{id}/RiskMatrix` | Risk matrix header | **Proposed** (add to 22 as CP-18) |
| Op: AssessRisk; Op: ChangePriority (override with reason and expiry) | Popover, bulk bar | New |
| `CaseHub.CaseRiskChanged` | Open pages and list rows | New |
| `GET /api/Case/{id}/PaymentHolds` | Held payment card | New |
| Op: ReleasePayment / RefusePayment / ExtendHold / RequestRelease | Payment lightbox | New |
| `CaseHub.PaymentHoldChanged` | Hold countdown, lane | New |
| `POST /api/Authentication/StepUp/Begin`, `/Complete`; step-up enrolment | Step-up panel, user settings | New |
| `GET /api/Journal` + `tags[]`; `GET /api/Archive/{id}/Card` + tags | Journal Tags column, transaction card | Extend |
| Op: TagTransaction / UntagTransaction (over today's `ArchiveTag` write) | "+ Tag" menu | Extend |
| Configuration: `User.PaymentReleaseLimit`; pack `MaxHold`, `HoldClock`, `OnMaxHold`; workflow status "Hold expiring" | Payment rules | New |

Background, not called by the UI: the case risk worker (4.5.8), `ArchiveScore` , and the case risk model registry and backtest (4.5.9).

### 23.1 Endpoints for holds, recall, claims, support needs, bulk tagging, the read audit and AskJooby

| # | Endpoint | Purpose | Status |
|---|---|---|---|
| CG-40 | `/api/RecallSchemeRule` + `/Resolve` | Recall reasons, windows and response times per scheme | New |
| CG-41 | `GET /api/Case/{id}/Recalls` | Recovery cards (outbound) and recall requests (inbound) | New |
| CG-42 | `POST /api/Recall/{id}/Response` and inbound recall intake | Answers to our recalls; recalls sent to us | New |
| CG-43 | `GET /api/Account/{id}/PayeeCheckStats` | Payee checks answered against an account | New |
| CG-44 | `GET /api/Claim/{caseId}` | Claim panel | New |
| CG-45 | `/api/ReimbursementScheme` + `/Resolve` | Reimbursement rules per scheme | New |
| CG-46 | `GET /api/Claim/Register` | Claim register export | New |
| CG-47 | `/api/Customer/{id}/Vulnerability` (+ `/Reveal`) | Support needs and audited reveal | New |
| CG-48 | `GET /api/Team/Outcomes` | Outcome monitoring split by support needs | New |
| CG-49 | `PUT /api/Invoke/Archive/Tag/Bulk` | Bulk tagging, by ids or by filter | New |
| CG-50 | `POST /api/Case/{id}/ViewReason` | Reason for viewing a Sensitive account | New |
| CG-51 | `GET /api/Team/ViewAnomalies` | Unusual viewing for supervisors | New |
| CG-52 | `POST /api/AskJooby/Bite` | One framed Bite for a subject, cached by facts hash | New |
| CG-53 | `POST /api/AskJooby/Proposal` | Agent proposal as a draft change set on a draft snapshot, with backtest | New |

New operations: CreditInbound, ReturnInbound, NotifyCounterpartyPsp, RequestRecall, ChaseRecall, AnswerRecall, OpenClaim, StopClock, ResumeClock, NotifyReceivingPsp, DecideClaim, RequestContribution, RecordContribution, RecordVulnerability, ReviewVulnerability, TagTransactions. All go through the generic Preview/Execute pipeline (stage 2).

**Build stage:** bulk tagging and the read audit are built in stage 3; inbound holds, recall and payee checks in stage 4; claims and support needs in stage 7, after requests for information, because Stop the clock uses that flow.

Endpoints CG-01 to CG-58 (QA, RFI, agent triage, web research, automation, rule tuning, suppressions, lists, narratives, hold policy) are listed with their contracts in the Vue blueprint, sections 8–9.

---

## 24. Build handoff

**Build from these, in this order:**
1. **This document:** what each screen does, and every interaction with its endpoint.
2. **`skill-vue-blueprint/SKILL.md`:** the Vue 3 blueprint, covering the component tree with every prop and event, composables and stores, states, validation, breakpoints and accessibility, the migration phases, library decisions, MCP setup and the security rules.
3. **`ui-specification-golden-audit.md`:** the code audit, with the full endpoint list (Appendix A) and backend build order (Appendix B).
4. **`jube_case_management_v2_spec_0.11.md`:** the product and technical specification.

**Facts from the code audit that the build relies on:**
- charts go through `Jube.Charting` (`Chart.Column()`, `Chart.Sparkline()`, `Chart.Waterfall()`);
- the AskJooby model is a deployment setting (default `deepseek-v4.1-flash`), so the design describes capability, not a model;
- only the Researcher persona exists today; the Analyst Agent is built first, then the Rule Writer;
- CodeQL and Playwright are new CI work;
- editing a session filter exists; saving one is new (CP-19).

**Stack:** Vue 3 components compiled in CI and checked in as built files, daisyUI 5, Tabulator, Lucide, SignalR and `Jube.Charting`. jQuery QueryBuilder stays, isolated to the rule builder. Jube's own code editor replaces ACE, and all of Kendo is removed.

**Live updates:**
- **Transport:** the existing `ServiceChangeHub` already publishes an event for every service-layer write (`OperationScope`, then `IServiceChangeBus`, then `ServiceChangeRelay`, with the tenant group joined automatically). The client listens with one connection for the whole app.
- **What an event does:** it only says an entity is stale, so it marks the matching TanStack Query entries stale (`[Area, entityId]` and the area's lists), and they refetch in the background.
- **Missed events:** a full refresh runs on reconnect and when a long-hidden tab returns.
- **Polling:** only money-critical views poll, every 30 s, as a safety net.
- **Editing:** an update never overwrites what someone is editing; they see "Changed by X · Reload" instead.
- **Presence:** `CaseHub` is new and carries presence only.
- **Screens and events:** where this document says a screen updates "live" or names a CaseHub push (`CaseRiskChanged`, `PaymentHoldChanged`), it now happens through this mechanism.

**Security (non-negotiable):**
- the JWT is held in a tab-scoped secure store and sent as `Authorization: Bearer`; no auth cookies;
- same origin in development through the Vite proxy;
- no `v-html`, no `unsafe-eval`;
- step-up tokens are single use and bound to the preview for payment decisions.

---

---

## Appendix A. Regulatory basis

The payment and claim screens follow these sources. Values that differ by market are held in the rule tables (section 22), and compliance confirms each market's row.

- PSR, PS25/5 consolidated policy statement on APP scams reimbursement (May 2025): psr.org.uk
- FCA, FG24/6 finalised guidance on payment delays and suspicious inbound payments: fca.org.uk/publication/finalised-guidance/fg24-6.pdf
- EPC, Verification Of Payee scheme rulebook and matching recommendations: europeanpaymentscouncil.eu
- EPC, clarification paper on SCT and SCT Inst rulebooks (recall windows and answers)
- Pay.UK Confirmation of Payee reason codes, as published by participating providers
- DLA Piper and Norton Rose Fulbright on the PSD3/PSR provisional agreement (November 2025)
- FCA, FG21/1 and the March 2025 review of firms' treatment of customers in vulnerable circumstances

---

- FCA, finalised guidance on payment delays (outbound delay up to 4 business days, payer told by the end of the next business day); NIST SP 800-63B-4 on phishing-resistant authentication
