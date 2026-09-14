/* Copyright (C) 2022-present Jube Holdings Limited.
 *
 * This file is part of Jube™ software.
 *
 * Jube™ is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License
 * as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
 * Jube™ is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty
 * of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.

 * You should have received a copy of the GNU Affero General Public License along with Jube™. If not,
 * see <https://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Jube.Cache;
using Jube.Cache.Observability.CacheCallCounters;
using Jube.Data.Context;
using Jube.Data.Poco;
using Jube.Data.Repository;
using Jube.Engine.BackgroundTasks.TaskStarters.Metrics;
using Jube.Engine.BackgroundTasks.TaskStarters.Metrics.OpenTelemetry;
using Jube.Engine.Observability;

namespace Jube.Engine.BackgroundTasks.TaskStarters
{
    public class InfrastructureHealthMetricsStarter(Context.Context context)
    {
        private const int SampleIntervalMilliseconds = 60000;
        private const int GroupTimeoutMilliseconds = 45000;
        private readonly DotNetRuntimeMetricSampler dotNetRuntimeMetricSampler = new();
        private readonly EtcdMetricSampler etcdMetricSampler = new();
        private readonly EtcdPatroniDiscovery etcdPatroniDiscovery = new();
        private readonly PatroniMetricSampler patroniMetricSampler = new();
        private readonly RedisConnectionMultiplexerMetricSampler redisConnectionMultiplexerMetricSampler = new();
        private readonly RedisSentinelStatusSampler redisSentinelStatusSampler = new();
        private bool postgresLogEntryLoggingCollectorWarningLogged;
        private PostgresLogSampler postgresLogSampler;
        private PostgresMetricSampler postgresMetricSampler;
        private RedisMetricSampler redisMetricSampler;

        public async Task StartAsync()
        {
            try
            {
                postgresMetricSampler =
                    new PostgresMetricSampler(context.Services.DynamicEnvironment.AppSettings("ConnectionString"));

                postgresLogSampler =
                    new PostgresLogSampler(context.Services.DynamicEnvironment.AppSettings("ConnectionString"));

                redisMetricSampler =
                    new RedisMetricSampler(context.Services.DynamicEnvironment.AppSettings("RedisConnectionString"));

                while (!context.Services.TaskCoordinator.CancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        await SampleAndFlushAsync(context.Services.TaskCoordinator.CancellationToken)
                            .ConfigureAwait(false);

                        await Task.Delay(SampleIntervalMilliseconds, context.Services.TaskCoordinator.CancellationToken)
                            .ConfigureAwait(false);
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        context.Services.Log.Error(
                            $"InfrastructureHealthMetricsStarter: An error in infrastructure health metrics sampling has been observed as {ex}.");
                    }
                }
            }
            catch (OperationCanceledException ex)
            {
                context.Services.Log.Info(
                    $"Graceful Cancellation InfrastructureHealthMetricsStarter: has produced an error {ex}");
            }
            catch (Exception ex)
            {
                context.Services.Log.Error($"InfrastructureHealthMetricsStarter: has produced an error {ex}");
            }
        }

        private async Task SampleAndFlushAsync(CancellationToken token)
        {
            var instance = Dns.GetHostName();
            var createdDate = DateTime.UtcNow;

            using var cycleActivity = EngineDiagnostics.ActivitySource.StartActivity();
            cycleActivity?.SetTag("jube.instance", instance);
            EngineDiagnostics.TagCurrentCodeLocation(cycleActivity);

            var etcdEndpointsTask =
                etcdPatroniDiscovery.DiscoverEtcdEndpointsAsync(context.Services.DynamicEnvironment, token);

            Task[] groups =
            [
                RunGroupAsync((dbContext, groupToken) =>
                    FlushHttpProcessingCounterAsync(dbContext, instance, createdDate, groupToken), token),
                RunGroupAsync((dbContext, groupToken) =>
                    FlushQueueBalanceAsync(dbContext, instance, createdDate, groupToken), token),
                RunGroupAsync((dbContext, groupToken) =>
                    FlushDotNetRuntimeMetricAsync(dbContext, instance, createdDate, groupToken), token),
                RunGroupAsync(async (dbContext, groupToken) =>
                {
                    await FlushPostgresMetricAsync(dbContext, instance, createdDate, groupToken)
                        .ConfigureAwait(false);
                    await FlushPostgresReplicationStatusAsync(dbContext, instance, createdDate, groupToken)
                        .ConfigureAwait(false);
                }, token),
                RunGroupAsync(async (dbContext, groupToken) =>
                {
                    await FlushRedisMetricAsync(dbContext, instance, createdDate, groupToken).ConfigureAwait(false);
                    await FlushRedisSlowOperationsAsync(dbContext, instance, createdDate, groupToken)
                        .ConfigureAwait(false);
                    await FlushRedisSentinelStatusAsync(dbContext, instance, createdDate, groupToken)
                        .ConfigureAwait(false);
                }, token),
                RunGroupAsync((dbContext, groupToken) =>
                    FlushRedisConnectionMultiplexerMetricAsync(dbContext, instance, createdDate, groupToken), token),
                RunGroupAsync((dbContext, groupToken) =>
                    FlushRedisConnectionEventsAsync(dbContext, instance, createdDate, groupToken), token),
                RunGroupAsync((dbContext, groupToken) =>
                    FlushRedisSentinelEventsAsync(dbContext, instance, createdDate, groupToken), token),
                RunGroupAsync((dbContext, groupToken) =>
                    FlushModelInvokeWarningsAsync(dbContext, instance, createdDate, groupToken), token),
                RunGroupAsync((dbContext, groupToken) =>
                        FlushCaseCreationStagePerformanceCountersAsync(dbContext, instance, createdDate, groupToken),
                    token),
                RunGroupAsync((dbContext, groupToken) =>
                    FlushCaseCreationWarningsAsync(dbContext, instance, createdDate, groupToken), token),
                RunGroupAsync((dbContext, groupToken) =>
                    FlushArchiverWarningsAsync(dbContext, instance, createdDate, groupToken), token),
                RunGroupAsync((dbContext, groupToken) =>
                    FlushCaptureQueueHealthAsync(dbContext, instance, createdDate, groupToken), token),
                RunGroupAsync((dbContext, groupToken) =>
                    FlushRedisCallCountersAsync(dbContext, instance, createdDate, groupToken), token),
#pragma warning disable VSTHRD003 // etcdEndpointsTask is deliberately started once above and shared by both groups
                RunGroupAsync((dbContext, groupToken) =>
                    FlushEtcdMemberStatusAsync(dbContext, instance, createdDate, etcdEndpointsTask, groupToken), token),
                RunGroupAsync((dbContext, groupToken) =>
                        FlushPatroniMemberStatusAsync(dbContext, instance, createdDate, etcdEndpointsTask, groupToken),
                    token),
#pragma warning restore VSTHRD003
                RunGroupAsync((dbContext, groupToken) =>
                    FlushOpenTelemetryMetricsAsync(dbContext, instance, createdDate, groupToken), token),
                RunGroupAsync((dbContext, groupToken) =>
                    FlushPostgresLogEntryAsync(dbContext, instance, createdDate, groupToken), token)
            ];

            await Task.WhenAll(groups).ConfigureAwait(false);
        }

        private async Task RunGroupAsync(Func<DbContext, CancellationToken, Task> group, CancellationToken token)
        {
            var dbContext = DataConnectionDbContext.GetResilientDbContextDataConnection(
                context.Services.DynamicEnvironment.AppSettings("ConnectionString"), context.Services.Log);

            using var deadline = CancellationTokenSource.CreateLinkedTokenSource(token);
            deadline.CancelAfter(GroupTimeoutMilliseconds);
            try
            {
                await group(dbContext, deadline.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (!token.IsCancellationRequested)
            {
                context.Services.Log.Error(
                    $"InfrastructureHealthMetricsStarter: A sampler group did not complete within " +
                    $"{GroupTimeoutMilliseconds}ms and was abandoned so the sampling cycle could proceed. " +
                    "This usually means a dependency (Postgres, Redis, etcd or Patroni) is slow or unreachable.");
            }
            finally
            {
                await dbContext.CloseAsync(token).ConfigureAwait(false);
                await dbContext.DisposeAsync(token).ConfigureAwait(false);
            }
        }

        private async Task FlushHttpProcessingCounterAsync(DbContext dbContext, string instance, DateTime createdDate,
            CancellationToken token)
        {
            using var scope = SamplerScope.Start("HttpProcessingCounter", instance);
            try
            {
                var model = new HttpProcessingCounter
                {
                    Instance = instance,
                    All = context.Counters.HttpCounterAllRequests,
                    Model = context.Counters.HttpCounterModel,
                    AsynchronousModel = context.Counters.HttpCounterModelAsync,
                    Error = context.Counters.HttpCounterAllError,
                    Tag = context.Counters.HttpCounterTag,
                    Sanction = context.Counters.HttpCounterSanction,
                    Callback = context.Counters.HttpCounterCallback,
                    CallbackTimeout = context.Counters.HttpCounterCallbackTimeout,
                    Exhaustive = context.Counters.HttpCounterExhaustive,
                    CreatedDate = createdDate
                };

                var repository = new HttpProcessingCounterRepository(dbContext);
                await repository.InsertAsync(model, token).ConfigureAwait(false);

                Interlocked.Exchange(ref context.Counters.HttpCounterAllRequests, 0);
                Interlocked.Exchange(ref context.Counters.HttpCounterModel, 0);
                Interlocked.Exchange(ref context.Counters.HttpCounterModelAsync, 0);
                Interlocked.Exchange(ref context.Counters.HttpCounterAllError, 0);
                Interlocked.Exchange(ref context.Counters.HttpCounterTag, 0);
                Interlocked.Exchange(ref context.Counters.HttpCounterSanction, 0);
                Interlocked.Exchange(ref context.Counters.HttpCounterCallback, 0);
                Interlocked.Exchange(ref context.Counters.HttpCounterCallbackTimeout, 0);
                Interlocked.Exchange(ref context.Counters.HttpCounterExhaustive, 0);

                scope.Rows(1);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                scope.Error(ex);
                context.Services.Log.Error(
                    $"InfrastructureHealthMetricsStarter: An error flushing HTTP processing counters has been observed as {ex}.");
            }
        }

        private async Task FlushQueueBalanceAsync(DbContext dbContext, string instance, DateTime createdDate,
            CancellationToken token)
        {
            using var scope = SamplerScope.Start("EntityAnalysisAsynchronousQueueBalance", instance);
            try
            {
                var model = new EntityAnalysisAsynchronousQueueBalance
                {
                    AsynchronousInvoke = context.ConcurrentQueues.PendingEntityInvoke.Count,
                    AsynchronousCallback =
                        context.Services.CacheService.CacheCallbackPublishSubscribe.Callbacks.Count,
                    CaseCreation = context.ConcurrentQueues.PendingCases.Count,
                    Tagging = context.ConcurrentQueues.PendingTagging.Count,
                    Notification = context.ConcurrentQueues.PendingNotifications.Count,
                    CreatedDate = createdDate,
                    Instance = instance,
                    AsynchronousImplicitAsyncOverdue = context.Services.ImplicitAsyncInvocationTracker.PendingCount
                };

                var repository = new EntityAnalysisAsynchronousQueueBalanceRepository(dbContext);
                await repository.InsertAsync(model, token).ConfigureAwait(false);
                scope.Rows(1);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                scope.Error(ex);
                context.Services.Log.Error(
                    $"InfrastructureHealthMetricsStarter: An error flushing the asynchronous queue balance has been observed as {ex}.");
            }
        }

        private async Task FlushDotNetRuntimeMetricAsync(DbContext dbContext, string instance, DateTime createdDate,
            CancellationToken token)
        {
            using var scope = SamplerScope.Start("DotNetRuntimeMetric", instance);
            try
            {
                var metric = dotNetRuntimeMetricSampler.Sample();
                metric.Instance = instance;
                metric.CreatedDate = createdDate;

                var repository = new DotNetRuntimeMetricRepository(dbContext);
                await repository.InsertAsync(metric, token).ConfigureAwait(false);
                scope.Rows(1);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                scope.Error(ex);
                context.Services.Log.Error(
                    $"InfrastructureHealthMetricsStarter: An error sampling .NET runtime metrics has been observed as {ex}.");
            }
        }

        private async Task FlushPostgresMetricAsync(DbContext dbContext, string instance, DateTime createdDate,
            CancellationToken token)
        {
            using var scope = SamplerScope.Start("PostgresMetric", instance);
            try
            {
                var metric = await postgresMetricSampler.SampleAsync(token).ConfigureAwait(false);
                if (metric == null)
                {
                    scope.Skipped();
                    return;
                }

                metric.Instance = instance;
                metric.CreatedDate = createdDate;

                var repository = new PostgresMetricRepository(dbContext);
                await repository.InsertAsync(metric, token).ConfigureAwait(false);
                scope.Rows(1);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                scope.Error(ex);
                context.Services.Log.Error(
                    $"InfrastructureHealthMetricsStarter: An error sampling Postgres metrics has been observed as {ex}.");
            }
        }

        private async Task FlushPostgresReplicationStatusAsync(DbContext dbContext, string instance,
            DateTime createdDate, CancellationToken token)
        {
            using var scope = SamplerScope.Start("PostgresReplicationStatus", instance);
            try
            {
                var replicas = await postgresMetricSampler.SampleReplicationAsync(token).ConfigureAwait(false);
                if (replicas.Count == 0)
                {
                    scope.Skipped();
                    return;
                }

                var repository = new PostgresReplicationStatusRepository(dbContext);
                foreach (var replica in replicas)
                {
                    replica.OccurredDate = createdDate;
                    replica.Instance = instance;
                    replica.CreatedDate = createdDate;
                    await repository.InsertAsync(replica, token).ConfigureAwait(false);
                }

                scope.Rows(replicas.Count);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                scope.Error(ex);
                context.Services.Log.Error(
                    $"InfrastructureHealthMetricsStarter: An error sampling Postgres replication status has been observed as {ex}.");
            }
        }

        private async Task FlushPostgresLogEntryAsync(DbContext dbContext, string instance, DateTime createdDate,
            CancellationToken token)
        {
            using var scope = SamplerScope.Start("PostgresLogEntry", instance);
            try
            {
                var entries = await postgresLogSampler.SampleAsync(token).ConfigureAwait(false);

                if (postgresLogSampler.LoggingCollectorOff)
                {
                    if (!postgresLogEntryLoggingCollectorWarningLogged)
                    {
                        postgresLogEntryLoggingCollectorWarningLogged = true;
                        context.Services.Log.Warn(
                            "InfrastructureHealthMetricsStarter: PostgresLogEntry capture requires " +
                            "logging_collector = on server-side; it is currently off, so this table will stay " +
                            "empty until it is enabled and Postgres is restarted. This warning is logged once.");
                    }

                    scope.Skipped();
                    return;
                }

                if (entries == null || entries.Count == 0)
                {
                    scope.Skipped();
                    return;
                }

                foreach (var entry in entries)
                {
                    entry.Instance = instance;
                    entry.CreatedDate = createdDate;
                    context.Services.LogCounterRuleCache?.CheckAndIncrement(entry.Message);
                }

                var repository = new PostgresLogEntryRepository(dbContext);
                await repository.BulkCopyAsync(entries, token).ConfigureAwait(false);
                scope.Rows(entries.Count);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                scope.Error(ex);
                context.Services.Log.Error(
                    $"InfrastructureHealthMetricsStarter: An error sampling Postgres log entries has been observed as {ex}.");
            }
        }

        private async Task FlushRedisMetricAsync(DbContext dbContext, string instance, DateTime createdDate,
            CancellationToken token)
        {
            using var scope = SamplerScope.Start("RedisMetric", instance);
            try
            {
                var metric = await redisMetricSampler.SampleAsync().ConfigureAwait(false);
                if (metric == null)
                {
                    scope.Skipped();
                    return;
                }

                metric.Instance = instance;
                metric.CreatedDate = createdDate;

                var repository = new RedisMetricRepository(dbContext);
                await repository.InsertAsync(metric, token).ConfigureAwait(false);
                scope.Rows(1);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                scope.Error(ex);
                context.Services.Log.Error(
                    $"InfrastructureHealthMetricsStarter: An error sampling Redis metrics has been observed as {ex}.");
            }
        }

        private async Task FlushRedisSlowOperationsAsync(DbContext dbContext, string instance, DateTime createdDate,
            CancellationToken token)
        {
            using var scope = SamplerScope.Start("RedisSlowOperation", instance);
            try
            {
                var slowOperations = await redisMetricSampler.SampleSlowOperationsAsync().ConfigureAwait(false);
                if (slowOperations.Count == 0)
                {
                    scope.Skipped();
                    return;
                }

                var repository = new RedisSlowOperationRepository(dbContext);
                foreach (var slowOperation in slowOperations)
                {
                    slowOperation.Instance = instance;
                    slowOperation.CreatedDate = createdDate;
                    context.Services.LogCounterRuleCache?.CheckAndIncrement(slowOperation.Command);
                    await repository.InsertAsync(slowOperation, token).ConfigureAwait(false);
                }

                scope.Rows(slowOperations.Count);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                scope.Error(ex);
                context.Services.Log.Error(
                    $"InfrastructureHealthMetricsStarter: An error sampling Redis slow operations has been observed as {ex}.");
            }
        }

        private async Task FlushRedisConnectionMultiplexerMetricAsync(DbContext dbContext, string instance,
            DateTime createdDate, CancellationToken token)
        {
            using var scope = SamplerScope.Start("RedisConnectionMultiplexerMetric", instance);
            try
            {
                var multiplexer = context.Services.CacheService?.ConnectionMultiplexer;
                if (multiplexer == null)
                {
                    scope.Skipped();
                    return;
                }

                var metric = redisConnectionMultiplexerMetricSampler.Sample(multiplexer,
                    context.Services.CacheService.ConnectionDiagnostics);
                metric.Instance = instance;
                metric.CreatedDate = createdDate;

                var repository = new RedisConnectionMultiplexerMetricRepository(dbContext);
                await repository.InsertAsync(metric, token).ConfigureAwait(false);
                scope.Rows(1);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                scope.Error(ex);
                context.Services.Log.Error(
                    $"InfrastructureHealthMetricsStarter: An error sampling the Redis connection multiplexer has been observed as {ex}.");
            }
        }

        private async Task FlushRedisSentinelStatusAsync(DbContext dbContext, string instance, DateTime createdDate,
            CancellationToken token)
        {
            using var scope = SamplerScope.Start("RedisSentinelStatus", instance);
            try
            {
                var cacheService = context.Services.CacheService;
                if (cacheService?.SentinelMultiplexer == null)
                {
                    scope.Skipped();
                    return;
                }

                var replicationLagByEndpoint = await redisMetricSampler.SampleReplicationLagAsync()
                    .ConfigureAwait(false);

                var entries = await redisSentinelStatusSampler
                    .SampleAsync(cacheService.SentinelMultiplexer, cacheService.SentinelServiceName,
                        replicationLagByEndpoint)
                    .ConfigureAwait(false);
                if (entries.Count == 0)
                {
                    scope.Skipped();
                    return;
                }

                var repository = new RedisSentinelStatusRepository(dbContext);
                foreach (var entry in entries)
                {
                    entry.OccurredDate = createdDate;
                    entry.Instance = instance;
                    entry.CreatedDate = createdDate;
                    await repository.InsertAsync(entry, token).ConfigureAwait(false);
                }

                scope.Rows(entries.Count);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                scope.Error(ex);
                context.Services.Log.Error(
                    $"InfrastructureHealthMetricsStarter: An error sampling Redis Sentinel status has been observed as {ex}.");
            }
        }

        private async Task FlushRedisSentinelEventsAsync(DbContext dbContext, string instance, DateTime createdDate,
            CancellationToken token)
        {
            using var scope = SamplerScope.Start("RedisSentinelEvent", instance);
            try
            {
                var drained = RedisSentinelEventCapture.DrainAll();
                if (drained.Count == 0)
                {
                    scope.Skipped();
                    return;
                }

                var models = drained.Select(record => new RedisSentinelEvent
                {
                    OccurredDate = record.OccurredDate,
                    Channel = record.Channel,
                    Message = record.Message,
                    CreatedDate = createdDate,
                    Instance = instance
                }).ToList();

                var repository = new RedisSentinelEventRepository(dbContext);
                await repository.BulkCopyAsync(models, token).ConfigureAwait(false);
                scope.Rows(models.Count);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                scope.Error(ex);
                context.Services.Log.Error(
                    $"InfrastructureHealthMetricsStarter: An error flushing captured Redis Sentinel events has been observed as {ex}.");
            }
        }

        private async Task FlushModelInvokeWarningsAsync(DbContext dbContext, string instance, DateTime createdDate,
            CancellationToken token)
        {
            using var scope = SamplerScope.Start("ModelInvokeWarning", instance);
            try
            {
                var drained = ModelInvokeWarningCapture.DrainAll();
                if (drained.Count == 0)
                {
                    scope.Skipped();
                    return;
                }

                var models = drained.Select(record => new ModelInvokeWarning
                {
                    OccurredDate = record.OccurredDate,
                    EntityAnalysisModelGuid = record.EntityAnalysisModelGuid,
                    EntityAnalysisModelName = record.EntityAnalysisModelName,
                    EntityAnalysisModelInstanceEntryGuid = record.EntityAnalysisModelInstanceEntryGuid,
                    Message = record.Message,
                    ElapsedMicroseconds = record.ElapsedMicroseconds,
                    SinceLastEntryMicroseconds = record.SinceLastEntryMicroseconds,
                    ThreadId = record.ThreadId,
                    CreatedDate = createdDate,
                    Instance = instance
                }).ToList();

                var repository = new ModelInvokeWarningRepository(dbContext);
                await repository.BulkCopyAsync(models, token).ConfigureAwait(false);
                scope.Rows(models.Count);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                scope.Error(ex);
                context.Services.Log.Error(
                    $"InfrastructureHealthMetricsStarter: An error flushing captured model invoke warnings has been observed as {ex}.");
            }
        }

        private async Task FlushCaseCreationStagePerformanceCountersAsync(DbContext dbContext, string instance,
            DateTime createdDate, CancellationToken token)
        {
            using var scope = SamplerScope.Start("CaseCreationStagePerformanceCounter", instance);
            try
            {
                var stageNames = CaseCreationStagePerformanceCounters.Instance.Stages.Keys.ToList();
                if (stageNames.Count == 0)
                {
                    scope.Skipped();
                    return;
                }

                var models = new List<CaseCreationStagePerformanceCounter>();
                foreach (var stageName in stageNames)
                {
                    if (!CaseCreationStagePerformanceCounters.Instance.Stages.TryGetValue(stageName,
                            out var accumulator))
                    {
                        continue;
                    }

                    var totalMicroseconds = Interlocked.Exchange(ref accumulator.TotalMicroseconds, 0);
                    var minMicroseconds = Interlocked.Exchange(ref accumulator.MinMicroseconds, long.MaxValue);
                    var maxMicroseconds = Interlocked.Exchange(ref accumulator.MaxMicroseconds, long.MinValue);
                    var invokeCount = Interlocked.Exchange(ref accumulator.InvokeCount, 0);

                    if (invokeCount == 0)
                    {
                        continue;
                    }

                    models.Add(new CaseCreationStagePerformanceCounter
                    {
                        StageId = (int)Enum.Parse<CaseCreationStage>(stageName),
                        TotalMicroseconds = totalMicroseconds,
                        MinMicroseconds = minMicroseconds,
                        MaxMicroseconds = maxMicroseconds,
                        InvokeCount = invokeCount,
                        CreatedDate = createdDate,
                        Instance = instance
                    });
                }

                if (models.Count == 0)
                {
                    scope.Skipped();
                    return;
                }

                var repository = new CaseCreationStagePerformanceCounterRepository(dbContext);
                await repository.BulkCopyAsync(models, token).ConfigureAwait(false);
                scope.Rows(models.Count);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                scope.Error(ex);
                context.Services.Log.Error(
                    $"InfrastructureHealthMetricsStarter: An error flushing case creation stage performance counters has been observed as {ex}.");
            }
        }

        private async Task FlushCaseCreationWarningsAsync(DbContext dbContext, string instance,
            DateTime createdDate, CancellationToken token)
        {
            using var scope = SamplerScope.Start("CaseCreationWarning", instance);
            try
            {
                var drained = CaseCreationWarningCapture.DrainAll();
                if (drained.Count == 0)
                {
                    scope.Skipped();
                    return;
                }

                var models = drained.Select(record => new CaseCreationWarning
                {
                    OccurredDate = record.OccurredDate,
                    TenantRegistryId = record.TenantRegistryId,
                    EntityAnalysisModelInstanceEntryGuid = record.EntityAnalysisModelInstanceEntryGuid,
                    CaseWorkflowGuid = record.CaseWorkflowGuid,
                    CaseKey = record.CaseKey,
                    CaseKeyValue = record.CaseKeyValue,
                    StageId = (int)record.StageId,
                    Destination = record.Destination,
                    DurationMicroseconds = record.DurationMicroseconds,
                    CreatedDate = createdDate,
                    Instance = instance
                }).ToList();

                var repository = new CaseCreationWarningRepository(dbContext);
                await repository.BulkCopyAsync(models, token).ConfigureAwait(false);
                scope.Rows(models.Count);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                scope.Error(ex);
                context.Services.Log.Error(
                    $"InfrastructureHealthMetricsStarter: An error flushing captured case creation warnings has been observed as {ex}.");
            }
        }

        private async Task FlushArchiverWarningsAsync(DbContext dbContext, string instance, DateTime createdDate,
            CancellationToken token)
        {
            using var scope = SamplerScope.Start("ArchiverWarning", instance);
            try
            {
                var drained = ArchiverWarningCapture.DrainAll();
                if (drained.Count == 0)
                {
                    scope.Skipped();
                    return;
                }

                var models = drained.Select(record => new ArchiverWarning
                {
                    OccurredDate = record.OccurredDate,
                    EntityAnalysisModelGuid = record.EntityAnalysisModelGuid,
                    EntityAnalysisModelName = record.EntityAnalysisModelName,
                    EntityAnalysisModelInstanceEntryGuid = record.EntityAnalysisModelInstanceEntryGuid,
                    StageId = (int)record.StageId,
                    DurationMicroseconds = record.DurationMicroseconds,
                    CreatedDate = createdDate,
                    Instance = instance
                }).ToList();

                var repository = new ArchiverWarningRepository(dbContext);
                await repository.BulkCopyAsync(models, token).ConfigureAwait(false);
                scope.Rows(models.Count);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                scope.Error(ex);
                context.Services.Log.Error(
                    $"InfrastructureHealthMetricsStarter: An error flushing captured archiver warnings has been observed as {ex}.");
            }
        }

        private async Task FlushCaptureQueueHealthAsync(DbContext dbContext, string instance, DateTime createdDate,
            CancellationToken token)
        {
            using var scope = SamplerScope.Start("CaptureQueueHealth", instance);

            var modelInvokeWarningDropped = ModelInvokeWarningCapture.TakeDroppedCount();
            var caseCreationWarningDropped = CaseCreationWarningCapture.TakeDroppedCount();
            var archiverWarningDropped = ArchiverWarningCapture.TakeDroppedCount();
            var redisSentinelEventDropped = RedisSentinelEventCapture.TakeDroppedCount();
            var redisConnectionEventDropped = RedisConnectionEventCapture.TakeDroppedCount();
            var openTelemetryMetricDropped = OpenTelemetryMetricCapture.TakeDroppedNewSeriesCount();

            try
            {
                var models = new List<CaptureQueueHealth>
                {
                    new()
                    {
                        QueueId = (int)CaptureQueueType.ModelInvokeWarning,
                        QueueDepth = ModelInvokeWarningCapture.QueueDepth,
                        DroppedCount = modelInvokeWarningDropped,
                        CreatedDate = createdDate,
                        Instance = instance
                    },
                    new()
                    {
                        QueueId = (int)CaptureQueueType.CaseCreationWarning,
                        QueueDepth = CaseCreationWarningCapture.QueueDepth,
                        DroppedCount = caseCreationWarningDropped,
                        CreatedDate = createdDate,
                        Instance = instance
                    },
                    new()
                    {
                        QueueId = (int)CaptureQueueType.ArchiverWarning,
                        QueueDepth = ArchiverWarningCapture.QueueDepth,
                        DroppedCount = archiverWarningDropped,
                        CreatedDate = createdDate,
                        Instance = instance
                    },
                    new()
                    {
                        QueueId = (int)CaptureQueueType.RedisSentinelEvent,
                        QueueDepth = RedisSentinelEventCapture.QueueDepth,
                        DroppedCount = redisSentinelEventDropped,
                        CreatedDate = createdDate,
                        Instance = instance
                    },
                    new()
                    {
                        QueueId = (int)CaptureQueueType.RedisConnectionEvent,
                        QueueDepth = RedisConnectionEventCapture.QueueDepth,
                        DroppedCount = redisConnectionEventDropped,
                        CreatedDate = createdDate,
                        Instance = instance
                    },
                    new()
                    {
                        QueueId = (int)CaptureQueueType.OpenTelemetryMetric,
                        QueueDepth = OpenTelemetryMetricCapture.QueueDepth,
                        DroppedCount = openTelemetryMetricDropped,
                        CreatedDate = createdDate,
                        Instance = instance
                    }
                };

                var repository = new CaptureQueueHealthRepository(dbContext);
                await repository.BulkCopyAsync(models, token).ConfigureAwait(false);
                scope.Rows(models.Count);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                ModelInvokeWarningCapture.AddDroppedCount(modelInvokeWarningDropped);
                CaseCreationWarningCapture.AddDroppedCount(caseCreationWarningDropped);
                ArchiverWarningCapture.AddDroppedCount(archiverWarningDropped);
                RedisSentinelEventCapture.AddDroppedCount(redisSentinelEventDropped);
                RedisConnectionEventCapture.AddDroppedCount(redisConnectionEventDropped);
                OpenTelemetryMetricCapture.AddDroppedNewSeriesCount(openTelemetryMetricDropped);

                scope.Error(ex);
                context.Services.Log.Error(
                    $"InfrastructureHealthMetricsStarter: An error flushing capture queue health has been observed as {ex}.");
            }
        }

        private async Task FlushRedisConnectionEventsAsync(DbContext dbContext, string instance,
            DateTime createdDate, CancellationToken token)
        {
            using var scope = SamplerScope.Start("RedisConnectionEvent", instance);
            try
            {
                var drained = RedisConnectionEventCapture.DrainAll();
                if (drained.Count == 0)
                {
                    scope.Skipped();
                    return;
                }

                var models = drained.Select(record => new RedisConnectionEvent
                {
                    OccurredDate = record.OccurredDate,
                    EventTypeId = (int)record.EventType,
                    EndPoint = record.EndPoint,
                    ConnectionTypeId = (int?)record.ConnectionType,
                    FailureTypeId = (int?)record.FailureType,
                    Origin = record.Origin,
                    Message = record.Message,
                    Exception = record.Exception,
                    CreatedDate = createdDate,
                    Instance = instance,
                    RetryCount = record.RetryCount,
                    BackoffMilliseconds = record.BackoffMilliseconds,
                    TransactionsImpacted = record.TransactionsImpacted
                }).ToList();

                var repository = new RedisConnectionEventRepository(dbContext);
                await repository.BulkCopyAsync(models, token).ConfigureAwait(false);
                scope.Rows(models.Count);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                scope.Error(ex);
                context.Services.Log.Error(
                    $"InfrastructureHealthMetricsStarter: An error flushing captured Redis connection events has been observed as {ex}.");
            }
        }

        private async Task FlushRedisCallCountersAsync(DbContext dbContext, string instance, DateTime createdDate,
            CancellationToken token)
        {
            using var scope = SamplerScope.Start("RedisCallCounter", instance);
            try
            {
                var snapshot = CacheCallCounters.TakeSnapshot();
                if (snapshot.Count == 0)
                {
                    scope.Skipped();
                    return;
                }

                var models = snapshot.Select(entry => new RedisCallCounter
                {
                    Call = entry.Call,
                    Count = entry.Count,
                    TotalMicroseconds = entry.TotalMicroseconds,
                    MinMicroseconds = entry.MinMicroseconds,
                    MaxMicroseconds = entry.MaxMicroseconds,
                    CreatedDate = createdDate,
                    Instance = instance
                }).ToList();

                var repository = new RedisCallCounterRepository(dbContext);
                await repository.BulkCopyAsync(models, token).ConfigureAwait(false);
                scope.Rows(models.Count);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                scope.Error(ex);
                context.Services.Log.Error(
                    $"InfrastructureHealthMetricsStarter: An error flushing Redis call counters has been observed as {ex}.");
            }
        }

        private async Task FlushEtcdMemberStatusAsync(DbContext dbContext, string instance, DateTime createdDate,
            Task<List<string>> etcdEndpointsTask, CancellationToken token)
        {
            using var scope = SamplerScope.Start("EtcdMemberStatus", instance);
            try
            {
#pragma warning disable VSTHRD003 // shared task, started once in SampleAndFlushAsync -- see comment there
                var endpoints = await etcdEndpointsTask.ConfigureAwait(false);
#pragma warning restore VSTHRD003
                if (endpoints.Count == 0)
                {
                    scope.Skipped();
                    return;
                }

                var statusRepository = new EtcdMemberStatusRepository(dbContext);
                var eventRepository = new EtcdClusterEventRepository(dbContext);
                var rows = 0;
                foreach (var endpoint in endpoints)
                {
                    var (status, events) = await etcdMetricSampler.SampleAsync(endpoint, token)
                        .ConfigureAwait(false);
                    if (status != null)
                    {
                        status.OccurredDate = createdDate;
                        status.CreatedDate = createdDate;
                        status.Instance = instance;
                        await statusRepository.InsertAsync(status, token).ConfigureAwait(false);
                        rows++;
                    }

                    foreach (var clusterEvent in events)
                    {
                        clusterEvent.OccurredDate = createdDate;
                        clusterEvent.CreatedDate = createdDate;
                        clusterEvent.Instance = instance;

                        context.Services.LogCounterRuleCache?.CheckAndIncrement(
                            $"{(EtcdClusterEventType)clusterEvent.EventTypeId.GetValueOrDefault()} {clusterEvent.PreviousValue} -> {clusterEvent.NewValue}");

                        await eventRepository.InsertAsync(clusterEvent, token).ConfigureAwait(false);
                        rows++;
                    }
                }

                scope.Rows(rows);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                scope.Error(ex);
                context.Services.Log.Error(
                    $"InfrastructureHealthMetricsStarter: An error sampling etcd member status has been observed as {ex}.");
            }
        }

        private async Task FlushPatroniMemberStatusAsync(DbContext dbContext, string instance, DateTime createdDate,
            Task<List<string>> etcdEndpointsTask, CancellationToken token)
        {
            using var scope = SamplerScope.Start("PatroniMemberStatus", instance);
            try
            {
                var endpoints = await etcdPatroniDiscovery
                    .DiscoverPatroniEndpointsAsync(context.Services.DynamicEnvironment, token)
                    .ConfigureAwait(false);
                if (endpoints.Count == 0)
                {
                    scope.Skipped();
                    return;
                }

                var (statuses, events) = await patroniMetricSampler.SampleAsync(endpoints, token)
                    .ConfigureAwait(false);

                var eventRepository = new PatroniClusterEventRepository(dbContext);
                var rows = 0;

                if (statuses.Count > 0)
                {
                    var statusRepository = new PatroniMemberStatusRepository(dbContext);
                    foreach (var status in statuses)
                    {
                        status.OccurredDate = createdDate;
                        status.CreatedDate = createdDate;
                        status.Instance = instance;
                        await statusRepository.InsertAsync(status, token).ConfigureAwait(false);
                        rows++;
                    }
                }

                foreach (var clusterEvent in events)
                {
                    clusterEvent.OccurredDate = createdDate;
                    clusterEvent.CreatedDate = createdDate;
                    clusterEvent.Instance = instance;

                    context.Services.LogCounterRuleCache?.CheckAndIncrement(
                        $"{(PatroniClusterEventType)clusterEvent.EventTypeId.GetValueOrDefault()} {clusterEvent.Reason} {clusterEvent.PreviousValue} -> {clusterEvent.NewValue}");

                    await eventRepository.InsertAsync(clusterEvent, token).ConfigureAwait(false);
                    rows++;
                }

                var scopeName = statuses.Select(s => s.Scope).FirstOrDefault(s => s != null);
                if (scopeName != null)
                {
#pragma warning disable VSTHRD003 // shared task, started once in SampleAndFlushAsync -- see comment there
                    var etcdEndpoints = await etcdEndpointsTask.ConfigureAwait(false);
#pragma warning restore VSTHRD003
                    var dcsNamespace = context.Services.DynamicEnvironment.AppSettings("PatroniDcsNamespace");

                    var historyEvents = await patroniMetricSampler
                        .SampleHistoryEventsAsync(etcdEndpoints, scopeName, dcsNamespace, token)
                        .ConfigureAwait(false);

                    foreach (var historyEvent in historyEvents)
                    {
                        historyEvent.OccurredDate ??= createdDate;
                        historyEvent.CreatedDate = createdDate;
                        historyEvent.Instance = instance;

                        context.Services.LogCounterRuleCache?.CheckAndIncrement(
                            $"{(PatroniClusterEventType)historyEvent.EventTypeId.GetValueOrDefault()} {historyEvent.Reason} {historyEvent.PreviousValue} -> {historyEvent.NewValue}");

                        await eventRepository.InsertAsync(historyEvent, token).ConfigureAwait(false);
                        rows++;
                    }
                }

                if (rows == 0)
                {
                    scope.Skipped();
                }
                else
                {
                    scope.Rows(rows);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                scope.Error(ex);
                context.Services.Log.Error(
                    $"InfrastructureHealthMetricsStarter: An error sampling Patroni member status has been observed as {ex}.");
            }
        }

        private async Task FlushOpenTelemetryMetricsAsync(DbContext dbContext, string instance, DateTime createdDate,
            CancellationToken token)
        {
            using var scope = SamplerScope.Start("OpenTelemetryMetric", instance);
            try
            {
                scope.Tag("jube.otel_capture.sampled_out_count", OpenTelemetryMetricCapture.SampledOutCount);

                var aggregations = OpenTelemetryMetricCapture.DrainAll();
                if (aggregations.Count == 0)
                {
                    scope.Skipped();
                    return;
                }

                var models = aggregations.Select(aggregation => new OpenTelemetryMetric
                {
                    OccurredDate = createdDate,
                    MetricName = aggregation.InstrumentName,
                    InstrumentType = aggregation.InstrumentType,
                    Tags = aggregation.Tags,
                    Count = aggregation.Count,
                    Sum = aggregation.Sum,
                    Min = aggregation.Min,
                    Max = aggregation.Max,
                    CreatedDate = createdDate,
                    Instance = instance
                }).ToList();

                var repository = new OpenTelemetryMetricRepository(dbContext);
                await repository.BulkCopyAsync(models, token).ConfigureAwait(false);
                scope.Rows(models.Count);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                scope.Error(ex);
                context.Services.Log.Error(
                    $"InfrastructureHealthMetricsStarter: An error flushing captured OpenTelemetry metrics has been observed as {ex}.");
            }
        }
    }
}