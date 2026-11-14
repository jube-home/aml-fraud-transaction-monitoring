# Connection and host lifetime in Jube.Tests

Scope: every creator of a DbContext, engine host, CacheService, task starter, multiplexer, HttpClient or web host under
Jube.Tests. Tests were not built or run.

## Cause of the pool exhaustion

1. `fx.GetDbContext().Connection.ConnectionString` was used in 5 classes (11 call sites: GraphRecallLoaderTests,
   GraphBuildJobTests, GraphJobsTests, GraphAnalyticsTests, GraphServiceTests). `DataConnection.Connection` opens the
   connection and the context was never disposed, so each call pinned one pooled connection for the life of the process.
   Fixed: `fx.ConnectionString` (already public).
2. `CachePayloadRepository.CreateLocalCacheInstanceAsync` (production, Jube.Cache) built a DbContext inline and never
   disposed it. Every `CacheService.StartAsync` (one per engine host, plus
   EntityAnalysisModelHistorySimulationServiceTests per test) leaked one connection whose last statement was
   `INSERT INTO "LocalCacheInstance"`. This explains the 27. Fixed with `await using var dbContext`.
3. `ModelEngineHost.DisposeAsync` cancelled the token and waited only for `Engine.StartAsync()` (which returns once
   tasks are launched), never for the TaskCoordinator tasks, and never disposed the CacheService multiplexer. About 20
   hosts (ModelScaffoldFixture subclasses plus Sanctions poll tests) each started InfrastructureHealthMetricsStarter,
   whose PostgresMetricSampler and PostgresLogSampler each hold one connection until disposed. The starter never
   disposes them (production, Jube.Engine, other agent). 20 hosts x 2 samplers = the 40 idle sampler connections. Test
   side fixed: hosts now await `WaitForAllAsync` (30 s cap) and dispose the multiplexers. The sampler disposal itself
   must be done in production.
4. No setting disables InfrastructureHealthMetricsStarter (`Engine.StartAsync` calls it unconditionally;
   DynamicEnvironment has only `EnableInfrastructureHealthMetricsPurge`). Not added: it is production code under
   Jube.Engine plus a documented env var (EnvironmentVariablesDocumentationTests). Recommended follow-up:
   `EnableInfrastructureHealthMetrics` default True, set False in `ModelEngineHost.DefaultSettings`.

## CaseRiskSweepTests.DisposeAsync line 48

Line 48 is `await seeder.CleanAsync(dbContext);`. `fx.GetDbContext()` on line 47 is lazy; the first query in
`CleanAsync` (the `CaseWorkflowStatusRole` DeleteAsync) is where the connection is opened. By then the pool (100) was
held by the leaks above, so Npgsql waited its 15 s timeout and threw
`InvalidOperationException: The connection pool has been exhausted`. It is a victim, not a cause: its own code is
correct (`await using`). Consequence of the throw: cleanup did not run, leaving seeded rows behind, and xunit reports it
against every test in the class. All ten named classes plus everything under CaseRisk, CaseSupervision and
BackgroundTasks use `await using var` or disposal in DisposeAsync; none leak.

## Classes examined

| Class or group                                                                                                                                                                                                                                                                                                                                | Creates                                                     | Verdict                                                                                               | Fix                                           |
|-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|-------------------------------------------------------------|-------------------------------------------------------------------------------------------------------|-----------------------------------------------|
| GraphRecallLoaderTests, GraphBuildJobTests, GraphJobsTests, GraphAnalyticsTests, GraphServiceTests                                                                                                                                                                                                                                            | DbContext opened via `.Connection` and never disposed       | LEAK                                                                                                  | `fx.ConnectionString`                         |
| ModelEngineHost (used by ModelScaffoldFixture and all subclasses, SanctionsChangePoll*Tests)                                                                                                                                                                                                                                                  | CacheService, TaskCoordinator, Engine, Svirfd               | LEAK (tasks not awaited, multiplexer not disposed)                                                    | WaitForAll with timeout, dispose multiplexers |
| CachePayloadRepository.CreateLocalCacheInstanceAsync (production)                                                                                                                                                                                                                                                                             | DbContext                                                   | LEAK                                                                                                  | `await using`                                 |
| EntityAnalysisModelHistorySimulationServiceTests                                                                                                                                                                                                                                                                                              | CacheService, TaskCoordinator per test                      | LEAK (multiplexer, token never cancelled, plus local cache row)                                       | cancel token, dispose multiplexer             |
| OverrideModelSyncTests, ReprocessingTaskStarterTests, BacktestHeartbeatTests, CaseRiskRecheckTriggerTests, CaseRiskSweepTests, CaseChecklistReadModelTests, CaseSupervisionServiceTests, CaseAnalyticsServiceTests, EntityAnalysisModelSyncWatermarkQueryPostgresBackedTests, rest of CaseRisk, CaseSupervision, BackgroundTasks/TaskStarters | DbContexts, starters                                        | OK, cascade victims                                                                                   | none                                          |
| ~60 service tests with `var dbContext = fx.GetDbContext()`                                                                                                                                                                                                                                                                                    | DbContext                                                   | OK, each disposed explicitly (failure-logging tests) or tracked in `openContexts`                     | none                                          |
| ServiceBoundToolCoverageTests                                                                                                                                                                                                                                                                                                                 | DbContext inside ServiceAgentBindingContext, never disposed | OK in practice, binding runs no query so no connection opens                                          | none                                          |
| AdvancedAnalytics test bases, DatabaseFixture, SanctionsSdnLoadFixture, ModelScaffold, ModelSync, QueryPlan                                                                                                                                                                                                                                   | DbContext, NpgsqlConnection                                 | OK                                                                                                    | none                                          |
| ModelInvokeHost, AuthApiHost, MocksHost, Wire e2e tests, PenTestHost, PenTestMockEndpointsGate, PenTestWafSeededSignatures, PenTestInventory                                                                                                                                                                                                  | WebApplication, HttpClient                                  | OK (stopped and disposed)                                                                             | none                                          |
| InvokeEndpointTests multi-host tests                                                                                                                                                                                                                                                                                                          | several ModelInvokeHost                                     | Minor: if the first assertion fails the later host is not disposed (Kestrel listener only, no DB)     | left                                          |
| Svirfd tests                                                                                                                                                                                                                                                                                                                                  | SvirfdHost                                                  | OK (`await using`)                                                                                    | none                                          |
| TtlCounterDeprecationTimingTests, TtlCounterModelSyncTests                                                                                                                                                                                                                                                                                    | TaskStarter loops                                           | Low risk: token cancelled mid-test, not in `finally`, so an exception before it leaves a loop running | left                                          |
| RespKeyActivityRepositoryTests, ReprocessingTaskStarterTests                                                                                                                                                                                                                                                                                  | ConnectionMultiplexer                                       | OK (`await using`)                                                                                    | none                                          |

## Flaky patterns

Fixed:

- Engine tasks outliving `ModelEngineHost.DisposeAsync`. SanctionsChangePoll*Tests set the static
  `SanctionsTaskStarter.ChangeMarkerReader` and assert on static `markerReads` and `LightRefreshCount`; a previous
  test's engine still polling could bump the next test's counters. Now awaited.

Left, with reason:

- `TestDynamicEnvironment.Create` mutates process env vars under a lock; readers falling back to `ConnectionString` can
  still see `unit-test-ConnectionString` when `JubeTestConnectionString` is unset. Fix needs an injection seam in
  DynamicEnvironment (production). Workaround stays: export `JubeTestConnectionString`.
- Real-timer tests with fixed sleeps and tight windows: BacktestHeartbeatTests (250 ms beat, `BeCloseTo` 1-2 s),
  TtlCounterDeprecationTimingTests, SanctionsChangePollConfigurationTests (3 s negative waits), SyncScheduleEngineTests.
  They are inherently time-based; replacing them needs production clock seams, and adding polling would be a retry.
  Under pool starvation they fail as a cascade.
- Unseeded `Random.Shared`: CaseSuppressionSeed IP (1 in 62,500 collision), CaseKeyJournal random bigint (negligible),
  `Task.Delay(Random...)` jitter (harmless).
- Env var mutation: EvalExpressionRegistry classes share a collection (safe); CaseHold/ResponseHold use unique names
  inside Database collection (safe).
- Collections: every DB-backed class under the filter carries `[Collection("Database")]`, directly or via a base class;
  no gaps found. PenTest classes use their own collection and were not audited here.
- Shared static `PenTestHost` is process-lifetime by design, stopped on fixture swap and ProcessExit.
