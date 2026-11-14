# Slice review: watcher

Agent-mode review, 2026-11-08 (resumed after a session restart; nothing had been edited before the restart). Steps 0, 2,
3, 4, 5. Nothing built; no dotnet, Playwright or DB test run. npm: prettier, eslint, vue-tsc and vitest run (watcher
specs 45 pass; the full run's only failures are `locales.spec.ts` "references only keys that exist in en", which now
also lists the 5 new `watcher.popup.*` keys until the integration pass adds them, and `signIn.spec.ts`, triaged and
fixed in section 12).

## 1. Summary and verdict

Verdict: READY AFTER OWNER ACTIONS. The stream was already well built (session cookie auth, tenant resolved server-side,
no client-supplied group or id, per-tenant dispatch, bounded drop-oldest queues, sweeper integration). Fixed this pass:

1. REMOTE ASSET: the map loaded its style, tiles and sprites from `tiles.openfreemap.org` by default and a CSP migration
   opened `img-src`/`connect-src` to openfreemap and `*.tile.openstreetmap.org`. Now the default is a bundled world
   outline with zero remote requests; a remote style is opt-in (build-time https URL AND a CSP edit), falls back to the
   outline when blocked.
2. Stream: permission and tenant were only checked on connect; now rechecked every 60 s. No per-user connection cap (now
   10, 429). SSE frame injection through line breaks and unbounded message size (now split per line, 64 KiB cap). A
   `Task.Delay` leaked per message (one 15 s timer per event); fixed with a per-iteration linked token.
3. Client: a 403/429 made EventSource give up while the UI said "connecting" forever; now reopens with 5 to 60 s
   back-off. Undrawn queue was unbounded (cap 20000).
4. Hard-coded English in map popups moved to `t()`.
5. `ActivationWatcherService` constructed its repository with a synchronous DB lookup that defaulted to tenant 0; it now
   takes the already resolved tenant id.

## 2. Files reviewed (first-touch pull-ins)

Read whole: `Jube.App/Vue/src/watcher/*` (geojson, geo, mapStyle, pageState, parse, rings, source, state, stream, types,
useWatcher), `pages/WatcherPage.vue`, `components/ActivationWatcher.vue`, `components/ActivationMap.vue`,
`Jube.App/Endpoints/Query/WatcherStreamEndpoints.cs`, `Jube.App/Endpoints/Repository/ActivationWatcherEndpoints.cs`,
`Jube.App/Code/Watcher/WatcherStreamRegistry.cs`, `Jube.App/Code/Sse/SseConnectionRegistry.cs` and
`SseConnectionEntry.cs` (inherited from shell-auth), `RevokedSessionSweeper.cs` (inherited),
`Jube.Service/Query/WatcherStream/*`, `Jube.Service/Repository/ActivationWatcher/*`,
`Jube.Dto/Repository/ActivationWatcher/ActivationWatcherDto.cs`, `Jube.Data/Repository/ActivationWatcherRepository.cs`,
`Jube.Cache/Resp/CacheActivationWatcherPublishSubscribe.cs`, `CacheActivationWatcherLastLocationRepository.cs`,
`ActivationRuleActivationWatcherExtensions.cs` (engine publisher), `Jube.Migrations/Branches/WatcherMap/*`,
`Jube.Resources/ActivationWatcherResources*`, `WatcherStreamResources*`, `Jube.App/Pages/Watcher/Activation.cshtml.cs`,
`wwwroot/js/Watcher/Activation.js` (grep), `WatcherSummaryChartRequest`, `ChartsEndpoints` watcher route,
`WatcherSummaryChartRenderer` limits. Tests read: listed in section 5. Closure pull-ins (DataGrid, JubeChart, useFormat,
i18n/index.ts, en.json) are inherited, not re-reviewed.

Claims verified in code: no `SelectToken` in any slice file; no `|| !TenantRegistryId.HasValue` pattern in
`ActivationWatcherRepository`; lat/long come from the transaction payload (Request XPath DataTypeId 6/7 or inline script
attributes) not from Svirfd, so the watcher has no IP-intelligence geodata (Svirfd is the model-network page); the Redis
publisher is the engine (`StreamingActivationWatcher`, tenant id from the model instance), the app subscribes to
`ActivationWatcher:*` and parses the tenant from the channel name.

## 3. Findings

| Sev                        | Where                                                                   | What                                                                                                                            | Status                                                                             |
|----------------------------|-------------------------------------------------------------------------|---------------------------------------------------------------------------------------------------------------------------------|------------------------------------------------------------------------------------|
| High (privacy, owner rule) | `watcher/mapStyle.ts`                                                   | default style `https://tiles.openfreemap.org/styles/positron\|dark`: viewer IP and viewed area leak; also sprites/glyphs remote | FIXED: bundled `world.json` outline style, no glyphs/sprite                        |
| High                       | `Branches/WatcherMap/RemoveRemoteTileHostsFromContentSecurityPolicy.cs` | CSP allowed openfreemap and OSM hosts                                                                                           | FIXED: names no remote host (test added)                                           |
| Med                        | `WatcherStreamEndpoints.cs`                                             | permission/tenant only checked on connect, so a revoked permission or a landlord tenant switch kept streaming the old tenant    | FIXED: recheck every 60 s via `StillAuthorisedAsync`, final `revoked` event        |
| Med                        | same                                                                    | no per-user open stream cap                                                                                                     | FIXED: 10, 429                                                                     |
| Med                        | same                                                                    | `data: {payload}` unsplit: a payload with line breaks could forge fields/events; no size cap                                    | FIXED: `WatcherStreamFrame`                                                        |
| Low                        | same                                                                    | one uncancelled `Task.Delay(15 s)` and an overlapping `WaitToReadAsync` per loop iteration                                      | FIXED                                                                              |
| Med                        | `watcher/stream.ts`                                                     | EventSource closed by 403/429/network policy was shown as "connecting" forever                                                  | FIXED: reopen with back-off                                                        |
| Low                        | `watcher/useWatcher.ts`                                                 | undrawn queue unbounded, copied on every push                                                                                   | FIXED: cap 20000 keeping the newest                                                |
| Low                        | `ActivationMap.vue`                                                     | hard-coded English popup text ("Response elevation", "an instant", "km/h", "Impossible travel")                                 | FIXED via `MapText` and `t()` with English defaults                                |
| Low                        | `ActivationWatcherRepository.cs`, service                               | repository looked tenant up synchronously and defaulted to 0; tenantless ctor would query `IS NULL`                             | FIXED: explicit tenant id; tenantless instance returns empty from the replay query |
| Low                        | `ChartsEndpoints` watcher summary                                       | RequireAuthorization only, no permission 30; pure render of posted data (max 100 rules, 60 chars), no leak                      | LEFT, shared TODO                                                                  |
| Low                        | `WatcherSummaryChartRenderer`                                           | series names hard-coded English                                                                                                 | LEFT, shared TODO (needs compiler)                                                 |
| Info (design)              | stream payload                                                          | key value (account or card number) and coordinates streamed to every permission 30 user                                         | DESIGN: masking policy is the owner's; documented                                  |
| Info                       | `SseConnectionRegistry.Tracked()`                                       | allocates per call                                                                                                              | shared TODO                                                                        |
| Info                       | Razor `/Watcher/Activation`                                             | loads OSM tiles                                                                                                                 | shared TODO (now blocked by CSP)                                                   |
| Info                       | cluster count labels                                                    | absent offline (no bundled font)                                                                                                | accepted, documented                                                               |

Reviewed and fine: marker popups use `textContent` (no HTML injection); swatch colours pass `safeColour` hex check;
coordinates validated for finiteness and range, 0,0 treated as unlocated; ids dedupe across reconnects; no group or
tenant parameter exists to forge; `Replay` bounded to 10000 rows and tenant-scoped; hostile query strings covered by
existing pen tests.

## 4. Fixes made

Code: `mapStyle.ts`, `world.json` (generated from `Jube.Charting/Maps/world-110m.txt` by scratch script),
`ActivationMap.vue`, `ActivationWatcher.vue`, `types.ts`, `stream.ts`, `useWatcher.ts`; `WatcherStreamEndpoints.cs`,
`WatcherStreamFrame.cs` (new), `WatcherStreamAuthorizationService.cs`, `ActivationWatcherRepository.cs`,
`ActivationWatcherService.cs`, `RemoveRemoteTileHostsFromContentSecurityPolicy.cs`. Resources: 14 new `.resx`.

## 5. Test and pen-test matrix

| Area                                                   | Existing (read)                                                                                                               | Added                                                                                                              |
|--------------------------------------------------------|-------------------------------------------------------------------------------------------------------------------------------|--------------------------------------------------------------------------------------------------------------------|
| Stream auth 401/403/no tenant/cross-site/hostile query | `Security/Surface/PenTestWatcherStream.cs` (4 tests)                                                                          | none needed                                                                                                        |
| Replay perms, tenant isolation, ranges, verbs          | `Security/Case/PenTestActivationWatcher.cs` (6), `Service/Repository/ActivationWatcher/ActivationWatcherServiceTests.cs` (11) | none (repository change is covered by the tenant tests)                                                            |
| Stream authorisation service                           | `WatcherStreamAuthorizationServiceTests.cs` (6)                                                                               | 3: still authorised true, other tenant false, no permission/unknown false                                          |
| Registry per-tenant dispatch, drop-oldest, abort       | none                                                                                                                          | `Jube.Tests/App/Code/Watcher/WatcherStreamRegistryTests.cs` (4, Unit)                                              |
| SSE frame injection and size cap                       | none                                                                                                                          | `.../WatcherStreamFrameTests.cs` (4 methods, 5 cases, Unit)                                                        |
| CSP migration                                          | `RemoveRemoteTileHostsFromContentSecurityPolicyMigrationTests.cs` (3)                                                         | updated 1, added 1 (no remote host)                                                                                |
| Engine publisher                                       | `ActivationRuleActivationWatcherExtensionsTests`, `...GeolocationTests`, `CacheActivationWatcherLastLocationRepositoryTests`  | none                                                                                                               |
| Front end                                              | vitest `stream`, `source`, `pageState`, `watcher` specs; Playwright `watcher.spec.ts`, `watcherPage.spec.ts`                  | vitest: stream reconnect (2), `useWatcher` backpressure (1), `mapStyle` (4); Playwright: no foreign host requested |

Docs catalogue updated: `docs/Testing/AppAndInfrastructure.md`, `ServiceQueries.md`, `DataAccessAndMigrations.md`.

Dotnet tests to run: `Jube.Test.App.Code.Watcher.*` (Unit),
`Jube.Test.Migrations.RemoveRemoteTileHostsFromContentSecurityPolicyMigrationTests` (Unit),
`WatcherStreamAuthorizationServiceTests` and `ActivationWatcherServiceTests` (Service, Database), then the serial
`PenTestWatcherStream` and `PenTestActivationWatcher`. Playwright: `watcher.spec.ts`, `watcherPage.spec.ts`.

## 6. Translations

Server: `ActivationWatcherResources` and `WatcherStreamResources` now have pl, ar, ka, id, sv, ur, hi (14 files; two
keys each, identical key set to neutral by construction, no `{0}` placeholders). `.fr.resx` untouched. SPA: en, pl, ar
had no gaps in the 68 owned keys; 5 new keys `watcher.popup.elevation|travel|travelInstant|impossible|plausible`
proposed for all eight locales in `review/watcher.locale-proposals.json` (flat dotted keys). Code carries English
defaults so the UI is right before integration. RTL: popup text is plain text in a LTR-agnostic div, numbers in `{}`
slots; map controls are the library's. Font stack for Mkhedruli/Devanagari/Nastaliq not checked here (shell-auth).

## 7. Docs written and TODO markers

`docs/Configuration/Models/ActivationWatcher/index.md`: every old Razor-era image now has a `TODO(screenshot)` marker
above it with a new `watcher-*.png` name, a top `TODO(docs)` flags the stale Razor text, and a new section covers the
offline map, how to opt in (build-time `VITE_JUBE_MAP_STYLE_LIGHT/DARK` https URL plus the CSP row), the stream's tenant
binding, recheck, limits and what fields are streamed. 23 markers, lines 10 to 134 (grep `TODO(`).

## 8. Shared-code TODOs raised

Five lines appended to `review/SHARED-TODO.md` (locale keys, legacy Razor page, superseded seed CSP migrations, chart
labels and permission, registry count and ServiceChange stream parity).

## 9. Manual test sheet

Permission 30 granted and refused (page shows refusal). Open `/watcher` with devtools Network: no request leaves the
origin. Stream status shows open, then toggle Real time off and on. Send activations for two tenants: only own tenant's
pins appear. Remove permission 30 while open: stream closes within about a minute. Sign out in another tab: closes at
next sweep. Open 11 tabs: the 11th shows connecting and retries. Hostile key value (`<img onerror>`, very long, unicode)
appears as text in popup, feed and chip. Hover pin and trail popups in en, pl, ar (RTL). Many markers (1000+):
clustering, heat layer, fade. Replay a range and a single entity journey. Narrow width, dark theme (map switches style),
keyboard only (Locate buttons, Fit).

## 10. Commit pathspecs

Slice paths from `slice_audit.py watcher --paths`, plus: `Jube.App/Endpoints/Query/WatcherStreamEndpoints.cs`,
`Jube.App/Code/Watcher/*`, `Jube.Service/Query/WatcherStream/*`, `Jube.Service/Repository/ActivationWatcher/*`,
`Jube.Data/Repository/ActivationWatcherRepository.cs`, `Jube.Migrations/Branches/WatcherMap/*`,
`Jube.Resources/ActivationWatcherResources*`, `WatcherStreamResources*`, `Jube.Tests/App/Code/Watcher/*`, the two
Service/migration test files, the three `docs/Testing` hunks, `Jube.App/Vue/src/watcher/*`. Hunks only for `slices.json`
(owner).

## 11. Not covered

No live stream pen test reading events across two tenants (the unit test covers the registry only). Rider lint: clean
for the five production C# files except allocation hints and one naming fix applied after (not re-linted); test files
`WatcherStreamRegistryTests` and `WatcherStreamFrameTests` clean at error level;
`WatcherStreamAuthorizationServiceTests`, `RemoveRemoteTileHostsFromContentSecurityPolicy*` timed out and are unlinted.
Playwright not run. Font coverage for non-Latin scripts, cluster-count labels offline, and key-value masking policy are
owner items. `Activation.js` legacy page not reviewed beyond the tile URL. Unchecked: whether `RevokedSessionSweeper`
closes streams of deleted users (inherited, `sessionStates` miss means not refused).

## 12. Follow-ups (owner policy: bugs get fixed)

1. CSP migration, exact end state.
   `Jube.Migrations/Branches/WatcherMap/RemoveRemoteTileHostsFromContentSecurityPolicy.cs` (renamed from
   AllowMapTilesInContentSecurityPolicy; unreleased, new on the branch, so editing and renaming was allowed; empty
   `Down()` kept; test class renamed to match). The two older migrations that seed and then harden the CSP
   (`PenTestGbkRemediation/SeedDefaultHttpResponseHeaders`,
   `ZapWarnNewHardening/HardenSiteIsolationAndContentSecurityPolicy`) exist on master, are left untouched and still
   write `img-src 'self' data: https://*.tile.openstreetmap.org`. The WatcherMap migration then runs
   `UPDATE ... WHERE Header = 'Content-Security-Policy' AND Value = <the exact Zap policy>`. Result: a fresh database,
   and a deployed database still on the stock policy, end with
   `img-src 'self' data: blob:; connect-src 'self'; worker-src 'self' blob:; child-src blob:` and no remote host. A
   deployed database whose CSP row an operator edited is deliberately left as is (the WHERE does not match), so such an
   operator keeps whatever hosts they set. Opting in later is a row edit, documented in the docs page. Tests:
   `RemoveRemoteTileHostsFromContentSecurityPolicyMigrationTests` (4, Unit); the SQL builder tests assert the stock
   value is the match condition.
2. Legacy Razor watcher: Razor pages are still mapped (`MapRazorPages`) and linked from `_Layout.cshtml`, so the page
   cannot be deleted by this slice. `Jube.App/wwwroot/js/Watcher/Activation.js` no longer asks OpenStreetMap for tiles
   (tile layer and root `urlTemplate` removed, `layers: []`), so it makes no remote request and draws markers on a blank
   canvas. Deleting the page stays with the Razor retirement (shared TODO).
3. `signIn.spec.ts` timeout. Pre-existing, not a regression from the shell lazy Ask Jooby widget, the locale
   registration or `useFormat`: the spec file is unchanged on this branch, it passes alone (about 3.2 s over two
   repeats), and its assertions are deterministic. The "walks credentials..." test ends by navigating the real router to
   `/tools`, which cold-imports `AppLayout.vue`, `ToolsPage.vue` and then the lazily warmed Jooby chunks; measured about
   2 to 5 s alone and more when 160 files compete for CPU. The test had the default 5 s test timeout but an inner 10 s
   wait, an inconsistent pair. Fix: test timeout 60 s and inner wait 45 s (`src/__tests__/signIn.spec.ts`, inherited
   shell-auth file, edited surgically; no agent was in it). Three full vitest runs after the fix: only the locale spec
   fails (a first run also showed three files failing to load, a transient not reproduced). Also moved the `mapStyle`
   import in `ActivationMap.vue` to a dynamic import so the 250 KB world outline is only fetched when a map mounts.
4. Chart series labels: nothing hard-coded in watcher-owned client code (`ActivationWatcher.vue` sends only rules and
   colours). The English series names live in the server SVG renderer and stay a plan item (shared TODO). Design
   decision unchanged: masking of key values and coordinates in the stream.

Tests to run: unchanged list plus `RemoveRemoteTileHostsFromContentSecurityPolicyMigrationTests` (Unit; renamed). Vitest
locale spec stays red until the integration pass adds the `watcher.popup.*` keys (and other agents' keys).
