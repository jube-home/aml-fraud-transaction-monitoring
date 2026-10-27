# Prompt: create the `jube-vue-port` skill

Copy everything below the line into Claude Code, run from the Jube repository root with the `design_handoff_case_management_v2/` folder present.

---

You are writing a Claude Code skill called **`jube-vue-port`**. Its job is to make the port of Jube's UI from Razor Pages, jQuery and Kendo to a single Vue 3 SPA mechanical: a developer or agent should be able to pick any page or component, follow the skill, and produce code that is indistinguishable from code produced by anyone else following it. The decisions are already made. The skill records them so they are never re-litigated.

## Read first, in this order

1. `design_handoff_case_management_v2/skill-vue-blueprint/SKILL.md` — the architecture blueprint. D-V1 to D-V29 in §0 are the binding decisions. §1 is the component tree with props and emits. §2 is composables, stores, TanStack Query and ServiceChangeHub invalidation. §3 is UI states, validation, breakpoints and accessibility. §5 is the library keep/drop table. §7 is the bearer-token security posture.
2. `design_handoff_case_management_v2/design/Decision log.md` — every design decision with its endpoint and interaction links.
3. `design_handoff_case_management_v2/design/Jube v2 - What is being built.md` — the design document (22 sections, 185 endpoints). Use it for intent and vocabulary, not for architecture.
4. `design_handoff_case_management_v2/specs/jube_case_management_v2_spec_0.11.md` and `specs/ui-specification-golden-audit.md` — endpoint references (CP-, CH-, S1–S6-, TC-, MF-, KP-, DC-, LB-, H-, G-, CG-) and the Razor → Vue file map.
5. `design_handoff_case_management_v2/brand/Jube Brand and Style Guide.docx` — Jube Navy, Jube Teal, IBM Plex Sans, voice rules.
6. The prototypes (`*.dc.html` in the folder) — **design references only**. They load daisyUI, Tailwind and Tabulator from CDNs with a different component runtime. Never copy their markup or scripts into the skill.
7. The current app: `Jube.App/Pages/**`, `Jube.App/wwwroot/js/**`, `Jube.App/Startup.cs`, `Jube.Service/Observability/OperationScope.cs`, the `[FormEndpoint]`/`[FormGroup]`/`[FormField]`/`[ListColumn]` attributes and the DTOs they decorate. Confirm every claim in the blueprint against the code; where they disagree, record the disagreement in the skill under "Open questions" rather than guessing.

Do not extend, redesign or improve any decision. If something is missing, write it under "Open questions" and stop.

## What the skill must contain

Write it as `.claude/skills/jube-vue-port/SKILL.md` with YAML frontmatter (`name`, `description` that triggers on any Vue, daisyUI, Tailwind, Pinia, TanStack Query, Tabulator, SignalR or port/migration work in this repository). Split large reference material into sibling files under the same folder (`styling.md`, `reactivity.md`, `components.md`, `migration-map.md`, `recipes/`) and link them from `SKILL.md`. Keep `SKILL.md` itself under 400 lines; it is the index and the rules.

### 1. Toolchain and project shape
- Vite project at `Jube.App/ClientApp/`; CI builds to `wwwroot/app/`; the built bundle is vendored and served by `Jube.App`; developers run without Node (D-V1). SPA fallback `MapFallbackToFile("app/index.html")`; Vue Router history mode owns all navigation; Razor Pages remain only for server endpoints with no UI (D-V28).
- `<script setup lang="ts">` everywhere. DTO types generated in CI from Swagger into `src/api/types.gen.ts`, never hand-written (D-V2).
- Folder layout: `src/pages/<Name>Page.vue`, `src/components/<Area>/<Name>.vue`, `src/composables/*.ts`, `src/stores/*.ts`, `src/api/{useApi,queryKeys,invalidationMap,types.gen}.ts`, `src/locales/{locale}.json`.
- Lint rules that fail the build: `fetch` outside `useApi`; any `v-html`; `eval`/`new Function`; `localStorage` outside `useTheme`; a cross-origin API base URL; a Tabulator import outside `DataGrid.vue`; a jQuery import outside `RuleBuilderIsland.vue`.
- CSP without `unsafe-eval`; Trusted Types; same origin always; Vite `server.proxy` for `/api`, `/caseHub`, `/watcherHub`, `/serviceChangeHub`, `/Account` with `ws: true`.

### 2. Styling: daisyUI 5 and Tailwind
- **daisyUI 5 classes and Tailwind utilities only.** No component-scoped CSS except `@keyframes`. No custom CSS classes, no CSS modules, no inline `style` except for values computed at runtime (a live width, a chart height).
- The Jube theme as a daisyUI theme definition: name the semantic tokens (`primary` = Jube Navy, `secondary`/`accent` = Jube Teal, `base-*`, `neutral`, `info`, `success`, `warning`, `error`) with the exact hex values from the brand guide, light and dark variants. Theme via `data-theme` on `<html>`; density via `data-density` (`comfortable` | `compact`, compact minimum 12 px text).
- Colour rule: colour carries **risk and state only**. The neutral palette is for Merge lanes. Everything else is `base-content` on `base-100/200/300`. Colour never carries meaning alone: every chip, badge and state has a word.
- Font: IBM Plex Sans via the Tailwind `fontFamily` config; the numeric variant for money, counts and timestamps.
- A daisyUI component mapping table for every current Kendo widget, taken from blueprint §5 (Grid → Tabulator via `DataGrid`; Window → `modal` on `<dialog>`; TabStrip → `tabs`; Notification → `toast`+`alert`; DropDownList → `select`; searchable → `dropdown`+`input`; DatePicker → native `<input type="date">` styled with `input`; NumericTextBox/Switch/Slider → `input`/`toggle`/`range`; barrating → `rating`; TreeView → `HubCard`+`Breadcrumbs`; ListBox → `RoleAllocationField`).
- The ten UI-state patterns from blueprint §3.1 (loading after 150 ms, empty, failed with `ref`, stale "as at", restricted, offline, case changed, graph over cap, model not explainable, payment outcome unknown) each with its exact daisyUI markup.
- Breakpoints from §3.3 (Desk ≥ 1280; Read-and-act < 1280; team wall fixed 1920×1080).
- Accessibility from §3.4: landmarks, `role="dialog"` rules, focus trap and return, live-region rules, focus ring `outline-2 outline-offset-2` in `primary`, 4.5:1 contrast in both themes and densities, the single keyboard map.
- Icons: Lucide, vendored ESM, tree-shaken (D-V12). No icon fonts, no PNG icons, no emoji.
- Instruct the agent to confirm daisyUI syntax through the **daisyUI Blueprint MCP** and to ignore its theme and creative tools; the Jube theme in the skill wins.

### 3. Reactivity and state
- **Three layers, never mixed:** server state in TanStack Query for Vue (`@tanstack/vue-query`) keyed by `queryKeys.ts`; client UI state in Pinia; form and selection state in component-local refs. Server data never goes in Pinia (D-V20).
- Live updates are **cache invalidation, not data push**: `useServiceChanges()` singleton on `/serviceChangeHub`, dedupe by version and `correlationId`, 250 ms batch, `invalidateQueries([area, entityId])` and `[area, 'list']`, then `invalidationMap.ts`. Reproduce the dependency map, the reconnect/visibility full-invalidation rules, the 30 s `refetchInterval` for money-critical views as the only polling, the QueryClient defaults (`staleTime 30_000`, `gcTime 300_000`, `retry 2` for queries, mutations never retried, `keepPreviousData` for grids), and the dirty-edit guard ("Changed by {actor} at {at} · Reload · Keep editing"). Presence only on `CaseHub`; `WatcherHub` unchanged.
- URL is the source of truth for section, focus key, selection, lens, filter and sort via `useUrlState` (`router.replace` for UI state, `router.push` for navigation).
- Props down, emits up. `defineProps<T>()` with `withDefaults`; `defineEmits<{...}>()` with every emit listed in blueprint §1. Components never call `fetch`; all I/O through a composable. `v-model` only through `update:modelValue` / `update:<prop>`.
- The operation pattern: `useOperation` state machine `closed → editing ⇄ previewing → (stepUp →) executing → result(confirmed|unknown|failed)`, consequence inputs re-preview (300 ms debounce, abort in flight), one idempotency key per open reused on every retry, `StepUpRequiredError` / `ConcurrencyError` / `OutcomeUnknownError` handling, undo toast.
- `useApi` contract in full (blueprint §2.1): same-origin `/api/...`, `credentials: 'omit'`, `Authorization: Bearer`, `X-Refreshed-Token` rotation, `Idempotency-Key`, `X-Step-Up-Token`, the error mapping for 400/401/409/timeout. `useTokenStore` is the only place the JWT is held: memory mirrored to `sessionStorage`, never `localStorage`, never cross-tab, never decoded (D-V27).
- The generic renderer: `useDtoForm` from `GET /api/{entity}/Schema` (CG-14), `[VisibleWhen]/[EnabledWhen]/[RequiredWhen]` evaluated client-side with the server's `ConditionOp` semantics, server authoritative; `FormFieldRenderer` with one exhaustive switch on `colour`-spelled widget names (D-V4).
- `useI18n` wrapping vue-i18n with lazy-loaded `src/locales/{locale}.json`, `en.json` as source and type reference, ICU plurals, `Intl` for dates/numbers/money, CI check for missing and unused keys (D-V29). Tenant-configured labels come from the API as data.
- Narratives: every generated text is a stateless `POST /api/AskJooby/Narrate` with a registered frame; never chat history, never client prompt text; "Drafted by Jooby · {frame} v{n} · from {k} facts" provenance line; never regenerate over edited text (D-V26).
- Agent parity rule: anything a person can do through the UI goes through the same service method, permission check and audit trail an agent uses. No UI-only shortcuts.

### 4. Component catalogue
Reproduce blueprint §1 as `components.md`: the tree, and for each component its file path, props (name, type, required, default, purpose) and emits (event, payload, trigger). Mark M1–M6 and phase-2 items. State the rule: **if a component, prop, emit, endpoint or state is not in the catalogue, stop and ask.**

### 5. Migration map
Reproduce blueprint §4 as `migration-map.md`: phases V0–V15 with gates, and for every current Razor page and `wwwroot/js` file the Vue page or component that replaces it and the phase it is deleted in. Add a row per current Kendo widget instance you find in the code with its replacement. Note the cut-over rule: one release, feature flag on the same origin, no route-by-route coexistence, no Kendo inside Vue, `RuleBuilderIsland` is the only jQuery consumer.

### 6. Recipes (`recipes/`)
One worked file each, complete and compilable, following every rule above, with its Vitest test:
- a read-only grid page (`DtoGridPage` on one `[FormEndpoint]` DTO);
- a DTO form page with conditional fields and maker-checker;
- a `useQuery` section with ServiceChange invalidation and the dirty-edit guard;
- an operation lightbox form (`CloseForm` in `OperationLightbox`);
- a Tabulator `DataGrid` wrapper usage with density, selection and in-cell actions;
- a `JubeChart` mount (server SVG, `role="img"`, table fallback);
- a locale key addition end to end.

### 7. Generation contract and review checklist
The six-step contract from blueprint §6, then a review checklist a reviewer can run line by line: lint rules, state-layer placement, every emit tested, every §3.1 state handled, accessibility attributes, no colour-only meaning, no `v-html`, no token leakage, daisyUI classes only.

### 8. Open questions
Anything you could not confirm against the code, with the file and line that raised it. Known items to carry forward: default AI model (Phi-4 Mini vs deepseek-v4.1-flash); whether Similar cases gets a drawn screen; exact login route behind `Pages/Account/Login`.

## Style of the skill
Jube voice: plain, exact, no marketing, British spelling (`colour`, `localisation`). Short declarative rules. Tables for mappings. Code blocks only where exact syntax matters. No emoji. Cite the decision id (D-Vn) or endpoint ref next to every rule so a reader can trace it back.

When finished, list the files you created and the open questions, and stop.
