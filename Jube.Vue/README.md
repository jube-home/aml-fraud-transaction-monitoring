# Jube.Vue

The Vue port of the Jube user interface. Design, decisions and migration order live in `.claude/skills/jube-vue-port/SKILL.md`.

Phase 0 (scaffolding) is in place: Vue 3.6 release candidate with Vapor, Vite, Vue Router, Pinia, TanStack Query, vue-i18n, Tailwind with daisyUI, Tabulator, Lucide, Vitest and Playwright. Three example pages (`/`, `/grid`, `/api-demo`) and a placeholder `/login` exist only to prove the scaffolding.

## Commands

| Command | Does |
|---|---|
| `npm install` | Installs dependencies (`.npmrc` sets `legacy-peer-deps`, see the skill's Decisions log) |
| `npm run dev` | Dev server on http://localhost:5173, proxying `/api/` to `JUBE_BACKEND` (default `http://127.0.0.1:5001`) |
| `npm run test:unit` | Vitest, once |
| `npm run test:e2e` | Playwright (Chromium), API calls stubbed, starts the dev server itself; first run needs `npx playwright install chromium` |
| `npm run ci` | Format check, lint, type check, unit tests and build, as CI does |
| `npm run api:types` | Generates `src/api/schema.d.ts` from the backend's OpenAPI document (`JUBE_OPENAPI` overrides the URL; the backend must be running and the request authenticated) |

## Layout

`src/api` (`useApi`, query client and keys), `src/components`, `src/composables`, `src/i18n`, `src/locales`, `src/pages`, `src/router`, `src/stores`, `src/test-support` (Vapor mount helper for Vitest), `e2e`.
