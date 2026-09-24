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

namespace Jube.Engine.EntityAnalysisModelManager.BackgroundTasks.TaskStarters
{
    using System;
    using System.Linq;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;
    using Context;
    using Data.Context;
    using Data.Query;
    using Data.Repository;
    using EntityAnalysisModelInvoke.Simulation;
    using Newtonsoft.Json;

    public class BacktestTaskStarter(Context context, int thread)
    {
        private const string EngineShutdownError = "The engine was shutting down and the backtest was cancelled.";
        private const string TimeoutError = "The backtest exceeded the maximum run duration and was stopped.";
        private const string UnexpectedError = "The backtest failed unexpectedly.";

        private static readonly TimeSpan pollDelay = TimeSpan.FromSeconds(2);
        private static readonly TimeSpan recoveryInterval = TimeSpan.FromSeconds(30);

        private readonly string claimedBy = $"{Dns.GetHostName()}:{thread}";

        public async Task StartAsync()
        {
            var token = context.Services.TaskCoordinator.CancellationToken;
            var lastRecovery = DateTime.MinValue;

            while (!token.IsCancellationRequested)
            {
                try
                {
                    if (thread == 1 && DateTime.UtcNow - lastRecovery > recoveryInterval)
                    {
                        await RecoverStaleAsync(token).ConfigureAwait(false);
                        lastRecovery = DateTime.UtcNow;
                    }

                    if (!await RunNextAsync(token).ConfigureAwait(false))
                    {
                        await Task.Delay(pollDelay, token).ConfigureAwait(false);
                    }
                }
                catch (OperationCanceledException) when (token.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    context.Services.Log.Error($"Entity Backtest: thread {thread} has produced an error {ex}");
                    await Task.Delay(pollDelay, token).ConfigureAwait(false);
                }
            }
        }

        public async Task<bool> RunNextAsync(CancellationToken token)
        {
            await using var dbContext = NewDbContext();
            var instanceId = await new GetNextEntityAnalysisModelBacktestInstanceQuery(dbContext, claimedBy,
                    Setting("BacktestMaxConcurrentRunsPerTenant", 1))
                .ExecuteAsync(token).ConfigureAwait(false);
            if (instanceId == null)
            {
                return false;
            }

            if (context.Services.Log.IsInfoEnabled)
            {
                context.Services.Log.Info(
                    $"Entity Backtest: thread {thread} has claimed backtest instance {instanceId}.");
            }

            await RunAsync(dbContext, instanceId.Value, token).ConfigureAwait(false);
            return true;
        }

        private async Task RunAsync(DbContext dbContext, int instanceId, CancellationToken shutdown)
        {
            var instance = await EntityAnalysisModelBacktestInstanceRepository
                .GetClaimedAsync(dbContext, instanceId, shutdown)
                .ConfigureAwait(false);
            var specification = JsonConvert.DeserializeObject<BacktestSpecification>(instance.Request);
            specification = specification with
            {
                Limit = Math.Min(specification.Limit, Setting("BacktestMaxRows", 1_000_000))
            };

            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(Setting("BacktestMaxRunSeconds",
                3600)));
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(shutdown, timeout.Token);
            var runToken = linked.Token;

            await using var archive = NewReportDbContext();
            await using var heartbeat = new BacktestHeartbeat(NewDbContext, instanceId,
                BacktestHeartbeat.IntervalFor(TimeSpan.FromSeconds(Setting("BacktestStaleSeconds", 120))),
                context.Services.Log);
            try
            {
                var execution = await BacktestExecutor.ExecuteAsync(dbContext, instance.TenantRegistryId, specification,
                    Setting("BacktestPageSize", 5000), heartbeat.OnPageAsync, runToken, archive).ConfigureAwait(false);

                if (!execution.Valid)
                {
                    await CompleteAsync(instanceId, BacktestInstanceStatus.Failed, heartbeat.Scanned,
                            heartbeat.Evaluated, null,
                            string.Join(" ", execution.Errors.Select(e => $"{e.Property}: {e.Message}")))
                        .ConfigureAwait(false);
                    return;
                }

                var result = JsonConvert.SerializeObject(execution.Result);
                var status = heartbeat.Stopped ? BacktestInstanceStatus.Cancelled
                    : execution.Result.Aborted ? BacktestInstanceStatus.Failed
                    : BacktestInstanceStatus.Succeeded;
                await CompleteAsync(instanceId, status, execution.Result.Scanned, execution.Result.Evaluated, result,
                        status == BacktestInstanceStatus.Failed ? execution.Result.AbortReason : null)
                    .ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (timeout.IsCancellationRequested &&
                                                     !shutdown.IsCancellationRequested)
            {
                await CompleteAsync(instanceId, BacktestInstanceStatus.Failed, heartbeat.Scanned, heartbeat.Evaluated,
                        null, TimeoutError)
                    .ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (shutdown.IsCancellationRequested)
            {
                await CompleteAsync(instanceId, BacktestInstanceStatus.Cancelled, heartbeat.Scanned,
                    heartbeat.Evaluated, null,
                    EngineShutdownError).ConfigureAwait(false);
                throw;
            }
            catch (Exception ex)
            {
                context.Services.Log.Error(
                    $"Entity Backtest: backtest instance {instanceId} has produced an error {ex}");
                await CompleteAsync(instanceId, BacktestInstanceStatus.Failed, heartbeat.Scanned, heartbeat.Evaluated,
                        null, UnexpectedError)
                    .ConfigureAwait(false);
            }
        }

        private async Task CompleteAsync(int instanceId, BacktestInstanceStatus status, long scanned, long evaluated,
            string result, string error)
        {
            await using var dbContext = NewDbContext();
            var recorded = await EntityAnalysisModelBacktestInstanceRepository.CompleteAsync(dbContext, instanceId,
                status,
                scanned, evaluated, result, error, CancellationToken.None).ConfigureAwait(false);

            if (!recorded)
            {
                if (context.Services.Log.IsWarnEnabled)
                {
                    context.Services.Log.Warn(
                        $"Entity Backtest: backtest instance {instanceId} was no longer running, " +
                        $"so its {status} outcome was not recorded.");
                }

                return;
            }

            if (context.Services.Log.IsInfoEnabled)
            {
                context.Services.Log.Info($"Entity Backtest: backtest instance {instanceId} has finished as {status}.");
            }
        }

        private async Task RecoverStaleAsync(CancellationToken token)
        {
            await using var dbContext = NewDbContext();
            var recovered = await EntityAnalysisModelBacktestInstanceRepository.RecoverStaleAsync(dbContext,
                TimeSpan.FromSeconds(Setting("BacktestStaleSeconds", 120)), token).ConfigureAwait(false);
            if (recovered > 0 && context.Services.Log.IsWarnEnabled)
            {
                context.Services.Log.Warn($"Entity Backtest: {recovered} stale backtest instances were closed.");
            }
        }

        private DbContext NewDbContext()
        {
            return DataConnectionDbContext.GetResilientDbContextDataConnection(
                context.Services.DynamicEnvironment.AppSettings("ConnectionString"), context.Services.Log);
        }

        private DbContext NewReportDbContext()
        {
            var report = context.Services.ReportConnectionString;
            return string.IsNullOrWhiteSpace(report)
                ? null
                : DataConnectionDbContext.GetResilientDbContextDataConnection(report, context.Services.Log);
        }

        private int Setting(string key, int fallback)
        {
            return int.TryParse(context.Services.DynamicEnvironment.AppSettings(key), out var value) && value > 0
                ? value
                : fallback;
        }
    }
}