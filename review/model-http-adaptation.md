# Slice review: model-http-adaptation

Agent-mode review, 2026-11-08. Steps 0, 2, 3, 4, 5. Nothing was built and no .NET or DB-backed test was run. The slice
has no Vue files, so npm format, eslint, type-check and vitest have nothing to check. Rider lint was not run (no build
allowed): lint owed on the changed C# files.

## 1. Summary and verdict

Verdict: READY AFTER OWNER DECISIONS and one serial test pass. The CRUD surface (service tests 47, PenTest
`PenTestEntityAnalysisModelHttpAdaptation` over `ModelAreaPenTestBase`) is well covered, and the save-time URL
validation is sound: the tenant supplies only a path that is appended to the operator-set `HttpAdaptationUrl`, never a
host. The engine side, however, had real defects: redirects were followed, TLS validation was hard-coded off and the
documented `HttpAdaptationTimeout` and `HttpAdaptationValidateSsl` keys were dead, response size was unbounded, a
non-2xx status was fed to the parser, and a timeout (TaskCanceledException) escaped the per-adaptation catch and could
abort the whole invoke. All fixed.

SSRF answers: scheme, host, port, userinfo, link-local and metadata, loopback, private ranges, IPv6 forms and DNS
rebinding are not reachable by a tenant because no host is tenant-controlled. `OutboundHostPolicy` and
`GuardedHttpEndpoint` (Jube.Case) are used by the case and webhook features, not by HTTP adaptation. Remaining reach:
every tenant can POST its own payload to any path on the shared `HttpAdaptationUrl` host (design, section 3 finding 8).
Secrets: the adaptation stores no credential, token or header, so nothing to encrypt, return or log. Header and CRLF
injection: no header is built from tenant input; CR/LF, whitespace and control characters are refused in the path
(validator and PenTest). Permission verbs: 15 for list, read and write alike, tenant from `UserInTenant`.

## 2. Files reviewed (first-touch pull-ins)

| File                                                                                                   | Read                                                                            | Notes                                                                                                 |
|--------------------------------------------------------------------------------------------------------|---------------------------------------------------------------------------------|-------------------------------------------------------------------------------------------------------|
| Jube.Case/Outbound/*.cs (OutboundHostPolicy, GuardedHttpEndpoint, IpClassifier, IpRange, HostSpelling) | whole                                                                           | not used by adaptation; confirmed by grep (users: webhooks, case hold, release hook, elevation range) |
| Jube.Validations/EntityAnalysisModelHttpAdaptation/...DtoValidator.cs                                  | whole                                                                           | path-only validation                                                                                  |
| Jube.Dto/EntityAnalysisModelHttpAdaptation/...Dto.cs                                                   | whole                                                                           | no secret fields                                                                                      |
| Jube.Data/Poco/EntityAnalysisModelHttpAdaptation.cs                                                    | whole                                                                           |                                                                                                       |
| Jube.Service/EntityAnalysisModelHttpAdaptation/*Mapper.cs                                              | whole                                                                           | copies only client fields                                                                             |
| Jube.Service/EntityAnalysisModelHttpAdaptation/*Service.cs                                             | permission arrays, EnsurePermitted call sites (1090 lines, not read end to end) |                                                                                                       |
| Jube.App/Endpoints/EntityAnalysisModelHttpAdaptationEndpoints.cs                                       | group and verb lines only                                                       | `RequireAuthorization` on the group; 651 lines not read in full                                       |
| Jube.Engine .../Models/Models/EntityAnalysisModelHttpAdaptation.cs                                     | whole                                                                           | edited                                                                                                |
| Jube.Engine .../Context/Extensions/SyncEntityAnalysisModelHttpAdaptationExtensions.cs                  | whole                                                                           | edited                                                                                                |
| Jube.Engine .../HttpAdaptationsExtensions.cs, HttpAdaptations/Recall..., Archive...                    | whole                                                                           | Recall edited                                                                                         |
| Jube.HttpAdaptationProtocol/Parsing/AdaptationResponseParser.cs, Adaptation.cs                         | whole                                                                           | parser edited                                                                                         |
| Jube.App/Endpoints/Mocks/MockHttpAdaptationEndpoints.cs                                                | grep and the failure scenarios                                                  | unauthenticated, gated by `EnableMockEndpoints`                                                       |
| Jube.Resources EntityAnalysisModelHttpAdaptationResources.resx/.cs                                     | whole                                                                           | 7 locales created                                                                                     |
| docs HTTPAdaptation, HTTPAdaptationProtocol, OutboundHostPolicy, EnvironmentVariables rows             | read / grep                                                                     |                                                                                                       |

Pull-ins: `--closure` reports none. `Jube.Preservation/Preservation.cs` (import path, lines 275, 702, 1109) was grepped
only, not claimed.

## 3. Findings

| #  | Severity                    | Where                                         | What                                                                                                                                                                                                                                                                                                            | Status                                                                                                                                                                           |
|----|-----------------------------|-----------------------------------------------|-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| 1  | High (defect, SSRF)         | EntityAnalysisModelHttpAdaptation.cs ctor     | `HttpClientHandler` default `AllowAutoRedirect=true`: a redirect from the configured host (open redirect, or a compromised service) was followed to anywhere, including 169.254.169.254 and other internal hosts.                                                                                               | Fixed: `AllowAutoRedirect=false`, `UseCookies=false`. A 3xx now fails the call.                                                                                                  |
| 2  | High (defect)               | Sync...Extensions.cs                          | `new EntityAnalysisModelHttpAdaptation(10, false, 1000)`: certificate validation always disabled and `HttpAdaptationTimeout` / `HttpAdaptationValidateSsl` (documented in EnvironmentVariables) never read.                                                                                                     | Fixed: both keys read at sync; unparseable ValidateSsl means validate, unparseable timeout means 1000.                                                                           |
| 3  | High (defect, availability) | HttpAdaptationsExtensions.cs catch and Recall | The per-adaptation catch is `when ex is not OperationCanceledException`; an HttpClient timeout is a TaskCanceledException, so it propagated out of `ExecuteHttpAdaptationsAsync` and failed the transaction. Other failures left the adaptation key absent.                                                     | Fixed: Recall catches everything and returns a suppressed `Adaptation` with an Error (timeout message distinct).                                                                 |
| 4  | Medium (defect)             | PostAsync                                     | Response size unbounded (default 2 GB buffer).                                                                                                                                                                                                                                                                  | Fixed: `MaxResponseContentBufferSize` 4 MiB (`MaximumResponseBytes`).                                                                                                            |
| 5  | Medium (defect)             | PostAsync                                     | Non-2xx body (error page, stack trace) was parsed and the full body logged at INFO.                                                                                                                                                                                                                             | Fixed: non-success throws `HttpRequestException` with the status; INFO logs the length only; parser warning truncates the body to 200 characters. Request and response disposed. |
| 6  | Medium (defence in depth)   | Sync                                          | Preservation import inserts `HttpEndpoint` verbatim, bypassing the validator's path check (dot segments, CRLF, scheme).                                                                                                                                                                                         | Fixed: `EntityAnalysisModelHttpAdaptation.IsPlainPath` re-checked at sync, offending row skipped and logged.                                                                     |
| 7  | Info                        | DTO                                           | `[ListColumn Title = "Name"]`, "HTTP Endpoint" hard-coded English.                                                                                                                                                                                                                                              | Shared TODO.                                                                                                                                                                     |
| 8  | Medium (design)             | Sync, `HttpAdaptationUrl`                     | One operator host shared by every tenant: any tenant can POST its payload to any path on it (including another tenant's scoring path, and any internal endpoint the host exposes). Intended for an R/Python scoring service; the question is whether per-tenant path allow-listing or an auth header is wanted. | Superseded: owner answered, fixed in section 12 (tenant-scoped settings).                                                                                                        |
| 9  | Medium (design)             | `HttpAdaptationValidateSsl` default           | Defaults to False. With an https `HttpAdaptationUrl` the certificate is not validated by default.                                                                                                                                                                                                               | Superseded: owner answered, default now True (section 12).                                                                                                                       |
| 10 | Medium (design)             | HttpAdaptationsExtensions.cs                  | Adaptations run sequentially, no retry, no circuit breaker: a down endpoint costs `timeout x active adaptations` per transaction. Does not stall indefinitely (timeout bounded, now enforced).                                                                                                                  | Superseded: owner answered, Jube.ResilientHttp circuit breaker added (section 12).                                                                                               |
| 11 | Low                         | PostAsync                                     | `UseProxy` left at default (environment proxy honoured); log text still says "R Plumber Hook".                                                                                                                                                                                                                  | Left, info.                                                                                                                                                                      |
| 12 | Low                         | Sync                                          | New `HttpClient` per adaptation per sync cycle, never disposed (pooled connections idle out).                                                                                                                                                                                                                   | Left, info.                                                                                                                                                                      |
| 13 | Info                        | Mock endpoints                                | Unauthenticated, `ServerError` returns 500 by design, gated by `EnableMockEndpoints` (default True, owner-documented).                                                                                                                                                                                          | Left.                                                                                                                                                                            |

Code comments were neither added nor touched. One class per file respected, no partial classes.

## 4. Fixes made

1. EntityAnalysisModelHttpAdaptation.cs: redirects off, cookies off, 4 MiB cap, success status required, disposal,
   length-only INFO log, `IsPlainPath`.
2. Sync...Extensions.cs: timeout and validate-SSL from `DynamicEnvironment`; `IsPlainPath` guard.
3. RecallHttpAdaptationEndpointExtensions.cs: failure becomes a suppressed Adaptation.
4. AdaptationResponseParser.cs: truncated body in the warning.
5. Tests added, see section 5. docs/Testing updated (parser 16 to 17, new "Http adaptation engine call" section).

## 5. Test and pen-test matrix

| Layer                   | Evidence read                                                                                                                                                                                                                                                                                                                                                                                                                        | Status                                  |
|-------------------------|--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|-----------------------------------------|
| Service CRUD (47)       | `EntityAnalysisModelHttpAdaptationServiceTests` names: insert, get, list, update with audit row, delete, forbidden for every method, not-authenticated, tenant B cannot get, update or delete tenant A, validators (name, duplicate case-insensitive, endpoint required, length, leading slash, priority), tampered ids and audit fields, locked, approval resubmit and retract, logging, spans, durations, audit line, change event | Covered                                 |
| PenTest                 | `PenTestEntityAnalysisModelHttpAdaptation` whole: `HttpEndpoint_OnlyPlainPathsOnTheConfiguredHost_AreAcceptedAsync` (allowed and refused list incl. metadata URL, schemes, dot segments, encoded dots, CRLF, backslash, NUL, NEL; create and update; echo check); plus `ModelAreaPenTestBase` matrix (names only: auth, per-verb 403, cross-tenant, mass assignment, injection, type confusion, antiforgery)                         | Covered. Not added: nothing overlapping |
| URL validation unit     | `OutboundHostPolicyTests` (19, Jube.Case) belongs to the webhook policy, not adaptation                                                                                                                                                                                                                                                                                                                                              | N/A                                     |
| Protocol                | `AdaptationTests` 13, `AdaptationResponseParserTests` 16 (+1), calibration, contribution, journey, model descriptor tests                                                                                                                                                                                                                                                                                                            | Covered by names                        |
| Engine call (new)       | `EntityAnalysisModelHttpAdaptationTests`: success, redirect not followed (second listener never contacted), 5xx throws, oversize refused, recall of 503, unreachable, timeout all give suppressed Adaptation; `IsPlainPath` theory (12 cases)                                                                                                                                                                                        | Added, unrun                            |
| Sync honouring env keys | not unit tested: needs a DB-backed sync context                                                                                                                                                                                                                                                                                                                                                                                      | Gap, listed in Not covered              |
| Front end               | none mapped                                                                                                                                                                                                                                                                                                                                                                                                                          | N/A                                     |

Tests needing the owner's serial run: `EntityAnalysisModelHttpAdaptationTests` (Unit, 8 methods, 19 cases; loopback
sockets only), `AdaptationResponseParserTests` (17), `ModelSnapshotBuilderTests` (unchanged, uses the constructor), then
`Category=Service` for `EntityAnalysisModelHttpAdaptationServiceTests` and `Category=PenTest` for
`PenTestEntityAnalysisModelHttpAdaptation` (no code in their path changed).

## 6. Translations

SPA: owns no keys; `review/model-http-adaptation.locale-proposals.json` is empty by design.

Server, closed: created `EntityAnalysisModelHttpAdaptationResources.{pl,ar,ka,id,sv,ur,hi}.resx`, 11 keys each, key
parity with the neutral file checked by parsing (11 data entries each); `.fr.resx` untouched (1 key). Machine
translation. Placeholder `{0}` preserved in `NameMaxLength` and `HttpEndpointMaxLength`. In ar and ur the literal `/` in
`HttpEndpointMustBeAbsolutePath` is wrapped in U+2068 and U+2069 so it does not flip in the RTL sentence. Digits are
Western in hi. Not checked: rendering and font coverage (ka, ur Nastaliq line height, hi Devanagari are on the shared
list), culture registration server-side, bidi isolation of endpoint values in the page (UI is the shared form model).

## 7. Docs written and TODO markers

`docs/Configuration/Models/HTTPAdaptation/index.md`: five Razor-era images re-pointed to `model-http-adaptation-*.png`
with TODO (screenshot) markers (lines 32, 39, 44, 49, 54... see grep), menu-path and label TODOs (24, 27), a new
"Security and operating behaviour" section (redirects, timeout, size, TLS, retries, failure behaviour, trust model) with
TODO (docs) markers (61, 66, 83, 84). `HTTPAdaptationProtocol/index.md`: stale source path corrected to
`Jube.HttpAdaptationProtocol/`, TODO line 267. `OutboundHostPolicy/index.md`: new "What this policy does not cover"
section, TODO line 60. Re-grep `TODO(` for exact lines.

## 8. Shared-code TODOs raised

Lines in `review/SHARED-TODO.md` under `model-http-adaptation`: duplicate `IsPlainPath` between Validations and Engine,
DTO hard-coded English titles, Svirfd to adopt Jube.ResilientHttp, and the surgical TenantRegistry hunks (section 12).

## 9. Manual test sheet

1. Permission 15 alone: list, read, create, update, delete allowed; without it all refused 403.
2. Create with `/predict`; refused: `https://x/`, `//` host forms, `/../x`, `/%2e%2e/x`, a space, a backslash, CR/LF,
   2049 characters, empty, no leading slash.
3. Duplicate name (case-insensitive) in the same model refused, same name in another model allowed. Negative priority
   refused.
4. Lock then edit and delete; maker-checker resubmit and retract.
5. Point `HttpAdaptationUrl` at the mock host, invoke a model, confirm the value in the response; use
   `/api/MockHttpAdaptation/ServerError`, malformed JSON and a never-responding path: transaction completes, adaptation
   shows an Error, log line written.
6. Point at a service that returns 302 to another host: not followed, adaptation suppressed.
7. Set `HttpAdaptationTimeout` to 200 and confirm it is honoured; set `HttpAdaptationValidateSsl=True` against a
   self-signed https host: call fails.
8. Two tenants: tenant B cannot read or edit tenant A's adaptation by id.
9. Locales en, pl, ar, ka, id, sv, ur, hi on validator messages (ar and ur: the `/` stays isolated), narrow width, dark
   theme, keyboard only.

## 10. Commit pathspecs

`slice_audit.py model-http-adaptation --paths` (slices.json unedited), plus hand-picked:
Jube.Engine/EntityAnalysisModelManager/EntityAnalysisModel/Models/Models/EntityAnalysisModelHttpAdaptation.cs,
.../Context/Extensions/SyncEntityAnalysisModelHttpAdaptationExtensions.cs (the whole file is the slice's),
Jube.Engine/EntityAnalysisModelInvoke/Context/Extensions/HttpAdaptations/RecallHttpAdaptationEndpointExtensions.cs,
Jube.HttpAdaptationProtocol/Parsing/AdaptationResponseParser.cs,
Jube.Resources/EntityAnalysisModelHttpAdaptationResources.{pl,ar,ka,id,sv,ur,hi}.resx,
Jube.Tests/Engine/EntityAnalysisModelManager/EntityAnalysisModel/Models/EntityAnalysisModelHttpAdaptationTests.cs,
Jube.Tests/HttpAdaptationProtocol/Parsing/AdaptationResponseParserTests.cs, docs (three pages),
docs/Testing/AppAndInfrastructure.md hunks (new section, parser row and count). Suggest adding
`Jube.Engine/EntityAnalysisModelManager/**/HttpAdaptation*` globs to the slice in slices.json (not edited by me).

## 11. Not covered

No build, no test run, no Rider lint. Not read: the 1090-line service beyond permissions and call sites, the 651-line
endpoint file beyond the group, `EntityAnalysisModelHttpAdaptationRepository` (296 lines), version history service,
migrations, `ModelAreaPenTestBase` bodies, `PermissionParityTests`, antiforgery. DNS rebinding, redirect to metadata and
IPv6 forms were reasoned about from the absence of tenant-controlled hosts, not tested against a live resolver.
`Preservation.cs` import path was grepped, not reviewed. Sync reading of the new env keys has no automated test. Retry,
backoff and circuit breaking do not exist (finding 10).

## 12. Owner decisions implemented (follow-up pass)

Scratch hygiene: every script in this pass was an inline heredoc (nothing written to the shared scratchpad). The earlier
resx files were re-verified against the neutral resx: key order and count identical for pl, ar, ka, id, sv, ur, hi.

### 12.1 Tenant-scoped settings (decision 1)

Convention found: there is no per-tenant settings table. Tenant records are managed by a landlord-only service
(`TenantRegistryService.RequiresLandlord`, every verb `EnsureLandlord`), so the three settings were added as nullable
columns on `TenantRegistry` (and `TenantRegistryVersion`, which the update snapshot copies with AutoMapper) and edited
through that landlord-only form. A tenant admin cannot write them. Global keys remain the fallback, resolved at sync
time per tenant (`HttpAdaptationSettings.Resolve`).

| Piece                                             | File                                                                                                                                                                                                |
|---------------------------------------------------|-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| Migration (3 columns x 2 tables, column comments) | Jube.Migrations/Branches/TenantHttpAdaptation/AddTenantHttpAdaptationSettings.cs (20263108600001, latest existing 2026110712000000)                                                                 |
| Poco                                              | Jube.Data/Poco/TenantRegistry.cs, TenantRegistryVersion.cs                                                                                                                                          |
| DTO, mapper                                       | Jube.Dto/Repository/TenantRegistry/TenantRegistryDto.cs (group HttpAdaptation; url, timeout 0 = inherit, validate-SSL default true), Jube.Service/Repository/TenantRegistry/TenantRegistryMapper.cs |
| Validator                                         | Jube.Validations/Repository/TenantRegistry/TenantRegistryDtoValidator.cs: `TenantBaseUrlPolicy.Check(url, false)` (save time is always strict), length 2048, timeout 0 or 100-30000                 |
| Messages                                          | `TenantRegistryResources` `HttpAdaptationUrlInvalid`, `HttpAdaptationTimeoutRange` in the neutral and pl, ar, ka, id, sv, ur, hi resx (fr untouched, falls back); key counts re-verified (10 each)  |
| Engine resolve                                    | Jube.Engine/.../Models/Models/HttpAdaptationSettings.cs, Sync...Extensions.cs (reads TenantRegistry once per tenant per sync, breaker key `{tenant}                                                 |{authority}`) |

SSRF guard (defence in depth, tenant URL only, because the operator-set global URL stays trusted so
`http://localhost:5001` still works for development):

1. Save: `Jube.Case/Outbound/TenantBaseUrlPolicy.cs` (new): absolute, https only, no userinfo, no query or fragment, no
   control characters or backslash, loopback, link-local (169.254.169.254), private, shared, multicast, unspecified,
   reserved and IPv6/tunnelled/mapped forms and odd spellings (via the existing `IpClassifier` and `HostSpelling`),
   `localhost` and `*.localhost`.
2. Sync: the same check again (a row written around the validator is refused and logged, its adaptations skipped); the
   global URL is checked with the relaxed flag (scheme, userinfo, query still refused).
3. Connect: `AdaptationConnector.ConnectAsync` (SocketsHttpHandler `ConnectCallback`) resolves the name, checks every
   address, and connects to the validated address, defeating rebinding. Redirects refused, proxy off, cookies off,
   strict timeout (also the connect timeout).
4. Owner-set global flag `HttpAdaptationAllowInsecureTargets` (default False) relaxes 1 to 3 for tenant URLs for
   development only.

### 12.2 TLS default (decision 2)

`HttpAdaptationValidateSsl` default is now True in `DynamicEnvironment.cs`, the tenant column treats null as True, and a
tenant with its own URL uses its own switch. Unparseable values mean validate. Everything that relied on False: a search
of the repository (excluding review notes) found only the default itself, the EnvironmentVariables doc row (updated),
the HTTPAdaptation doc (updated) and nothing in compose files, tests or CI. The dev default URL is http, so TLS does not
apply to it; a development https endpoint with a self-signed certificate now needs `HttpAdaptationValidateSsl=False`.
`EnvironmentVariablesDocumentationTests` pins code default against doc row: both updated.

### 12.3 Jube.ResilientHttp (decision 3)

New project, added to `Jube.sln` with `dotnet sln add` (not built), referenced by Jube.Engine and Jube.Tests. Structure
follows ResilientNpgsql (Polly 8.6.6, a shared static configure point, classification class, Extensions folder; no
HintPath). Generic names only.

| File                                        | Role                                                                                                                                   |
|---------------------------------------------|----------------------------------------------------------------------------------------------------------------------------------------|
| HttpResilienceOptions.cs                    | timeout, failure ratio, minimum throughput, sampling and break duration, retry budget (default 0) and delay                            |
| HttpFailureClassification.cs                | transient: 5xx, 408, 429, network and IO errors, timeouts; not transient: 4xx, 3xx, caller cancellation                                |
| ResilientHttpExecutor.cs                    | per-key pipeline cache (retry optional, circuit breaker, timeout), one breaker per caller key so tenants are isolated; `IsCircuitOpen` |
| HttpResilienceDefaults.cs                   | `ConfigureShared` / `Shared` like `PgRetryBudget`                                                                                      |
| Extensions/HttpResponseMessageExtensions.cs | `ThrowIfUnsuccessful` carrying the status                                                                                              |

Retry: default none, documented why (the adaptation POST is not known to be idempotent and a retry against a slow
endpoint multiplies cost); a small budget with exponential backoff and jitter is available for transient failures only.
Configuration keys (all in DynamicEnvironment and the EnvironmentVariables table): `ResilientHttpFailureRatio` 0.5,
`ResilientHttpMinimumThroughput` 5, `ResilientHttpSamplingDurationSeconds` 30, `ResilientHttpBreakDurationSeconds` 30,
`ResilientHttpMaxRetries` 0, `ResilientHttpRetryDelayMilliseconds` 100, plus `HttpAdaptationAllowInsecureTargets` False.
Out-of-range values are clamped to what Polly accepts. Open breaker: recall returns a suppressed Adaptation with Error
"temporarily suspended", nothing is sent. The breaker is in memory per engine process.

Svirfd has since adopted the executor (model-network follow-up, `SvirfdHost.cs`, no change to Jube.ResilientHttp was
needed). Originally: Svirfd was not touched. Recorded in `review/SHARED-TODO.md`: Svirfd (`SvirfdHost.cs`) should adopt
Jube.ResilientHttp once the model-network agent is done.

### 12.4 Tests added in this pass

| Test class                                                       | Kind    | Covers                                                                                                                                                                                                        |
|------------------------------------------------------------------|---------|---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| Jube.Tests/ResilientHttp/ResilientHttpExecutorTests.cs (12)      | Unit    | success, circuit opens and stops invoking, per-key isolation, 4xx never opens, no retry by default, retries only transient, timeout rejected, pipeline reuse, classification, `ThrowIfUnsuccessful`, defaults |
| Jube.Tests/Case/TenantBaseUrlPolicyTests.cs (8)                  | Unit    | public https accepted; scheme, userinfo, query, fragment, localhost, loopback spellings, IPv6 forms, metadata, private, multicast, unspecified refused; rebinding via resolved addresses; dev flag            |
| Jube.Tests/Engine/.../Models/HttpAdaptationSettingsTests.cs (10) | Unit    | global vs tenant resolution, fallbacks, unparseable values, stored-around-validator URL refused, dev flag, resilience defaults and clamping                                                                   |
| EntityAnalysisModelHttpAdaptationTests.cs (3 added, 11 total)    | Unit    | strict mode refuses loopback and `localhost` at connect time, breaker opens and is keyed                                                                                                                      |
| TenantRegistryServiceTests.cs (4 methods added)                  | Service | unsafe URL refused and no row written (7 URLs), timeout range, round trip and version-table snapshot, new tenant inherits                                                                                     |

`docs/Testing` regenerated rows for the new classes (AppAndInfrastructure.md new sections "Http adaptation engine call"
and "Resilient http"; ServiceRepositories.md TenantRegistry section). Tests to run in the owner's serial pass: all of
the above, `TestDocumentationTests`, `EnvironmentVariablesDocumentationTests`, `ModelSnapshotBuilderTests`,
`TenantRegistryServiceTests`, the existing PenTest and Service classes for TenantRegistry and HTTP adaptation, and a
build (the whole of section 12 is unbuilt: Jube.ResilientHttp compiled never; check
`PredicateBuilder.Handle<Exception>(Func<Exception,bool>)` and the `GetOrAdd` static-lambda overload compile).

### 12.5 Docs

HTTPAdaptation page security section rewritten (base URL sources, guard, breaker, retries), EnvironmentVariables rows
for every changed and new key, OutboundHostPolicy page boundary section. New TODO markers include a Tenants-page
screenshot and label confirmation (re-grep `TODO(`).

### 12.6 Remaining caveats

Tenants page form group title and field labels for "HttpAdaptation" need entries in FormModelResources (shared);
form-model golden or wire stamp for `TenantRegistryDto` not checked. Breaker state is per process. `UseProxy` is now off
(behaviour change for deployments reaching the scoring service through a proxy). Save-time validation ignores
`HttpAdaptationAllowInsecureTargets` by design (always strict); development uses the global URL or the flag at sync. Not
built, not run, Rider lint not run.
