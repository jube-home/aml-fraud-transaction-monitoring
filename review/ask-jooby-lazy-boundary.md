# Ask Jooby lazy boundary (shell-auth finding 16)

## What changed

- `Jube.App/Vue/src/askjooby/pageContext.ts` moved (content unchanged, Pinia id `joobyContext` unchanged) to
  `Jube.App/Vue/src/stores/joobyContext.ts`. It has no imports beyond vue and pinia, so the shell can use it without
  touching the askjooby tree.
- `AppLayout.vue`: `JoobyLauncher` and `JoobyWidget` are now `defineAsyncComponent` (silent `onError`, no loading or
  error component). When `showWidget` first becomes true (immediate watch), both loaders are called from
  `requestIdleCallback` (fallback `setTimeout 0`) so the chunks are warm before the first click. Failures are swallowed.
- Importers repointed: `AppLayout.vue`, `pages/OverridePage.vue`, `pages/AskJoobyPage.vue`,
  `components/askjooby/JoobyWidget.vue`.

## Import-graph proof (static, scripted over relative imports)

- `LoginPage.vue` transitive static closure: 21 files, zero paths containing askjooby, no `deep-chat`. Bare packages
  reached: @msgpack/msgpack, @tanstack/vue-query, lucide-vue-next, lz4js, pinia, vue, vue-i18n, vue-router.
- `main.ts` closure: 22 files, zero askjooby, no `deep-chat` (router uses dynamic imports for LoginPage and AppLayout).
- Answer: the authentication page did NOT pay for Ask Jooby before this change, by static graph. Finding 16's "login
  page's lazy chain" claim was wrong for the login route; the cost was on every authenticated page (AppLayout chunk).

## Bundle effect (scratch outDir, `dist` untouched, no other build or dev server running)

- Before: widget tree and chat stack statically in the AppLayout chunk (per finding 16).
- After: `AppLayout` 22.71 kB (gzip 8.51); `JoobyLauncher` 1.18 kB; `JoobyWidget` 2.35 kB; `deep-chat` appears in
  exactly one chunk, `useJooby-*.js` (508 kB), reached only via the dynamic `JoobyWidget` import (and the lazy ask-jooby
  page). `AppLayout` and `index` contain no deep-chat reference. Entry `index` 24.93 kB. The AppLayout chunk has a
  mapDeps preload list for the widget chunk only inside the dynamic import.
- I did not capture a before build, so there is no measured before figure.

## Verification

prettier, eslint (touched files) clean; `vue-tsc --build` clean; vitest `src/askjooby src/components` 11 files / 132
tests pass. No unit test imported the moved store.

## E2E specs at risk (not run)

- `e2e/askjooby.spec.ts` "Ask Jooby widget" tests (lines 345-395): use `widget-launcher` / `widget-panel` after
  `page.goto`; locators auto-wait so should pass, but first render is now async.
- `e2e/askjooby.spec.ts:388` "not offered on the Ask Jooby page": `toHaveCount(0)` can now pass vacuously before any
  load; weak but still correct (showWidget is false there).
- `e2e/shell.spec.ts:321`: `toBeVisible` on `widget-launcher`, auto-retries.
- `e2e/askjooby.spec.ts:350-352`: `expect(panel).toBeHidden()` before click is unaffected.

## Files touched

- `Jube.App/Vue/src/components/AppLayout.vue` (shell-auth)
- `Jube.App/Vue/src/stores/joobyContext.ts` (moved from `src/askjooby/pageContext.ts`; ask-jooby slice file, reviewer
  inherits the move; delete of old path is unstaged)
- `Jube.App/Vue/src/pages/OverridePage.vue`, `Jube.App/Vue/src/pages/AskJoobyPage.vue` (import path only)
- `Jube.App/Vue/src/components/askjooby/JoobyWidget.vue` (import path only; ask-jooby slice file, reviewer inherits)
