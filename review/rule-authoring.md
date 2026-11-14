# Slice review: rule-authoring

Agent-mode review, 2026-11-08. Steps 0, 2, 3, 4, 5. Nothing built, no .NET, DB or Playwright test run. Prettier, eslint,
vue-tsc clean and vitest on ruleBuilder, ruleTools and filterBuilder pass (131 tests).

## 1. Summary and verdict

Verdict: READY AFTER OWNER MANUAL TEST AND THE SERIAL RUN. Backtest isolation questions answered:

- **Tenant**: model, instance, archive query (`GetArchiveBacktestSampleQuery` filters the model's tenant), rule
  environment queries and Stop all take the caller's tenant; `RequireModelAsync` re-checks it. Covered by
  `PenTestEntityAnalysisModelBacktest.AnotherTenantsModelsAndRunsBehaveLikeAbsentOnesAsync`.
- **Allow list**: backtest compiles through `RuleParse.Execute`, the same token allow list (RuleScriptToken table) and
  engine wrap and references as the engine, then runs in `RuleSandbox` (5 s per call, abort on first timeout). Hostile
  text: `PenTest...HostileRuleText_IsNeverPersistedOrExecutedAsync` (22 payloads, submit and explain).
- **Volume and time**: submit capped by BacktestMaxRows, paged (BacktestPageSize, keyset), BacktestMaxRunInterval (1 h),
  per-tenant concurrency 1, stale recovery. The immediate run (agent tool only, not an HTTP route) had a row cap but no
  wall clock: fixed.
- Parity of translators: server `BuilderRuleTranslator` and client `ruleText.ts` agree on the pinned cases; drift found
  and fixed (below).

## 2. Files reviewed

Read: EntityAnalysisModelBacktestService, EntityAnalysisModelBacktestEndpoints, BacktestExecutor, RuleBacktest,
RuleRunner, BacktestTaskStarter, GetArchiveBacktestSampleQuery, GetNextEntityAnalysisModelBacktestInstanceQuery (claim
SQL), BacktestInstanceRepository, RuleParse.Execute, BuilderRuleTranslator, ParserService, RuleVocabularyEndpoints,
ErrorSpan; Vue: BuilderCoder, RuleCoder, RuleTools, BacktestPanel, BuilderStatus, RuleBuilderIsland (head),
customInputs, ruleText, problems, ruleTools/model; main.css fonts. Closure pull-ins (DataGrid, JubeChart, useFormat,
i18n, en.json, ProblemAlert) are inherited from earlier-reviewed slices. Jube.Parser edits in the tree (TokenIndex,
Compile log) are not mine.

## 3. Findings

| #  | Sev          | Where                                                                                                                                                                                        | What                                                                                                                           | Status                                                                                                                                     |
|----|--------------|----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|--------------------------------------------------------------------------------------------------------------------------------|--------------------------------------------------------------------------------------------------------------------------------------------|
| 1  | Med (RTL)    | RuleCoder.vue                                                                                                                                                                                | CodeMirror inherited page direction: in ar/ur the code, gutter and caret ran right-to-left and operators reordered             | Fixed: host `dir="ltr"`, diagnostics and completion sections `unicode-bidi: plaintext`, fixed line height (ur line-height 2 inflated rows) |
| 2  | Med (RTL)    | customInputs.ts                                                                                                                                                                              | numeric, date and field inputs inherited rtl (`-5` shows as `5-`)                                                              | Fixed: `dir="ltr"` (text `auto`)                                                                                                           |
| 3  | Low (RTL)    | BuilderStatus, BacktestExplain, BacktestPanel                                                                                                                                                | English compiler and engine messages inside Arabic sentences                                                                   | Fixed: `dir="auto"` / `<bdi>`                                                                                                              |
| 4  | Med          | EntityAnalysisModelBacktestService                                                                                                                                                           | Validation messages hard-coded English though resource keys RuleTypeInvalid, RangeInvalid existed unused                       | Fixed: localised by code; new keys RuleTextRequired, ArchiveNotFound, LimitTooLarge, ResultUnavailable, OnlineTimeLimit                    |
| 5  | Med          | same, RunRule                                                                                                                                                                                | no wall-clock bound on the immediate run                                                                                       | Fixed: BacktestOnlineMaxSeconds (default 60), returns `OnlineTimeLimit` error                                                              |
| 6  | Low          | ruleTools/model.ts, BacktestInstances, BacktestPanel, RuleDependencies                                                                                                                       | `(line N)`, `(inactive)` and raw status names English                                                                          | Fixed with te() fallback, keys proposed                                                                                                    |
| 7  | Low (parity) | ruleText.ts                                                                                                                                                                                  | client accepted `0x10`, `Infinity`-like and C1 control characters the server refuses; integers beyond 2^53 rounded by parseInt | Fixed: decimal regex, C1 range, BigInt                                                                                                     |
| 8  | Med          | RuleBuilderIsland.vue, labels.ts                                                                                                                                                             | operator, group and argument names came from the server in English                                                             | Fixed (see section 12)                                                                                                                     |
| 9  | Med          | ParserService                                                                                                                                                                                | no rate limit on parse and compile                                                                                             | Fixed (section 12)                                                                                                                         |
| 10 | Med          | EntityAnalysisModelBacktestService.SubmitAsync                                                                                                                                               | no cap on uncompleted backtests                                                                                                | Fixed (section 12)                                                                                                                         |
| 11 | Info         | gen.py in the shared scratchpad was overwritten by another agent and I re-ran theirs once in Jube.Resources; their reprocessing-instance resx may have been rewritten with their own content | tell owner                                                                                                                     |

No code comments added. `ErrorSpan`: client uses Column/EndColumn (original line); Start/Length are translated
coordinates (RuleParseErrorColumnTests, 7 tests, names read).

## 4. Fixes made

Files: RuleCoder.vue, customInputs.ts, ruleText.ts, BuilderStatus.vue, BacktestPanel.vue, BacktestInstances.vue,
BacktestExplain.vue, RuleDependencies.vue, ruleTools/model.ts; EntityAnalysisModelBacktestService.cs;
EntityAnalysisModelBacktestResources.cs/.resx. 21 new resx (pl ar ka id sv ur hi for Backtest, Parser, RuleVocabulary
classes; ar/ur Latin wrapped in U+2066/2069, `.fr.resx` untouched).

## 5. Test and pen-test matrix

| Layer            | Evidence                                                                                                                                          | Status             |
|------------------|---------------------------------------------------------------------------------------------------------------------------------------------------|--------------------|
| Service          | EntityAnalysisModelBacktestServiceTests (16 read by name); ParserServiceTests 19                                                                  | Covered; one added |
| PenTest          | PenTestEntityAnalysisModelBacktest (role matrix GET/POST, cross tenant, malformed, hostile text, mass assignment), PenTestParser                  | Covered            |
| Parser units     | RuleParseSecurityCorpusTests, RuleSandboxTests, BuilderRuleTranslatorTests (browser-equal strings), BuilderFilterTests, RuleParseErrorColumnTests | Covered            |
| Vitest added     | customInputs.spec.ts (6, new), ruleText.spec (+ C1, hex, Infinity, BigInt, cases), model.spec (+2 localised wording)                              | Pass               |
| Playwright added | builderCoder.spec: coder stays ltr in Arabic                                                                                                      | Not run            |
| Added C#         | `TheMessagesOfAnInvalidRequestComeFromTheResourcesInTheCallersLanguageAsync` (en and pl)                                                          | Unrun              |

To run: that Service test, whole EntityAnalysisModelBacktestServiceTests, PenTestEntityAnalysisModelBacktest,
builderCoder e2e.

## 6. Translations

SPA: pl/ar complete for owned keys. 8 new keys proposed; ka, id, sv, ur, hi for all 266 in
`review/rule-authoring.locale-proposals.json` (machine, checked for keys and placeholders). Server: 21 resx. Fonts local
(@fontsource); non-Latin editor fallback is system fonts (shared TODO already).

## 7. Docs

Edited: docs/Navigation/RuleBuilderAndCoder (13 screenshot markers, RTL section, 5 TODO (docs)),
Configuration/Models/Backtest (5 screenshot, limits section, TODOs), CuratedDynamicExpressions and
RuleCompilationAlgorithm (3 TODO (docs) each), Concepts/EnvironmentVariables (one row), docs/Testing/ServiceQueries.md
(one row). Find with `grep -n "TODO(" <page>`.

## 8. Shared-code TODOs

One line (SPA locale keys). The three design lines were fixed and removed.

## 9. Manual test sheet

Coder: type, errors squiggle at the typed span, completion, Beautify, locked builder and Reset, in en and ar/ur (code
ltr, gutter left). Builder: each operator group, number/date inputs in ar. Backtest: permission 61 denied/granted,
submit, Stop, result, Explain, bad range and rule type messages in pl and ar, other-tenant ids 404, huge limit. Narrow
width, dark theme, keyboard only.

## 10. Commit pathspecs

Paths from slices.json plus: Jube.Service/Query/EntityAnalysisModelBacktest, Jube.Resources
Backtest/Parser/RuleVocabulary files, the Backtest service test and ServiceQueries.md hunk, e2e/builderCoder.spec.ts,
the four docs pages and the env var row, review files.

## 11. Not covered

Nothing compiled or run. RuleBuilderIsland body, filterBuilder, CaseFilterBuilder, ExhaustiveFilterBuilder, intellisense
extension not read in depth. Dependency and integrity services read by UI only. Operator catalogue and BuilderProfile
only grepped. Nastaliq/Devanagari rendering untested. Rider lint not run.

## 12. Follow-up: the three former design items (fixed)

1. **Operator labels.** The builder renders server labels, so the fix is SPA keys: `ruleBuilder.operators.<type>`,
   `ruleBuilder.groups.<key>`, `ruleBuilder.arguments.<key>` with `te()` fallback to the server English
   (`Jube.App/Vue/src/ruleBuilder/labels.ts`, used by RuleBuilderIsland and customInputs). The server catalogue is
   reflection-generated from the extension library (`PipelineOperators`), so a per-label resx is not possible without
   enumerating hundreds of generated labels. Translated: all 23 groups, the 20 Basic operators, `has`, 6
   compare-to-field, 17 extension checks, 7 argument names (74 keys, 7 locales, in
   `review/rule-authoring.locale-proposals.json`, en under `_en_new`). Residual, honest: pipeline-generated operators
   and their argument names still show English until the owner runs the app once to dump the catalogue and asks for
   translations. Tests: `labels.spec.ts` (new), `customInputs.spec.ts` (+1).
2. **Parse rate limit.** Search found a Redis-backed HTTP limiter (GlobalHttpRateLimitMiddleware) and a service-level
   `RateLimitedException` -> 429 pattern (synchronisation schedule). Followed the latter: `ParseRateLimiter` (bounded
   in-memory fixed window, 10,000 keys, per user and per tenant), settings `ParseWindowSeconds` 60,
   `ParseMaxPerUserPerWindow` 120, `ParseMaxPerTenantPerWindow` 600 via `DynamicEnvironment` (service gets it as an
   optional trailing parameter), new `Jube.Service/Exceptions/Query/Parser/RateLimitedException.cs`, resource
   `ParseRateLimited` in 8 resx, endpoint 429, and BuilderCoder retries after 10 s on 429. Counted per process; a
   multi-node deployment multiplies the limit by the node count (stated in the env var docs). Tests:
   `ParseRateLimiterTests` (3, Unit), `ParserServiceTests` (+3).
3. **Backtest queue cap.** `BacktestMaxPendingPerModel` 5 and `BacktestMaxPendingPerTenant` 20 (Pending, Running,
   Cancelling), checked before compile in `SubmitAsync` via
   `EntityAnalysisModelBacktestInstanceRepository.CountUncompletedAsync`; refusal is `Valid=false` with error codes
   `PendingLimitModel` and `PendingLimitTenant`, localised in 8 resx. Tests: `EntityAnalysisModelBacktestServiceTests`
   (+2). The service reads settings as environment variables (existing `Setting` pattern), defaults also added to the
   DynamicEnvironment dictionary together with BacktestOnlineMaxSeconds (one surgical edit; another agent is also
   editing that file).

Tests to run: ParserServiceTests, ParseRateLimiterTests, EntityAnalysisModelBacktestServiceTests (the tenant cap of 20
could be hit if another test class leaves Pending rows in the seed tenant; check), PenTestParser,
PenTestEntityAnalysisModelBacktest. Vitest (134) passes. Helper scripts only wrote into `.../scratchpad/rule-authoring/`
(en-labels.json, labels-<locale>.json, addkeys.py) and edited `Jube.Resources/ParserResources*.resx`,
`EntityAnalysisModelBacktestResources*.resx`.
