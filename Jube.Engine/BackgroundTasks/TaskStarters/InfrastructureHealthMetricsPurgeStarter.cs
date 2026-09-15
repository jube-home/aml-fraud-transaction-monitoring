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
using System.Linq.Expressions;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Jube.Data.Context;
using Jube.Data.Helpers;
using Jube.Data.Poco;
using Jube.Engine.Observability;

namespace Jube.Engine.BackgroundTasks.TaskStarters
{
    public class InfrastructureHealthMetricsPurgeStarter(Context.Context context)
    {
        public async Task StartAsync()
        {
            try
            {
                while (!context.Services.TaskCoordinator.CancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        await PurgeAllAsync(context.Services.TaskCoordinator.CancellationToken).ConfigureAwait(false);

                        var wait = int.Parse(context.Services.DynamicEnvironment
                            .AppSettings("WaitInfrastructureHealthMetricsPurge"));
                        await Task.Delay(wait, context.Services.TaskCoordinator.CancellationToken)
                            .ConfigureAwait(false);
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        context.Services.Log.Error(
                            $"InfrastructureHealthMetricsPurgeStarter: An error in the purge cycle has been observed as {ex}.");
                    }
                }
            }
            catch (OperationCanceledException ex)
            {
                context.Services.Log.Info(
                    $"Graceful Cancellation InfrastructureHealthMetricsPurgeStarter: has produced an error {ex}");
            }
            catch (Exception ex)
            {
                context.Services.Log.Error($"InfrastructureHealthMetricsPurgeStarter: has produced an error {ex}");
            }
        }

        private Task PurgeAllAsync(CancellationToken token)
        {
            var cutoffUtc = ComputeCutoffUtc();
            var chunkSize = int.Parse(context.Services.DynamicEnvironment
                .AppSettings("InfrastructureHealthMetricsPurgeDeleteLimit"));

            Task[] purges =
            [
                PurgeTableAsync<ApplicationLogEntry>("ApplicationLogEntry",
                    w => w.OccurredDate < cutoffUtc, w => w.Id, ids => w => ids.Contains(w.Id), chunkSize, token),
                PurgeTableAsync<ContainerLogEntry>("ContainerLogEntry",
                    w => w.OccurredDate < cutoffUtc, w => w.Id, ids => w => ids.Contains(w.Id), chunkSize, token),
                PurgeTableAsync<DockerContainerMetric>("DockerContainerMetric",
                    w => w.OccurredDate < cutoffUtc, w => w.Id, ids => w => ids.Contains(w.Id), chunkSize, token),
                PurgeTableAsync<DockerEvent>("DockerEvent",
                    w => w.OccurredDate < cutoffUtc, w => w.Id, ids => w => ids.Contains(w.Id), chunkSize, token),
                PurgeTableAsync<DockerHostMetric>("DockerHostMetric",
                    w => w.CreatedDate < cutoffUtc, w => w.Id, ids => w => ids.Contains(w.Id), chunkSize, token),
                PurgeTableAsync<DotNetRuntimeMetric>("DotNetRuntimeMetric",
                    w => w.CreatedDate < cutoffUtc, w => w.Id, ids => w => ids.Contains(w.Id), chunkSize, token),
                PurgeTableAsync<EntityAnalysisModelResponseTimePipelineCounter>(
                    "EntityAnalysisModelResponseTimePipelineCounter",
                    w => w.CreatedDate < cutoffUtc, w => w.Id, ids => w => ids.Contains(w.Id), chunkSize, token),
                PurgeTableAsync<EntityAnalysisModelStagePerformanceCounter>(
                    "EntityAnalysisModelStagePerformanceCounter",
                    w => w.CreatedDate < cutoffUtc, w => w.Id, ids => w => ids.Contains(w.Id), chunkSize, token),
                PurgeTableAsync<EntityAnalysisModelTaskPerformanceCounter>(
                    "EntityAnalysisModelTaskPerformanceCounter",
                    w => w.CreatedDate < cutoffUtc, w => w.Id, ids => w => ids.Contains(w.Id), chunkSize, token),
                PurgeTableAsync<EtcdClusterEvent>("EtcdClusterEvent",
                    w => w.OccurredDate < cutoffUtc, w => w.Id, ids => w => ids.Contains(w.Id), chunkSize, token),
                PurgeTableAsync<EtcdMemberStatus>("EtcdMemberStatus",
                    w => w.OccurredDate < cutoffUtc, w => w.Id, ids => w => ids.Contains(w.Id), chunkSize, token),
                PurgeTableAsync<OpenTelemetryMetric>("OpenTelemetryMetric",
                    w => w.OccurredDate < cutoffUtc, w => w.Id, ids => w => ids.Contains(w.Id), chunkSize, token),
                PurgeTableAsync<OtlpDispatchCounter>("OtlpDispatchCounter",
                    w => w.CreatedDate < cutoffUtc, w => w.Id, ids => w => ids.Contains(w.Id), chunkSize, token),
                PurgeTableAsync<PatroniClusterEvent>("PatroniClusterEvent",
                    w => w.OccurredDate < cutoffUtc, w => w.Id, ids => w => ids.Contains(w.Id), chunkSize, token),
                PurgeTableAsync<PatroniMemberStatus>("PatroniMemberStatus",
                    w => w.OccurredDate < cutoffUtc, w => w.Id, ids => w => ids.Contains(w.Id), chunkSize, token),
                PurgeTableAsync<PostgresMetric>("PostgresMetric",
                    w => w.CreatedDate < cutoffUtc, w => w.Id, ids => w => ids.Contains(w.Id), chunkSize, token),
                PurgeTableAsync<PostgresReplicationStatus>("PostgresReplicationStatus",
                    w => w.OccurredDate < cutoffUtc, w => w.Id, ids => w => ids.Contains(w.Id), chunkSize, token),
                PurgeTableAsync<PostgresLogEntry>("PostgresLogEntry",
                    w => w.OccurredDate < cutoffUtc, w => w.Id, ids => w => ids.Contains(w.Id), chunkSize, token),
                PurgeTableAsync<RedisConnectionEvent>("RedisConnectionEvent",
                    w => w.OccurredDate < cutoffUtc, w => w.Id, ids => w => ids.Contains(w.Id), chunkSize, token),
                PurgeTableAsync<RedisConnectionMultiplexerMetric>("RedisConnectionMultiplexerMetric",
                    w => w.CreatedDate < cutoffUtc, w => w.Id, ids => w => ids.Contains(w.Id), chunkSize, token),
                PurgeTableAsync<RedisCallCounter>("RedisCallCounter",
                    w => w.CreatedDate < cutoffUtc, w => w.Id, ids => w => ids.Contains(w.Id), chunkSize, token),
                PurgeTableAsync<RedisMetric>("RedisMetric",
                    w => w.CreatedDate < cutoffUtc, w => w.Id, ids => w => ids.Contains(w.Id), chunkSize, token),
                PurgeTableAsync<RedisSentinelEvent>("RedisSentinelEvent",
                    w => w.OccurredDate < cutoffUtc, w => w.Id, ids => w => ids.Contains(w.Id), chunkSize, token),
                PurgeTableAsync<ModelInvokeWarning>("ModelInvokeWarning",
                    w => w.OccurredDate < cutoffUtc, w => w.Id, ids => w => ids.Contains(w.Id), chunkSize, token),
                PurgeTableAsync<ArchiverStagePerformanceCounter>("ArchiverStagePerformanceCounter",
                    w => w.CreatedDate < cutoffUtc, w => w.Id, ids => w => ids.Contains(w.Id), chunkSize, token),
                PurgeTableAsync<CaseCreationStagePerformanceCounter>("CaseCreationStagePerformanceCounter",
                    w => w.CreatedDate < cutoffUtc, w => w.Id, ids => w => ids.Contains(w.Id), chunkSize, token),
                PurgeTableAsync<CaseCreationWarning>("CaseCreationWarning",
                    w => w.OccurredDate < cutoffUtc, w => w.Id, ids => w => ids.Contains(w.Id), chunkSize, token),
                PurgeTableAsync<ArchiverWarning>("ArchiverWarning",
                    w => w.OccurredDate < cutoffUtc, w => w.Id, ids => w => ids.Contains(w.Id), chunkSize, token),
                PurgeTableAsync<CaptureQueueHealth>("CaptureQueueHealth",
                    w => w.CreatedDate < cutoffUtc, w => w.Id, ids => w => ids.Contains(w.Id), chunkSize, token),
                PurgeTableAsync<RedisSentinelStatus>("RedisSentinelStatus",
                    w => w.OccurredDate < cutoffUtc, w => w.Id, ids => w => ids.Contains(w.Id), chunkSize, token),
                PurgeTableAsync<RedisSlowOperation>("RedisSlowOperation",
                    w => w.OccurredDate < cutoffUtc, w => w.Id, ids => w => ids.Contains(w.Id), chunkSize, token),
                PurgeTableAsync<HttpProcessingCounter>("HttpProcessingCounter",
                    w => w.CreatedDate < cutoffUtc, w => w.Id, ids => w => ids.Contains(w.Id), chunkSize, token),
                PurgeTableAsync<EntityAnalysisAsynchronousQueueBalance>("EntityAnalysisAsynchronousQueueBalance",
                    w => w.CreatedDate < cutoffUtc, w => w.Id, ids => w => ids.Contains(w.Id), chunkSize, token),
                PurgeTableAsync<EntityAnalysisModelAsynchronousQueueBalance>(
                    "EntityAnalysisModelAsynchronousQueueBalance",
                    w => w.CreatedDate < cutoffUtc, w => w.Id, ids => w => ids.Contains(w.Id), chunkSize, token),
                PurgeTableAsync<EntityAnalysisModelProcessingCounter>("EntityAnalysisModelProcessingCounter",
                    w => w.CreatedDate < cutoffUtc, w => w.Id, ids => w => ids.Contains(w.Id), chunkSize, token),
                PurgeTableAsync<HAProxyServerStatus>("HAProxyServerStatus",
                    w => w.CreatedDate < cutoffUtc, w => w.Id, ids => w => ids.Contains(w.Id), chunkSize, token),
                PurgeTableAsync<HAProxyReachabilityProbe>("HAProxyReachabilityProbe",
                    w => w.CreatedDate < cutoffUtc, w => w.Id, ids => w => ids.Contains(w.Id), chunkSize, token),
                PurgeTableAsync<OverlayNetworkTaskDrift>("OverlayNetworkTaskDrift",
                    w => w.CreatedDate < cutoffUtc, w => w.Id, ids => w => ids.Contains(w.Id), chunkSize, token)
            ];

            return Task.WhenAll(purges);
        }

        private DateTime ComputeCutoffUtc()
        {
            var intervalType = context.Services.DynamicEnvironment
                .AppSettings("InfrastructureHealthMetricsPurgeIntervalType");

            var intervalValue = int.Parse(context.Services.DynamicEnvironment
                .AppSettings("InfrastructureHealthMetricsPurgeIntervalValue"));

            var now = DateTime.UtcNow;

            return intervalType switch
            {
                "d" => now.AddDays(intervalValue * -1),
                "h" => now.AddHours(intervalValue * -1),
                "n" => now.AddMinutes(intervalValue * -1),
                "s" => now.AddSeconds(intervalValue * -1),
                "m" => now.AddMonths(intervalValue * -1),
                "y" => now.AddYears(intervalValue * -1),
                _ => now.AddDays(intervalValue * -1)
            };
        }

        private async Task PurgeTableAsync<T>(string tableName, Expression<Func<T, bool>> olderThanPredicate,
            Expression<Func<T, int>> idSelector, Func<List<int>, Expression<Func<T, bool>>> byIdsPredicate,
            int chunkSize, CancellationToken token) where T : class
        {
            using var scope = SamplerScope.Start($"{tableName}Purge", Dns.GetHostName());
            var dbContext = DataConnectionDbContext.GetResilientDbContextDataConnection(
                context.Services.DynamicEnvironment.AppSettings("ConnectionString"), context.Services.Log);
            try
            {
                var deleted = await ChunkedPurge.DeleteOlderThanAsync(dbContext, olderThanPredicate, idSelector,
                    byIdsPredicate, chunkSize, token).ConfigureAwait(false);

                if (deleted == 0)
                {
                    scope.Skipped();
                }
                else
                {
                    scope.Rows((int)Math.Min(deleted, int.MaxValue));
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                scope.Error(ex);
                context.Services.Log.Error(
                    $"InfrastructureHealthMetricsPurgeStarter: An error purging {tableName} has been observed as {ex}.");
            }
            finally
            {
                await dbContext.CloseAsync(token).ConfigureAwait(false);
                await dbContext.DisposeAsync(token).ConfigureAwait(false);
            }
        }
    }
}