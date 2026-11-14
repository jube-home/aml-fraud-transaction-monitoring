# Slice review: model-network

Agent-mode review, 2026-11-08. Steps 0, 2, 3, 4, 5. Nothing built, no .NET or DB test run. Prettier, eslint,
`vue-tsc --build` and vitest (`src/svirfd`, 10 pass) were run on the one Vue file touched. Scratch files lived in
`scratchpad/model-network/`.

## 1. Summary and verdict

Verdict: READY AFTER THE OWNER MANUAL TEST AND THE SERIAL TEST RUN. The platform agent's fixes were re-read and are
correct; one gap in the edge loader and three Svirfd privacy gaps were found and fixed.

- Graph recall is tenant-isolated by construction: loader queries filter `TenantRegistryId`, node ids are global
  bigserials, the builder drops edges whose endpoints it did not load, the cache is keyed by tenant and the invoke path
  uses the model's own tenant with no fallback.
- Svirfd remote lookups send only the address (URL path, no body, no other payload field); the key is
  configuration-only, never stored, logged by Jube or returned by the API.

## 2. Files reviewed

Platform fixes: `GraphRecallTaskStarter.cs`, `GraphRecallSnapshotBuilder.cs`, `GraphRecallSnapshot.cs`,
`GraphRecallSnapshotLoader.cs`, `GraphRecallLoaderOptions.cs`, `GraphRecallEvaluator.cs`, `SvirfdHost.cs`. Also read:
`GraphRecallCache/NodeIndex/Plan/Node`, the four `GetGraphRecall*Query` SQL files, `GraphRecallExtensions.cs`,
`SyncEntityAnalysisModelGraphRecallExtensions.cs`, `SvirfdSettings/RemoteCache/RemoteCircuit`, `SvirfdExtensions.cs`,
`SvirfdStartup.cs`, `SvirfdService.cs`, `SvirfdEndpoints.cs`, `EntityAnalysisModelSvirfdEndpoints.cs` (route and
antiforgery list), both model-child validators, both repositories (tenant filters), both DTOs (GraphRecall whole),
`Jube.Svirfd.csproj`, NuGet `Svirfd.xml`, `SvirfdPage.vue`, `SvirfdMap.vue`, `SvirfdFacts.vue`, `format.ts`,
`validate.ts`, both docs pages, the DynamicEnvironment Svirfd rows. `--closure`: DataGrid, JubeChart, useFormat, i18n,
en.json, ProblemAlert are inherited, not reviewed. Svirfd and GraphRecall resx classes only had neutral and fr.

## 3. Findings

| #  | Sev               | Where                                | What                                                                                                                                                                                                                                                                                                                           | Status                                                                                                                                                                                                                                                                        |
|----|-------------------|--------------------------------------|--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| 1  | Medium            | GetGraphRecallEdgesQuery.cs          | Edges were not filtered by registry or definition Active/Deleted, so an edge of a deactivated registry still fed `Edge.*` when no registry guid was set, while `Connects` ignored it.                                                                                                                                          | Fixed, test `EdgesOfADeactivatedRegistryAreNotLoadedAsync`                                                                                                                                                                                                                    |
| 2  | Medium (privacy)  | SvirfdHost.LookupAsync               | Private, loopback and CGNAT addresses were sent to svirfd.com, disclosing internal address plans and wasting rate limit.                                                                                                                                                                                                       | Fixed: `SvirfdAddressScope`, tests                                                                                                                                                                                                                                            |
| 3  | Medium (secrets)  | SvirfdHost ctor                      | `SvirfdBaseUrl` accepted `http://` or user-info URLs, so the API key and addresses could go in clear. Operator setting, not tenant reachable, so not an SSRF by a tenant.                                                                                                                                                      | Fixed: `BaseUrlIsSecure`, client not created, logged. Tests                                                                                                                                                                                                                   |
| 4  | Low               | Jube.Svirfd.csproj                   | `Version="0.0.3"` is a minimum, not a pin.                                                                                                                                                                                                                                                                                     | Fixed: `[0.0.3]`                                                                                                                                                                                                                                                              |
| 5  | Low (testability) | GraphRecallTaskStarter               | Startup rule and retry wait were inline in a loop with no harness.                                                                                                                                                                                                                                                             | Extracted `Succeeded` and `NextWait`, tested                                                                                                                                                                                                                                  |
| 6  | Low (RTL)         | SvirfdFacts.vue                      | Numbers, dates, lists and text values had no direction, so IPs, prefixes and AS names could reorder in ar and ur.                                                                                                                                                                                                              | Fixed: `dir="ltr"` on numbers and dates, `dir="auto"` on text and list items                                                                                                                                                                                                  |
| 7  | Low (i18n)        | SvirfdService.cs, SvirfdEndpoints.cs | Hard-coded English refusal messages.                                                                                                                                                                                                                                                                                           | Fixed (follow-up): new `SvirfdResources` (6 keys, neutral plus pl, ar, ka, id, sv, ur, hi), `SvirfdService.CreateAsync` takes `IStringLocalizerFactory`, endpoints pass it; test `RefusalMessagesComeFromTheLocaliserInTheCallersCultureAsync`; five existing callers updated |
| 8  | Info              | Platform fixes                       | Re-read for correctness: timeout opens a 5 s circuit and caller cancellation still rethrows; `LoadedForStartup` set only on `failures == 0                                                                                                                                                                                     |                                                                                                                                                                                                                                                                               | successes > 0`; node and edge caps independent; second node pass always runs. All correct. One tenant failing while another loads waits the full interval before retrying that tenant (design, acceptable). | None |
| 9  | Info              | GraphRecall validator                | `RequestXPathName` is not checked against the model's fields and `GraphRegistryGuid` not against the tenant registries (Svirfd does check IP type). Harmless: an unknown field yields typed defaults and a foreign guid matches nothing in the tenant's own snapshot. Adding the check would break existing service seed data. | Left, design: the UI constrains it through the Fields lookup                                                                                                                                                                                                                  |
| 10 | Info              | Repositories                         | `                                                                                                                                                                                                                                                                                                                              |                                                                                                                                                                                                                                                                               | !tenantRegistryId.HasValue` is the parameter form (engine tenantless mode); both constructors used by the service resolve a tenant, so the row-level bug in SHARED-TODO does not apply here. | None |
| 11 | Info              | Svirfd, privacy default              | `EnableSvirfd` and remote engine lookup default True, but nothing is sent unless a model has an active Svirfd entry. No per-tenant switch exists.                                                                                                                                                                              | Design, documented                                                                                                                                                                                                                                                            |
| 12 | Info              | Remote cache                         | One cache shared across tenants: the record is public IP data, not tenant data; capacity bounded (100000, quarter eviction).                                                                                                                                                                                                   | None                                                                                                                                                                                                                                                                          |
| 13 | Info              | Status DTO                           | `lastError` shows `ExceptionType: message` from the package to any holder of permission 72; the package could not be proven free of the key.                                                                                                                                                                                   | Docs TODO                                                                                                                                                                                                                                                                     |

Permission 60 traced end to end (follow-up): 60 is `View Model Integrity` (`AddModelIntegrity.cs`,
`EntityAnalysisModelIntegrityService`, `RuleToolsBench.vue`, `integrity/model.ts`) and belongs to model-integrity-sync;
Svirfd and Graph Recall never use it, they only appear as rows in the integrity listing. The slice's real permissions
are 15 (both model children, service lists, agent tools) and 72 (View Svirfd: `SvirfdService.permissions`, `menuTree.ts`
entry, `SvirfdPage.vue` `svirfdPermission` plus `me.svirfd`; the route has no guard of its own, the page refuses with
its own alert). No code mismatch; the mismatch is `permissions: [60, 72]` in `slices.json`, which I may not edit: it
should read `[15, 72]`. New vitest `layout/__tests__/svirfdMenu.spec.ts` pins the entry to 72 alone. Mutations carry
`ValidateAntiforgeryForCookieSessions`. Locale: Graph recall `Name`/`NodeKey`/`RequestXPathName` regex and length limits
covered by PenTest below.

## 4. Fixes made

`GetGraphRecallEdgesQuery.cs` (registry filters), `SvirfdAddressScope.cs` (new), `SvirfdHost.cs`, `SvirfdSettings.cs`,
`Jube.Svirfd.csproj`, `GraphRecallTaskStarter.cs` (two static helpers, no behaviour change), `SvirfdFacts.vue`, 14 resx
files, docs.

## 5. Test and pen-test matrix

| Layer                   | Evidence read                                                                                                                                                                                                                                                                                                                          | Status     |
|-------------------------|----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|------------|
| Service CRUD both areas | `EntityAnalysisModelGraphRecallServiceTests` (14, read `ValidatesTheNameTheFieldAndTheCounterpartAsync`, `ListsTheModelsRequestFieldsWithTheirReportTableFlagAsync`, delete test), `EntityAnalysisModelSvirfdServiceTests` (14, names)                                                                                                 | Covered    |
| PenTest                 | `PenTestEntityAnalysisModelGraphRecall` (matrix, tenant, mass assignment, length and hostile via `ModelAreaPenTestBase`, counterpart cross-tenant), `PenTestEntityAnalysisModelSvirfd`                                                                                                                                                 | Covered    |
| Svirfd lookup service   | `SvirfdServiceTests` (4 with the new localisation test)                                                                                                                                                                                                                                                                                | Covered    |
| Svirfd lookup PenTest   | new `PenTestSvirfdLookup` (searched first: no existing class touched `/api/Svirfd`): 401/403/no-tenant/unknown user matrix on Lookup and Status, tenant sameness, 40 hostile addresses (private, loopback, link-local, IPv6 forms, numeric forms, oversize, unicode, CRLF, SQL, script), no secrets, mass assignment by query and body | New, unrun |
| Engine units            | Evaluator (8), Snapshot (7), Rule (9), Entity (4), Loader (3 DB), Invoke (1)                                                                                                                                                                                                                                                           | Covered    |
| Added unit              | `GraphRecallEvaluatorEdgeCaseTests` (8), `GraphRecallTaskStarterTests` (3 theories), `SvirfdPrivacyTests` (5)                                                                                                                                                                                                                          | New, unrun |
| Added DB                | three in `GraphRecallLoaderTests`                                                                                                                                                                                                                                                                                                      | New, unrun |
| Remote host             | `SvirfdRemoteHostTests` (10, incl. URL path assertion, timeout circuit)                                                                                                                                                                                                                                                                | Covered    |
| Vitest, Playwright      | `svirfd.spec.ts` 10 pass; graphRecall 4 and svirfdPage 12 counted, not run                                                                                                                                                                                                                                                             | Counted    |

To run in the owner pass: `GraphRecallEvaluatorEdgeCaseTests`, `GraphRecallTaskStarterTests`, `SvirfdPrivacyTests`,
`SvirfdRemoteHostTests` (Unit); `SvirfdResilienceTests` (Unit); `SvirfdServiceTests` (Service, own DB);
`PenTestSvirfdLookup` (PenTest); vitest `svirfdMenu.spec.ts` already passes; `GraphRecallLoaderTests` (Service, own DB);
then `PenTestEntityAnalysisModelGraphRecall` and `PenTestEntityAnalysisModelSvirfd`. The Rider build must also compile
`Jube.Svirfd` (new file) and `Jube.Engine`.

## 6. Translations

Created for `EntityAnalysisModelGraphRecallResources`, `EntityAnalysisModelSvirfdResources` and the new
`SvirfdResources` (6 keys): pl, ar, ka, id, sv, ur, hi (21 files, key parity with neutral asserted, placeholders `{0}`,
`{Name}`, `{Member}` preserved). `.fr.resx` untouched. SPA: 135 `nav.svirfd` and `svirfd.*` keys exist in en, pl, ar;
proposals for ka, id, sv, ur, hi in `review/model-network.locale-proposals.json` (placeholder parity asserted).

## 7. Docs written and TODO markers

New `docs/Configuration/Models/GraphRecall/index.md`; `Models/Svirfd/index.md` gained "What leaves the installation";
`Configuration/Svirfd/index.md` updated. `docs/Testing/Engine.md` and `AppAndInfrastructure.md` rows added. TODO
markers: Models/GraphRecall/index.md (screenshot x1, docs x3), Models/Svirfd/index.md (screenshot x1, docs x3),
Configuration/Svirfd/index.md (screenshot x2, docs x1); grep `TODO(` for lines.

## 8. Shared-code TODOs raised

Lines in `review/SHARED-TODO.md`: SvirfdSearch input direction, environment variable doc rows, one fixed BUG. The
service-messages line is now resolved by finding 7.

## 8a. Follow-ups recorded

- **Jube.ResilientHttp adopted (follow-up 2).** Read: `review/model-http-adaptation.md` section 12,
  `ResilientHttpExecutor`, `HttpResilienceOptions`, `HttpFailureClassification`, `HttpResilienceDefaults`,
  `ResilientHttpExecutorTests` (head). `SvirfdHost.LookupAsync` now runs the client call through a
  `ResilientHttpExecutor` (timeout, then breaker; Polly timeout replaces the hand-made linked token source). Decisions
  with evidence:
    - **Own executor per host, not `ResilientHttpExecutor.Shared`.** Breaker state is per key in a static cache; the
      Svirfd unit tests all use `https://svirfd.test`, so the shared instance would leak failures between tests (and
      between hosts). Production has one singleton host, so behaviour is identical there. Key is the base URL authority,
      so the breaker is per service host.
    - **Options.** `SvirfdSettings.ToResilienceOptions()`: shared defaults (ratio 0.5, minimum throughput 5, sampling 30
      s, break 30 s) with the Svirfd lookup timeout (floored at 50 ms, because Polly rejects very small timeouts) and
      `MaxRetries = 0`.
    - **No retry.** Lookups are idempotent reads, but the call happens inside a transaction invocation (a retry
      multiplies a 2 s wait), the open tier allows one lookup a second (429 is classed transient, so a retry would
      hammer the limit) and the cache plus the five second pause already absorb transient errors. The interactive page
      could take a budget: left as an owner decision, noted in the docs.
    - **What stays bespoke, and why.** `SvirfdRemoteCircuit` is kept for pauses the service itself dictates: exact
      `Retry-After`, exponential rate-limit backoff, indefinite off for a refused key, and a short window after a
      timeout or any failure. Polly's breaker cannot open on the first failure (minimum throughput 2 at best) nor take a
      server-supplied duration without changing the shared library, so replacing it would have broken established,
      tested behaviour. The shared breaker adds what was missing: sustained transient failures (network, IO, 5xx) open
      for 30 s per host. A `BrokenCircuitException` mirrors into the Svirfd pause (`CircuitOpenUntil`), a
      `TimeoutRejectedException` opens the five second window as before, `HttpRequestException` still goes through
      `Trip`, caller cancellation still rethrows.
    - **Unchanged rules:** private-address filtering, https base URL rule, local and Directory modes (the pipeline is
      only on the Remote path), only the address in the URL path.
    - **Changed existing tests: none.** Every assertion in `SvirfdRemoteHostTests` (cache and fresh, rate limit,
      Retry-After, key refused, index loading, slow service cut off, timeout opens circuit, user agent, auto fallback,
      non-addresses) still describes behaviour the new code keeps. New `SvirfdResilienceTests` (6) cover options, no
      retry, breaker on repeated network failure, caller cancellation, per-host isolation.
    - Jube.ResilientHttp was not changed; no bug found in it. Not compiled: confirm `catch (BrokenCircuitException ex)`
      with `ex.RetryAfter`, and that `Jube.Svirfd` resolves Polly transitively.
- Per-tenant Svirfd switch: owner design decision (IP addresses are personal data). Not built. Evidence: switches are
  installation-wide (`EnableSvirfd`, `EnableSvirfdRemoteEngineLookup`); a tenant opts in only by configuring an entry.
- The new PenTest assumes the PenTest host's Svirfd starts with defaults (Auto, remote tier); it deliberately looks up
  only non-public or invalid addresses so it never needs the network. If the owner wants a public-address positive case,
  set `EnableSvirfd=False` or stub the handler in the host first.

## 9. Manual test sheet

Permission 15 and 72 denied and granted; create, edit, delete, lock a graph recall entity and a Svirfd entry; each
validator refusal (name, counterpart self, archive plus response exclusive, non-IP field); two tabs concurrency; Svirfd
page: 8.8.8.8, `10.0.0.5` (no remote call), invalid text, state notice; set `SvirfdBaseUrl=http://x` and confirm lookups
are off with a log line; en, pl, ar, ur (IP and prefix stay left to right), narrow width, dark theme, keyboard only; one
hostile string per text field.

## 10. Commit pathspecs

Slice paths plus: `Jube.Svirfd/`, `Jube.Engine/GraphRecall/`, `GraphRecallTaskStarter.cs`,
`Jube.Data/Query/GetGraphRecall*`, `Jube.Tests/Engine/GraphRecall/`, `Jube.Tests/Svirfd/`, 14 resx,
`docs/Configuration/Models/GraphRecall`, `review/model-network*`.

## 11. Not covered

No build or test was run. The `slices.json` permission correction is the owner's. Svirfd package internals (key echo in
errors) not decompiled. Graph snapshot memory not measured. Playwright not run. Slice-adjacent AskJooby and case-journal
Svirfd tools not reviewed. Edge case: a GraphRegistry rename of tenant mid-cycle not analysed.
