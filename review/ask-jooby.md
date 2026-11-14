# Review: ask-jooby (Ask Jooby chat, tools and memory)

## Summary and verdict

Second pass completed in risk order (script execution, outbound HTTP, write confirmation, rate limits, provider keys,
System memory, RTL and accessibility). Still not exhaustive: see Not covered.

First pass: partially reviewed, fixes made. The session was interrupted once and the budget was spent on the
highest-risk surface (conversation access, stream endpoint, chat rendering, memory, tool gating). Not a full pass: see
Not covered. The headline finding is a real authorisation bug: every conversation in a tenant was visible, readable,
writable and deletable by every other user of that tenant. Fixed. Verdict: acceptable to proceed once the dotnet tests
listed below are run.

## Files reviewed (read in full unless noted)

Vue: src/askjooby/{api,controller,render,stream,format,welcome,widgetState,types,deepChatSurface,deepChatTheme}.ts,
src/components/askjooby/{JoobyChat,JoobyWidget,JoobyLauncher,ConversationList}.vue, useJooby.ts,
src/pages/AskJoobyPage.vue, src/download/file.ts. Not read: offlineWelcome.ts (text only), toolIcons.ts (literal-entry
invariant untouched), e2e/askjooby.spec.ts.
Back end read: Jube.App/Endpoints/AskJoobyConversationEndpoints.cs, AskJoobyStreamEndpoints.cs,
AskJoobyConversationServiceFactory.cs, Jube.Service AskJoobyConversationService (send, list, get, delete, factory
sections), AskJoobyFileService (upload, list, download, delete), AskJoobyMemoryClient, AskJoobySensitiveDataGuard,
AskJoobySqlQueryClient, PageAgentToolBinder, Jube.AskJooby AskJoobyInjectionGuard, ToolCallAndResultGuardChatClient,
AgentGuardToolProvenance, AgentFactory, AgentDefinitions.IsPermitted, MemoryTool, SearchAskJoobyMemoryQuery,
AskJoobyConversationRepository, AskJoobyFileRepository, AskJoobyMemoryRepository, deep-chat 2.5.1 bundle (markdown
config).
Inherited, not re-reviewed: shell-auth (AppLayout lazy boundary, joobyContext store), model-advanced-analytics
(AskJoobyAnalytics*), model-http-adaptation.

## Findings

| #  | Severity | Where                                                                                     | What                                                                                                                                                                                                                                                                                                                                                                                     | Status                                                                                     |
|----|----------|-------------------------------------------------------------------------------------------|------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|--------------------------------------------------------------------------------------------|
| 1  | High     | AskJoobyConversationRepository, AskJoobyFileRepository, services                          | Conversations and files were scoped by tenant only. Any user holding AskJooby access could list, open (GetMessages), send into, rate, delete, upload to and download from other users' conversations; a lower-privilege user could read a higher-privilege user's tool results. Transcript and Audit services already refused other users, so the rest of the surface contradicted them. | Fixed (owner-restricted repositories)                                                      |
| 2  | Medium   | AttachToMessageAsync                                                                      | Attached any tenant file id to the caller's message, re-parenting another conversation's file.                                                                                                                                                                                                                                                                                           | Fixed (conversation constraint, owner restriction)                                         |
| 3  | Medium   | AskJoobyStreamEndpoints.cs WriteStreamAsync                                               | Raw `e.Message` of any exception (provider endpoint, SQL, paths) was sent to the browser in the SSE error event.                                                                                                                                                                                                                                                                         | Fixed (localised fixed message, detail stays in log)                                       |
| 4  | Medium   | AskJoobyStreamEndpoints.cs                                                                | No size limit on message text or page context (token-cost and memory abuse); null AttachmentFileIds would throw outside the try.                                                                                                                                                                                                                                                         | Fixed (20,000 / 8,000 character caps returning 400, null guards)                           |
| 5  | Medium   | Jube.App/Vue controller.ts                                                                    | Markdown images in model output would auto-load remote URLs (prompt injection exfiltration channel). Mitigated by CSP img-src ('self' data: and tile hosts) but that header is DB-editable.                                                                                                                                                                                              | Fixed (images replaced by text, defence in depth)                                          |
| 6  | Low      | MemoryTool.Remember                                                                       | No content length bound (tool path); System scope is tenant-wide and writable by the agent.                                                                                                                                                                                                                                                                                              | Length cap added; System scope left (design)                                               |
| 7  | Low      | controller.ts, ConversationList.vue                                                       | Hard-coded English user strings and English relative time.                                                                                                                                                                                                                                                                                                                               | Controller strings and relative time fixed; format/welcome/offline text left (SHARED-TODO) |
| 8  | Info     | deep-chat                                                                                 | Remarkable default: html false, link validation blocks javascript:, no CDN loads unless configured. Verified in the bundle. No stored or reflected XSS path found; activity html built only through escapeHtml; svg attachments are data URI images.                                                                                                                                     | No change                                                                                  |
| 9  | Info     | tool gating                                                                               | Tools offered only if IsPermitted (any-of, landlord flag fail-closed); service-bound tools re-check permissions and the privilege-escalation guard inside the service, so tools cannot bypass it. SpawnAgent types are filtered by Available(). Dynamic SQL is rewritten to an allow-list per caller.                                                                                    | No change                                                                                  |
| 10 | Info     | no null-tenant pattern or plain SelectToken found in owned repositories and tools (grep). | None                                                                                                                                                                                                                                                                                                                                                                                     |

## Design questions (owner)

- Write tools run after an automatic preservation snapshot with no separate human confirmation; AskJoobyReadOnly removes
  them (default off). Design, not a bug: intended model is snapshot-and-revert. Prompt injection from case notes could
  request a write; the input and tool-result guards are pattern and classifier based, not a guarantee.
- System-scope memory is shared tenant-wide and an agent can write it. A poisoned Fact would reach every user (Recall
  results are screened by the result guard). Consider requiring a permission for System scope.
- Conversations are now private to their creator with no administrator read path.

## Fixes made

- Jube.Data/Repository/AskJoobyConversationRepository.cs and AskJoobyFileRepository.cs: optional `restrictToOwner`
  (case-insensitive CreatedUser match); AttachToMessageAsync takes an optional conversation id (placed after the token
  so existing calls compile).
- Jube.Service AskJoobyConversationService and AskJoobyFileService construct both repositories restricted. Transcript
  and audit services unchanged (their own owner check returns 403).
- Jube.App/Endpoints/AskJoobyStreamEndpoints.cs: error message, size caps, null guard.
- Jube.Resources/AskJoobyConversationResources: new key StreamFailed.
- Jube.AskJooby/Tools/Memory/MemoryTool.cs: content cap 2000.
- Vue: render.ts neutraliseMarkdownImages, controller.ts uses it and a `t` translator with English fallbacks,
  useJooby.ts passes t, format.ts relativeTime locale, ConversationList.vue.

## Test and pen-test matrix

Read: AskJoobyRepositoryTests, MemoryToolTests; existing coverage cited by name list in the audit
(AgentFactoryPermissionTests, AgentRouterPermissionTests, AskJoobyInjectionGuardTests, AskJoobyMemoryTests,
AskJoobySqlQueryClientTests, PermissionParityTests, ChatTranscript and Audit service tests with
GenerateAsyncIsForbiddenForADifferentUserInTheSameTenantAsync).
Added (unrun): AskJoobyRepositoryTests: OwnerRestrictedRepositoryNeverListsLoadsOrDeletesAnotherUsersConversationAsync,
UnrestrictedRepositoryStillSeesEveryConversationInTheTenantAsync,
OwnerRestrictedFileRepositoryNeverExposesOrChangesAnotherUsersFileAsync,
AttachToMessageAsyncIgnoresAFileFromADifferentConversationWhenOneIsGivenAsync. MemoryToolTests:
RememberRejectsContentOverTheLengthCapAsync, RememberRejectsBlankContentAsync.
Vitest added and passing (140 tests in src/askjooby and src/components): render.spec (neutraliseMarkdownImages x3),
controller.spec (never shows markdown image; translator with fallback), format.spec (locale relative time). eslint,
prettier, vue-tsc clean.
Gap: no HTTP pen test of cross-user conversation access or of the 400 size cap; none exists for the Ask Jooby endpoints
beyond the 3 PenTest files in the audit.
dotnet to run (serial pass): AskJoobyRepositoryTests, MemoryToolTests, AskJoobyChatTranscriptServiceTests,
AskJoobyChatAuditServiceTests, AskJoobyConversationStartTests, AskJoobyConversationValidationBypassTests,
AskJoobyConversationHistoryQueryTests, AskJoobyFileExportClientTests, any class using a second user against the same
conversation (watch for tests that relied on the old tenant-wide visibility), PenTest Ask Jooby classes.

## Translations

Created seven resx (pl, ar, ka, id, sv, ur, hi) for AskJoobyConversationResources and AskJoobyMemoryResources (14 files;
placeholder parity and XML validity checked by script; generator in scratch). SPA:
review/ask-jooby.locale-proposals.json (ka, id, sv, ur, hi full askJooby section; pl and ar the 11 new keys). Existing
en/pl/ar parity was clean per the audit. Not done: font coverage for ka/hi/ur in deep-chat, RTL bidi isolation of code
and ids in chat bubbles, remaining English in format.ts/welcome.ts/offlineWelcome.ts. Agent prompts and tool
descriptions stay English deliberately (model-facing).

## Docs written and TODO markers

docs/AskJooby/index.md: new sections "Who can see a conversation" and "What the chat window renders", with 3 screenshot
TODOs and 5 docs TODOs (end of file). docs/Testing/AskJooby.md updated for the six new .NET tests. The other pages in
docs/AskJooby were not re-read: TODO (docs) owed for staleness.

## Shared-code TODOs raised

Three lines appended to review/SHARED-TODO.md (remaining English, locale JSON, slices.json).

## Manual test sheet

Two users in one tenant: user B must not see, open, delete or send into user A's conversation, nor download A's files
(deep link by id gives 204/403). Reply containing a markdown image shows "[image removed]". Provoke a provider failure:
only the generic message appears. Send 25,000 characters: 400. Widget: open with keyboard, Escape closes and focus
returns to launcher; screen reader announces send button. Switch to pl and ar: conversation list times localised, errors
localised, chat input right-to-left. Hostile text in a case note asking to remember a System fact.

## Second pass (coordinator order of risk)

### 1. RunScript (R sandbox)

Read: RunScriptTool, AskJoobyScriptExecutionClient, ScriptExecutionGateway, ScriptExecutionService (permission, size,
timeout, pointer resolution), RServerClient, Jube.R README, R/sandbox.R, test-forbidden-corpus.R, docker-compose.yml.
Permission `SandboxedScriptExecutionId` is checked in the service; script max 200,000 characters, timeout 1 to 300 s
plus guard, at most 10 dataframes and 50 variables, dataframes resolved from the caller's own pointer cache (tenant and
conversation bound), R server container read-only, caps dropped, 256 pids, 2 GB, 2 CPUs. Findings:

- BUG fixed: the sandbox forbid-list was a blacklist with gaps. `readLines("http://...")`, `read.csv(url)`, `file()`,
  `scan`, `readRDS`, `list.files`, `Sys.glob`, `file.copy`, `serverSocket`, `nsl`, `parallel::mclapply` and namespace
  indirection (`getFromNamespace("system","base")`, `getAnywhere`, `asNamespace`, `loadNamespace`) were all permitted,
  giving file reads, network exfiltration and a route round the `system` block. About 45 symbols added to
  `FORBIDDEN_SYMBOLS` in Jube.R/R/sandbox.R and a seed for each in `test-forbidden-corpus.R` (every wrapping shape).
  `load`, `dir` and `save` deliberately left allowed (common column and variable names; a bare symbol is rejected).
  Residual: still a blacklist; R tests not run (no Rscript here).
- BUG fixed: `jube-r` shared the default compose network with postgres, resp and the app, so any script that did reach a
  socket could reach them, and Jube.R is unauthenticated by design. Both compose files now put it on an `internal: true`
  network `rsandbox` shared only with the app.
- Design: the tool description asks the model to confirm with the caller before running; it is now not a server rule.
  RunScript is not a database write so the new approval gate does not cover it. Not changed.

### 2. Outbound HTTP (web search, OpenCorporates, other)

Read: TavilyWebSearchClient, OpenCorporatesClient (send and error paths), WebSearchTool, OpenCorporatesTool (guard call
sites), Startup registration. No host is chosen by a user or the agent (operator-set endpoint), so the case-feature SSRF
machinery (GuardedHttpEndpoint, OutboundHostPolicy) is not applicable; section 12 of model-http-adaptation.md and
case-workflow-config.md read for the pattern only. Fixed:

- Both clients used `new HttpClient()` (redirects followed, 100 s, 2 GB buffer). New
  `Jube.Service/AskJoobyOutbound/AskJoobyOutboundHttpClient.cs`: no redirects, no cookies, 10 s connect, 60 s total, 8
  MB cap; used from Startup for both.
- WebSearchTool: snippet 4,000, extracted page 12,000, answer 4,000 characters; the provider's synthesised answer and
  the result title are now screened for injection (previously only content and raw content). Existing: query screened
  for secrets, results screened and withheld, result text framed as untrusted. New test
  OversizedSnippetsPagesAndAnswersAreTruncatedBeforeTheyReachTheModelAsync.
- Not adopted: Jube.ResilientHttp (no breaker on these two clients); noted, small. OpenCorporates puts its api_token in
  the query string; error messages use the query text, not the URL, so it is not echoed.
- Data out: queries can contain tenant data the model chooses to search (names). Only secrets are blocked, PII is not.
  No per-tenant switch exists (`EnableWebSearch`, `EnableOpenCorporates` are deployment-wide). Design line for the
  owner.

### 3. Write confirmation (bug-class fix)

Framework check: Microsoft.Agents.AI 1.20 has an approval-request mechanism but this application rebuilds history per
request and streams its own item types, so using it would need a resumable run; not adopted. Implemented the smallest
server-enforced version:

- `ToolApprovalLedger` (Jube.AskJooby/Security): bounded in-memory ledger of pending, approved and rejected requests
  keyed by conversation, user (case-insensitive), tenant, tool and a hash of the canonical arguments; 30 minute life,
  500 entries, 20 pending per conversation.
- `ApprovalGatedAiFunction` wraps every tool with `RequiresSnapshot` (Write and Delete service operations) in
  `AgentFactory`, outside the restore-point wrapper: first call returns a "not done, user approval needed" result and
  registers a pending approval; an approved identical call runs once (after the snapshot); a declined call is refused.
- The service yields an `Approval` stream item (new `ApprovalId` DTO key 17) for each pending approval at the end of the
  turn; `POST /api/AskJoobyConversation/{id}/Approvals/{approvalId}` (new request DTO) records the decision;
  `AskJoobyConversationService.DecideApprovalAsync` checks the caller owns the conversation and the ledger entry matches
  user and tenant. It carries no `ServiceOperation` attribute, deliberately, so the agent can never be offered it. Only
  an authenticated owner call can approve; the model cannot.
- UI: approval card in JoobyChat.vue (tool name and arguments left-to-right, Approve and Decline); the controller posts
  the decision and then sends a localised follow-up message so the agent repeats the call.
- Switch: `AskJoobyRequireWriteApproval` (default True). `AskJoobyReadOnly` still removes write tools.
- Background callers (case narrative) run read-only, so they are unaffected.
- Limit: ledger is per process; multi-node needs the decision on the same node or the agent asks again (docs TODO).
- Tests: ToolApprovalTests (10, Unit), controller.spec (+2), e2e askjooby.spec (+1, unrun).

### 4. Rate limits and cost

Found: no limiter on the chat endpoints (GlobalHttpRateLimitMiddleware is IP level and was not read in depth); per-turn
caps exist (AskJoobyMaxSearchesPerTurn, AskJoobyMaxConcurrentToolCalls, token budget, fan-out). Added the house pattern
(service `RateLimitedException` then 429, bounded in-memory fixed window, DynamicEnvironment settings, localised
refusal): `AskJoobyRateLimiter` and `AskJoobyRateLimits`; SendMessage and the model-backed Start are wrapped (limit then
core method); refusal is 429 with `Retry-After` and a localised `errors[0].errorMessage` the SPA already reads. Defaults
60 s window, 20 per user, 120 per tenant, 3 concurrent turns per user, 20 per tenant. Tests: AskJoobyRateLimiterTests
(6, Unit). Per-conversation tool-call caps and an overall per-tenant token ceiling were not added (existing per-turn
budget stands).

### 5. Provider keys and data to the provider

Read Startup.AddSingletonForAskJooby and the key grep. Keys (`AskJoobyApiKey`, validation, web search, OpenCorporates)
are process settings from environment or configuration, never stored in the database, never in an API DTO, not in any
log line (only "is not set" warnings), and the stream error no longer echoes exception text. Whether the environment
values are encrypted at rest is a deployment matter (the compose file passes them as plain environment). Data sent to
the inference provider: user text, page context, attachments, tool results and tool-result summaries, recorded in
AskJoobyModelCall. There is no per-tenant switch; `EnableAskJooby` is deployment-wide. Design line: a per-tenant opt-out
would need a tenant setting and a check in the service factory. Docs TODO written.

### 6. System-scope memory

Memory design files re-read: System scope is deliberately tenant-wide ("something genuinely useful to any user"). Any
user could be steered (by injected case text) into an agent writing a tenant-wide memory that is recalled for every
colleague. Fixed: a System-scope `Remember` now goes through the same approval gate (MemoryTool takes the gate from
RunContext); User and Conversation scope are unchanged. Tests: ASystemScopeMemoryIsHeldBackUntilTheUserApprovesItAsync,
UserAndConversationScopeMemoriesNeedNoApprovalAsync. Recall results are screened by the tool-result guard. The admin
memory service (AskJoobyMemoryService) is a human path and is not in the tool catalogue.

### 7. RTL, accessibility, e2e

Read JoobyWidget, JoobyLauncher, JoobyChat, ConversationList, AskJoobyPage, e2e/askjooby.spec.ts (stubs and first
tests). Fixed: bubbles use `unicode-bidi: plaintext`; code, pre, pills, timestamps and activity detail are isolated
left-to-right; approval card tool and arguments are `dir="ltr"`; message list gets `role="log"`, polite live region and
a label; opening the widget moves focus to its heading (Escape already closed it and returned focus to the launcher);
launcher already has aria-expanded and aria-controls and the panel is preloaded when the shell is idle, so lazy loading
does not break either. Logical CSS (`ms-auto`, `border-s`, `text-start`, `end-0`) was already used in the SFCs. Not
verified in a browser: deep-chat's own internal markup (class names `message-bubble`, ids `messages`) was taken from the
bundle; Urdu Nastaliq, Georgian and Devanagari font coverage; RTL screenshot. E2E: two tests added (approval flow,
remote image never requested); none run (owner runs Playwright).

### Ownership fix: tests that could break

Searched Jube.Tests for service and endpoint use of AskJoobyConversation, files and repositories. Every conversation
service test seeds the conversation with the same `userName` it constructs the service with
(AskJoobyConversationCompactionTests, ContentSafetyTests, StartTests, ReviseStreamingTests, NudgeTests,
ValidationBypassTests, FeedbackTests, SpawnAgentsConcurrencyDbTests, ChatTranscriptServiceTests), the export client
tests use the repository unrestricted, so none should break. Not confirmed by running; watch these when run.
PenTestUploadAntiforgery only checks the Files route is protected. No test relied on cross-user visibility.

### Lint

Rider `min_severity=error`, one file at a time, no errors: AskJoobyFileRepository, AskJoobyConversationService (large
file, analysis may be off), AgentFactory, ToolApprovalLedger, ApprovalGatedAiFunction, AskJoobyRateLimiter, MemoryTool,
WebSearchTool, AskJoobyStreamEndpoints, AskJoobyConversationRepository (weak warnings only, existing style). Timed out
and so unlinted: AskJoobyFileService, ToolApprovalTests, and the other new test files, ToolApprovalGate,
AskJoobyOutboundHttpClient, resx.

### dotnet tests to run (serial pass)

New: ToolApprovalTests, AskJoobyRateLimiterTests, MemoryToolTests, WebSearchToolTests, AskJoobyRepositoryTests.
Regression: AgentFactoryPermissionTests, BoundToolSurfaceDecorationTests, SnapshotOnWriteAIFunctionTests,
PreservationSnapshotCoverageTests, ServiceToolCatalogueTests, PermissionParityTests, every AskJoobyConversation* service
test listed above, AskJoobyChatTranscriptServiceTests, AskJoobyChatAuditServiceTests, TavilyWebSearchClientTests,
OpenCorporatesClientTests, a PenTest pass over the Ask Jooby endpoints (none exist for conversation access, approvals or
the 429: gap). R: `Rscript -e 'testthat::test_dir("Jube.R/tests/testthat")'` for the forbidden corpus. Playwright:
e2e/askjooby.spec.ts.

### Still open

- Design: per-tenant opt-out of provider data sharing and web search; ResilientHttp breaker for web search; HTTP-level
  pen tests for the new endpoints; per-conversation tool-call caps and a tenant token ceiling; Start endpoint size cap;
  multi-node approval ledger.
- Not read: RunSqlQueryTool internals, SpawnAgent budgets, data pointer cache repository bodies, memory admin service,
  audit zip contents, GlobalHttpRateLimitMiddleware, Playwright run.

## Third pass (coordinator follow-ups)

### A. jube-r network topology (proof)

Who calls whom, from code: the only callers of the R server are `RServerClient.FromSettings` in `Jube.Service`
(`ServiceAgentServiceProvider` line 73, `AskJoobyConversationService` line 3301, `ScriptExecutionService`), reading
`RServerUrl` (`http://jube-r:8080`). Calls are plain HTTP GET (`/health`, `/packages`) and POST (`/validate`,
`/execute`) made by the app, with the result in the same response; the R-job queue and poller from the earlier design
were removed (memory `project-r-script-sync-cross-cut-refactor`, and no `AdvancedAnalyticsRScriptRun*` caller remains).
Jube.R itself: no DBI, RPostgres, redis or httr use in `plumber.R` or `R/` (grep), never initiates a connection, README
states it "never reaches a database itself" and data arrives pre-resolved in the request body. `RServerUrl` is set on
exactly one service per compose file. So the required path is app to jube-r only, and the topology keeps it: the app is
on both its original network and `rsandbox`, jube-r only on `rsandbox`, Docker/Swarm DNS resolves `jube-r` on the shared
network, and jube-r publishes no ports. Compose files that define jube-r: `docker-compose.yml`,
`docker-compose.redis.yml` (bridge, `internal: true`), `Jube.Cluster/docker-compose.yml`,
`Jube.Cluster/docker-compose.redis.yml` (overlay, `internal: true`, encrypted; these had jube-r on the shared
`jube-cluster` network with Patroni, so the same exposure existed and is now fixed; the service is `jube-api`).
`.github/zap/docker-compose.zap.yml` defines no jube-r (it overrides env on the base stack, so inherits the fix); the CI
workflows only build the image and run `docker compose up` on the root file. All four files parse as YAML and each shows
the jube-r service on `rsandbox` alone and the RServerUrl service on both networks (scripted check). Residual: not run
against Docker or Swarm here; `internal: true` also removes jube-r's outbound internet (intended: the image has all
packages baked in, `install.packages` is forbidden).

### B. PenTests for approvals and the write gate

New `Jube.Tests/Security/Surface/PenTestAskJoobyApprovals.cs` (Category PenTest, PenTestCollection, derived from
PenTestBase like `PenTestUploadAntiforgery`; searched first, no existing test touches the Ask Jooby conversation routes
except the upload antiforgery target list): unauthenticated 401; cookie session without antiforgery 400; another
same-tenant user, another-tenant user and the landlord cannot approve or decline (refusal 401/403/404 and the ledger
entry stays Pending); owner approves once (204) and replay and change of mind are 404 and the state stays Approved; id
from another conversation and unknown id 404; extra body fields (state, userName, tenantRegistryId, id, conversationKey)
from a forging user refused and ignored for the owner; malformed bodies and hostile ids never 5xx; oversized id 400;
decision on a deleted conversation 404; the gate never unlocks for another user, tenant or tool. The host is in-process,
so the test seeds a pending request through the real `ToolApprovalLedger.Shared` and reads its state back. Added
`ToolApprovalLedger.StateOf` for that. Extra unit test `TheApprovalDecisionIsNeverOfferedToAnAgentAsATool` (no
ServiceOperation attribute, absent from `ServiceToolCatalogue.All`), which is the "agent identity cannot approve" proof;
at HTTP level the agent has no credential of its own, it runs inside the user's turn and can only call catalogued tools.
The wrong-permission 403 variant does not exist: the endpoint, like the rest of the conversation surface, needs an
active user, not a permission id (EnsurePermitted checks ActiveUser). An expired approval cannot be produced over HTTP
in a test without waiting 30 minutes: covered by `AnExpiredApprovalIsForgotten` (unit, injected lifetime). Not run.

### C. RunSqlQueryTool internals

Path read end to end: `RunSqlQueryTool` calls `ISqlQueryClient.ExecuteAsync` (AskJoobySqlQueryClient), which builds the
allow-list from `DynamicSqlTableAccess.BuildAllowedTables(userName, hasPermission, conversationId)` where
`hasPermission` is `permissionValidation.Validate` of the signed-in caller and `userName` is the caller (service line
3309; the agent has no identity of its own), then `PostgresSqlValidator.RewriteTableReferences` (the shared hardened
validator from review/visualisation.md section 12), then `Postgres.ExecuteAlreadyTokenSwitchedAsync`, which re-runs
`AssertSelectOnly(blockProtectedRelations)` and executes inside `BeginGuardAsync` (BEGIN READ ONLY, statement and lock
timeouts). So it is the same validator, read-only transaction and timeouts. Two gaps found and fixed:

- BUG: no byte caps on this path (visualisation's `ReportLimits` applied only to
  `ExecuteLimitedByNamedParametersAsync`). A user with Archive access could make the agent pull up to 100,000 rows of
  large JSON into process memory. Fixed in `Postgres.ExecuteAlreadyTokenSwitchedAsync` (only Ask Jooby calls it): 1 MB
  per cell and 32 MB per result from `ReportLimits`, refused as `InvalidOperationException` with a model-facing message
  the tool already relays. Test `AValueOverTheCellByteLimitIsRefusedBeforeItIsHeldInMemoryAsync` (DB, unrun; its first
  half also proves `lpad` is not denied).
- BUG: the isolated scope ran user SQL on the application login (write grants) although every other SQL consumer prefers
  `ReportConnectionString`. `AskJoobySqlQueryClient` now takes an optional report connection string for the two methods
  that run user SQL (`ExecuteAsync`, EXPLAIN) and falls back to the application string; table comments and statistics
  keep the application connection (catalogue reads). The service passes `ReportConnectionString`. With no report string
  configured behaviour is unchanged.
  Not changed: parameters are not supported by this tool (SQL text only), results are screened row by row for injection
  and secrets (existing), logging records the rewritten SQL.

### D. Lint (Rider, error severity, one file at a time)

Clean: ToolApprovalTests, AskJoobyFileService, AskJoobySqlQueryClient, Postgres.cs (large file, analysis may be off).
Timed out, unlinted: PenTestAskJoobyApprovals.cs, AskJoobyRateLimiterTests.cs, ToolApprovalGate.cs,
AskJoobyOutboundHttpClient.cs and the resx.

### Tests to run now (additions to the earlier list)

`PenTestAskJoobyApprovals`, `ToolApprovalTests`, `AskJoobySqlQueryClientTests`, the visualisation validator classes
listed in review/visualisation.md section 12.6 (Postgres.cs edited), `Reporting/ReportLimitsTests`, and a compose smoke:
`docker compose up` on the root file then `POST /execute` through the app (RunScript) to see the R path work on
`rsandbox`.

## Commit pathspecs

Jube.App/Vue/src/askjooby, src/components/askjooby, e2e/askjooby.spec.ts, Jube.App/Vue/src/stores/joobyContext.ts,
src/pages/AskJoobyPage.vue, src/download, src/stores/joobyContext.ts; Jube.App/Endpoints/AskJoobyStreamEndpoints.cs;
Jube.Data/Repository/AskJoobyConversationRepository.cs, AskJoobyFileRepository.cs;
Jube.Service/AskJoobyConversation/AskJoobyConversationService.cs, AskJoobyFileService.cs;
Jube.AskJooby/Tools/Memory/MemoryTool.cs, Tools/WebSearch/WebSearchTool.cs, Security/ToolApproval*,
Security/ApprovalGatedAiFunction.cs, Security/Models/ToolApproval*, Security/Interfaces/IToolApprovalGate.cs,
Agents/AgentFactory.cs, Agents/Models/RunContext.cs, Agents/Basic/AgentDefinitions.cs;
Jube.Service/AskJoobyConversation/AskJoobyRateLimit*.cs, Jube.Service/AskJoobyOutbound,
Jube.Service/Exceptions/AskJoobyConversation/RateLimitedException.cs;
Jube.Dto/AskJoobyConversation/AskJoobyApprovalDecisionRequestDto.cs and AskJoobyStreamItemDto.cs; Jube.R/R/sandbox.R and
its corpus test; docker-compose.yml and docker-compose.redis.yml (hunks); Jube.DynamicEnvironment and
Jube.App/Startup.cs (hunks); Jube.Resources/AskJoobyConversationResources.* and AskJoobyMemoryResources.*; the two test
files; docs/AskJooby/index.md, docs/Testing/AskJooby.md; review files.

## Not covered

Superseded by "Second pass" and "Still open" above. Remaining honest gaps: RunSqlQueryTool internals, SpawnAgent
budgets, data pointer cache repository bodies, memory admin service, audit zip contents, GlobalHttpRateLimitMiddleware,
SelectToken sites beyond the grep, inference provider HttpClient options, RTL and font checks in a browser, any dotnet,
R or Playwright run, and Rider lint on the files listed as unlinted.
