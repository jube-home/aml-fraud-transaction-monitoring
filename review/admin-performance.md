# Slice review: admin-performance

Agent mode, 2026-11-08. Nothing .NET built or run. Steps 0, 2, 3, 4, 5.

## 1. Summary and verdict

READY AFTER OWNER ACTIONS. Priority 1: all 43 performance and monitoring grid areas are correctly landlord-only and
GridModelAccess resolves each one as such; only UserLogin and UserLogout (login-audit) need permission 35. No area
became landlord-only by accident and none is too open. Evidence per area: every `{Area}Service` has
`RequiresLandlord => true` and `EnsurePermitted` that checks `permissionValidation.Landlord`; the menu entry is
`landlord: 'only'` (menuTree.ts:143); the page shows a denied alert for non-landlords; service Descriptions say
"Landlord only"; pen tests (PenTestObservabilityLists.PermissionMatrix_MatchesSpecAsync,
MonitoringListTestBase.AuthorizationMatrixAsync) assert a tenant user holding every permission gets 403 and the landlord
200. One real defect found and fixed: the WAF attack log stored and returned the full flagged request field value
unredacted (passwords, tokens could be persisted and shown).

## 2. Files reviewed

Read whole: GridModelAccess.cs, GridModelAuthorizationService.cs, GridModelEndpoints.cs, AdminPerformancePage.vue,
performance/families.ts, address.ts, PerformanceCard.vue, PayloadGrid.vue (inherited from data-grid, read for polling
and refresh), RespMetricEndpoints.cs, PostgresActivityEndpoints.cs, HttpProcessingCounterService.cs,
PostgresActivityService.cs and repository, WafAttackService.cs (list), GetArchiverStagePerformanceCounterListQuery.cs,
ModelInvokeWarningService.cs (header and list), SensitiveTextRedactor.cs, LogTextRedactor.cs, WafInspector.cs. Scripted
across all 43: service `RequiresLandlord`/permissions, `Math.Clamp(take)`, Landlord check, endpoint shape equivalence
(24 distinct shapes by normalised text, all RequireAuthorization, Forbid on both exceptions, GET only), repository
search and sort (grep: every search is a parameterised LINQ `Contains`, every sort a `switch` whitelist, no reflection
or string SQL). Not line-by-line: the other 38 services and repositories beyond the scripted checks.

## 3. Findings

| #  | Sev    | Where                                         | What                                                                                                                                                  | Status                                                                                              |
|----|--------|-----------------------------------------------|-------------------------------------------------------------------------------------------------------------------------------------------------------|-----------------------------------------------------------------------------------------------------|
| 1  | None   | GridModelAccess                               | 43 landlord, 2 permission 35: correct (see summary)                                                                                                   | Verified                                                                                            |
| 2  | Low    | GridModelAccess                               | Unknown vs renamed service both failed closed to landlord, indistinguishable                                                                          | Fixed: `IsKnown`, tests                                                                             |
| 3  | Medium | WafAttackFlushService.cs, WafAttackService.cs | MatchedValue is the whole flagged field value (WafInspector 69), up to 512 chars, stored and returned raw                                             | Fixed: redacted at capture and on read                                                              |
| 4  | Info   | repositories                                  | `                                                                                                                                                     |                                                                                                     | !TenantRegistryId.HasValue` pattern: none in my files (grep); tenant restriction via `includeAllTenants` is always true after the landlord gate | None |
| 5  | Info   | list queries                                  | statistics load up to 100000 rows plus COUNT per request                                                                                              | design, SHARED-TODO                                                                                 |
| 3b | Low    | WafAttackFlushService.cs, WafAttackService.cs | Route and MatchedField (JSON property path, attacker named, unbounded) stored raw and uncapped                                                        | Fixed: `Clean` (redact then cap 512) at capture; Route, MatchedField, MatchedValue redacted on read |
| 3c | Info   | repo-searched logs                            | search matches raw stored text, display is redacted (probe oracle for a trusted landlord); CaseKeyValue and Message show business keys across tenants | design, SHARED-TODO                                                                                 |
| 6  | Info   | PerformanceCard.vue                           | `openLabel` prop unused                                                                                                                               | Left                                                                                                |
| 7  | Info   | PayloadGrid                                   | no polling, refresh is manual only; cells rendered via textContent so stored XSS inert                                                                | Verified                                                                                            |
| 8  | Info   | exports                                       | browser-side CSV/xlsx, formula neutralised per data-grid.md                                                                                           | Inherited                                                                                           |
| 9  | Info   | SelectToken                                   | none in slice files (grep)                                                                                                                            | None                                                                                                |

## 4. Fixes made

1. GridModelAccess.cs: `IsKnown(area)`, `FindType`.
2. WafAttackService.cs: `LogTextRedactor.Redact(r.MatchedValue)`. WafAttackFlushService.cs:
   `SensitiveTextRedactor.Redact` at capture.
3. 280 resx: `{Area}Resources.{pl,ar,ka,id,sv,ur,hi}.resx` for the 40 slice classes lacking them (copied from
   GridModelResources translations, identical two keys, parity verified; list in scratchpad
   admin-performance/written.txt, all `{Area}Resources.*`). .fr untouched.
4. Docs section added.
5. review/admin-performance.locale-proposals.json (ka, id, sv, ur, hi; 77 keys each).

## 5. Test and pen-test matrix

| Layer                              | Evidence                                                                                                                                                                                            | Status                                              |
|------------------------------------|-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|-----------------------------------------------------|
| Area to service, requirement table | NEW Jube.Tests/Service/Query/GridModel/GridModelAccessTests.cs (Unit): registry equals pinned list, each area resolves to exactly one service, 43 landlord rows, 2 permission-35 rows, unknown area | Added                                               |
| Existing                           | GridModelAuthorizationServiceTests (read), GridModelRegistryTests                                                                                                                                   | Covered                                             |
| Authz, param abuse, consumption    | Monitoring/PenTestRespMonitoring, PenTestPostgresMonitoring, PenTestPlatformMonitoring; Observability/PenTestObservabilityLists, ObservabilityAreas (read)                                          | Covered; HAProxy areas only in Observability matrix |
| Secret redaction                   | PenTestObservabilityContent TextAreas lacks WafAttack                                                                                                                                               | SHARED-TODO                                         |
| WAF redaction service test         | NEW Jube.Tests/Service/WafAttack/WafAttackServiceTests.cs (Service)                                                                                                                                 | Added                                               |
| Vue                                | performance/__tests__ address.spec, families.spec (12 pass, run); e2e adminPerformance.spec read (not run)                                                                                          | Covered                                             |

## 6. Translations

SPA perfHub (63) and payloadGrid (14): pl and ar complete. Proposals for ka/id/sv/ur/hi in
review/admin-performance.locale-proposals.json. Server: 280 resx created. Hard-coded English: grid titles from DTO
`[GridEndpoint(Title)]` (data-grid shared TODO).

## 7. Docs

docs/Configuration/Administration/PerformanceCounterBalance/index.md, new "The Performance page" section: 3 screenshot
TODOs and 4 docs TODOs (find with grep "TODO" at the end of the file).

## 8. Shared TODOs raised

Four lines in review/SHARED-TODO.md (slices.json claims, WafAttack pen test, locale JSON, statistics cost).

## 9. Manual test sheet

Landlord: six families, drill, deep link, Back; tenant admin: menu hidden, direct URL shows denied; each grid: filter,
sort, export, refresh; WAF log with a field named password; ar/ur mirror.

## 10. Commit pathspecs

Jube.Service/Query/GridModel/GridModelAccess.cs, Jube.Service/WafAttack/WafAttackService.cs,
Jube.App/Code/Waf/WafAttackFlushService.cs, the two new tests, the 280 resx, docs page, review files.

## 11. Dotnet tests to run

GridModelAccessTests (Unit), WafAttackServiceTests (Service), GridModelAuthorizationServiceTests,
PenTestObservabilityLists.

## 12. Pattern read of the 43 services (closing the earlier gap)

Method: all 43 `EnsurePermitted` bodies normalised and compared (39 identical, 4 differ only by line wrapping: all
`if (permissionValidation.Landlord) return; ... throw ForbiddenException`); `Math.Clamp(take,1,100000)`, last-hour
default for from and to, and repository call shapes extracted for every service; the full text of each service diffed
against its pattern representative (differences are only DTO projection and filter parameters, 23 to 99 lines each).
Representatives read in full: HttpProcessingCounterService (B), ApplicationLogEntryService list and
ModelInvokeWarningService (C), GetArchiverStagePerformanceCounterListQuery and its service (A), PostgresActivityService
and repository (D). Repositories: every search is parameterised LINQ, every sort a switch whitelist (grep of all 41
repositories and query classes).

| Pattern                                                                                                         | Services                                                                                                                                                                                                                                                                                                               | Notes                                                                                      |
|-----------------------------------------------------------------------------------------------------------------|------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|--------------------------------------------------------------------------------------------|
| A. Query class, model counters, `includeAllTenants` = Landlord (always true after the gate), join to model name | ArchiverStagePerformanceCounter, ArchiverWarning, EntityAnalysisModelStagePerformanceCounter, EntityAnalysisModelResponseTimePipelineCounter, EntityAnalysisModelTaskPerformanceCounter, EntityAnalysisModelProcessingCounter, EntityAnalysisModelAsynchronousQueueBalance                                             | numeric plus model name; nothing sensitive                                                 |
| A2. Query class, platform metrics                                                                               | EntityAnalysisAsynchronousQueueBalance, DockerContainerMetric, DockerHostMetric, DotNetRuntimeMetric                                                                                                                                                                                                                   | container Name and Image are infrastructure names                                          |
| B. Repository numeric rollups                                                                                   | HttpProcessingCounter, CaptureQueueHealth, OtlpDispatchCounter, RespCallCounter, RespMetric, PostgresMetric, CaseCreationStagePerformanceCounter, RespConnectionMultiplexerMetric, OpenTelemetryMetric                                                                                                                 | OpenTelemetryMetric Tags is admin-telemetry's page, not redacted, left to that slice       |
| C. Repository free-text logs, redacted                                                                          | ApplicationLogEntry, ContainerLogEntry, PostgresLogEntry, ModelInvokeWarning, CaseCreationWarning (Destination), EtcdClusterEvent, EtcdMemberStatus, PatroniClusterEvent, PatroniMemberStatus (ApiUrl), RespConnectionEvent, RespSentinelEvent, RespSlowOperation (command and key redacted), HaProxyReachabilityProbe | Redact on Message, Exception, URLs, values; verified per service by grep of the projection |
| C2. Repository infrastructure state, no free text from outside                                                  | RespSentinelStatus, HaProxyServerStatus, DockerEvent, OverlayNetworkTaskDrift, PostgresReplicationStatus                                                                                                                                                                                                               | addresses, names, flags only                                                               |
| C3. Attacker-controlled input                                                                                   | WafAttack                                                                                                                                                                                                                                                                                                              | fixed (see findings)                                                                       |
| D. Live snapshot, no take, bounded by the source view                                                           | PostgresActivity, PostgresStatementStatistics (query redacted before search), PostgresIndexStatistics, PostgresTableStatistics                                                                                                                                                                                         | row count bounded by pg_stat views                                                         |

No list returns request bodies, user agents or tokens other than the WAF value; RemoteIp comes from
`Connection.RemoteIpAddress`, not a header.

## 13. Additional changes in this pass

PenTestObservabilityContent.cs: WafAttack added to TextAreas, SeedTextAsync (own Int64 insert and cleanup because the
shared InsertAsync builds an Int32 id comparison) and TextFields; now exercised by
StoredHostileText_IsReturnedAsInertJsonDataAsync and SecretsInStoredText_AreNotReturnedAsync. File was unmodified since
2026-11-07, no other agent in it.

## 14. Not covered

Rider lint (errors only): clean for GridModelAccess.cs, GridModelAccessTests.cs, WafAttackService.cs,
WafAttackFlushService.cs, WafAttackServiceTests.cs; PenTestObservabilityContent.cs timed out (unlinted). Nothing built
or run; RTL not seen running; OpenTelemetryMetric tags not reviewed (admin-telemetry). Tests to run:
GridModelAccessTests, WafAttackServiceTests, GridModelAuthorizationServiceTests, PenTestObservabilityLists,
PenTestObservabilityContent (WafAttack rows), WafInspectorTests.
