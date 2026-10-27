---
name: jube-vue-port
description: Use for any Vue, daisyUI, Tailwind, Pinia, TanStack Query, Tabulator, SignalR, vue-i18n or Razor/jQuery/Kendo porting work in the Jube repository. Makes the port of Jube Case Management v2 mechanical. Records the decisions already made (D-V1 to D-V29), the component catalogue with every prop and emit, the styling rules, the three-layer state model, the migration phases and the review checklist. Nothing here is up for redesign.
---

# Jube Vue port

Jube is moving from Razor Pages, jQuery and Kendo to one Vue 3 SPA in a single release. The backend is already production-ready. This work surfaces it and makes it safe for agentic use. Anything a person can do in the UI, an agent can do through the same service method, permission check and audit trail.

**The rule:** do not improvise. If a component, prop, emit, endpoint or state is not in these files, stop and ask. Every rule below cites its decision (D-Vn) or endpoint ref so you can trace it.

## Files

| File | Use it for |
|---|---|
| `decisions.md` | D-V1 to D-V29, verbatim. Read first. |
| `components.md` | The component tree and every prop and emit. Plus the figure → component map. |
| `reactivity.md` | Composables, state machines, Pinia stores, TanStack Query + ServiceChangeHub invalidation, narratives (D-V26), bearer-token security (D-V27). |
| `styling.md` | daisyUI 5 theme, Tailwind rules, Kendo → daisyUI table, skeleton sizes. |
| `states-a11y.md` | The ten UI states, validation rules, breakpoints, accessibility, keyboard map. |
| `migration-map.md` | Phases V0–V15 with gates, keep/replace/drop library table, CG-series endpoints. |
| `recipes/` | Complete worked files: a `useQuery` section with invalidation, a lightbox form. |

Source documents sit in `design_handoff_case_management_v2/`: `specs/jube_case_management_v2_spec_0.11.md`, `specs/ui-specification-golden-audit.md` (endpoint refs CP-, CH-, S1–S6-, TC-, MF-, KP-, DC-, LB-, H-, G-), `design/Decision log.md`, `design/Jube v2 - What is being built.md`, `brand/Jube Brand and Style Guide.docx`. The `*.dc.html` prototypes are design references only: never copy their markup or scripts.

## Project shape (D-V1, D-V2, D-V28)

- Vite project at `Jube.App/ClientApp/`. CI builds to `wwwroot/app/`, vendored and served by `Jube.App`. Developers run without Node.
- `MapFallbackToFile("app/index.html")` for every non-API, non-hub path. Vue Router history mode owns navigation. Razor Pages remain only for server endpoints with no UI (OIDC callback).
- `<script setup lang="ts">` everywhere. DTO types generated in CI from Swagger into `src/api/types.gen.ts`. Never hand-write a DTO type.
- Layout: `src/pages/<Name>Page.vue` · `src/components/<Area>/<Name>.vue` · `src/composables/*.ts` · `src/stores/*.ts` · `src/api/{useApi,queryKeys,invalidationMap,types.gen}.ts` · `src/locales/{locale}.json`.
- Dev proxy: `server.proxy` for `/api`, `/caseHub`, `/watcherHub`, `/serviceChangeHub`, `/Account` with `ws: true`. Same origin always; no CORS.

## Lint rules that fail the build

`fetch` outside `useApi` · any `v-html` · `eval` / `new Function` · `localStorage` outside `useTheme` · a cross-origin API base URL · a Tabulator import outside `DataGrid.vue` · a jQuery import outside `RuleBuilderIsland.vue` · component-scoped `<style>` other than `@keyframes`.

CSP: `default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline'; connect-src 'self'; frame-ancestors 'none'`, no `unsafe-eval`, Trusted Types on (D-V1, §7).

## Styling in one paragraph (full rules in `styling.md`)

daisyUI 5 classes and Tailwind utilities only. Theme `jube` and `jube-dark` via `data-theme` on `<html>`; density via `data-density`. `primary` = Jube Navy `#005F7F`, `accent` = Jube Teal `#2FAB9B`, `error` = `#B42318`. Colour carries risk and state only; every state also has a word. IBM Plex Sans for text, IBM Plex Mono with tabular figures for identifiers, amounts and timestamps. Lucide icons, vendored ESM (D-V12). No icon fonts, no PNGs, no emoji.

## State in one paragraph (full rules in `reactivity.md`)

Three layers, never mixed (D-V20): server state in `@tanstack/vue-query` keyed by `queryKeys.ts`; client UI state in Pinia; form and selection state in component-local refs. Live updates are cache invalidation, not data push (D-V3): `useServiceChanges()` singleton on `/serviceChangeHub` invalidates `[area, entityId]` and `[area, 'list']`, then `invalidationMap.ts`. Props down, emits up; components never call `fetch`. URL is the source of truth for section, focus, selection, lens, filter and sort (`useUrlState`). Every operation runs through `useOperation`: Preview → Execute with one idempotency key per open. The JWT lives only in `useTokenStore` (memory + `sessionStorage`), sent as `Authorization: Bearer`; no cookies (D-V27). Generated text is a stateless `POST /api/AskJooby/Narrate` with a registered frame, never chat history (D-V26). Strings come from `src/locales/en.json` through `useI18n` (D-V29).

## Generation contract

1. Pick exactly one component from `components.md`.
2. Write `<script setup lang="ts">` with the listed props and emits, no others, using the composables named for it in `reactivity.md`.
3. Build the template from daisyUI 5 classes per `styling.md`. Confirm syntax with the daisyUI Blueprint MCP; ignore its theme and creative tools.
4. Handle every state in `states-a11y.md` §3.1 that applies, and the §3.4 attributes.
5. Write a Vitest test for each emit's trigger condition, and a Playwright step if the component is in a journey.
6. Anything ambiguous: stop and ask.

## Review checklist

- Props and emits match `components.md` exactly; `withDefaults` for optional props.
- No `fetch`, `v-html`, `eval`, jQuery, Tabulator or `localStorage` outside their one allowed file.
- Server data only in vue-query; UI state only in Pinia; form state only in local refs. No JWT, step-up token, raw payload or revealed masked field in any store or cache.
- Query keys built only in `queryKeys.ts`; area string is the server constant.
- Every applicable §3.1 state handled; skeleton only on `isLoading`, never on `isFetching`.
- Dirty-edit guard present on any editable surface that reads an invalidatable query.
- `role`, `aria-*`, focus trap and return per §3.4. Focus ring `outline-2 outline-offset-2 outline-primary`.
- No colour-only meaning. Contrast 4.5:1 in both themes and densities.
- daisyUI and Tailwind classes only; no custom CSS class, no inline `style` except a runtime-computed value.
- Every user-visible string is a locale key in `en.json`.
- One Vitest test per emit.

## Open questions (carried from the handoff)

- Default AI model for AskJooby: Phi-4 Mini or deepseek-v4.1-flash. Tenant setting; the UI only reads it.
- Similar cases (pgvector + full-text hybrid): drawn screen or text-only in this release.
- Exact login route behind today's `Pages/Account/Login`; confirm in phase V2.
- Area constants for `ServiceChangeEvent.Area`: read them from the server before writing `queryKeys.ts`.
