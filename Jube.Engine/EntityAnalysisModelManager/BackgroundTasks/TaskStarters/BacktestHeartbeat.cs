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
    using System.Threading;
    using System.Threading.Tasks;
    using Data.Context;
    using Data.Repository;
    using EntityAnalysisModelInvoke.Simulation;
    using log4net;

    public sealed class BacktestHeartbeat : IAsyncDisposable
    {
        private static readonly TimeSpan minimumInterval = TimeSpan.FromSeconds(1);
        private static readonly TimeSpan maximumInterval = TimeSpan.FromSeconds(10);

        private readonly int instanceId;
        private readonly TimeSpan interval;
        private readonly SemaphoreSlim exited = new(0, 1);
        private readonly ILog log;
        private readonly Func<DbContext> newDbContext;
        private readonly CancellationTokenSource stop = new();
        private long evaluated;
        private long limit;
        private long scanned;
        private volatile bool stopped;
        private volatile bool stopRequested;

        public BacktestHeartbeat(Func<DbContext> newDbContext, int instanceId, TimeSpan interval, ILog log)
        {
            this.newDbContext = newDbContext;
            this.instanceId = instanceId;
            this.interval = interval;
            this.log = log;
            _ = Task.Run(RunAsync);
        }

        public bool StopRequested => stopRequested;

        public bool Stopped => stopped;

        public long Scanned => Interlocked.Read(ref scanned);

        public long Evaluated => Interlocked.Read(ref evaluated);

        public static TimeSpan IntervalFor(TimeSpan stale)
        {
            var quarter = TimeSpan.FromTicks(stale.Ticks / 4);
            return quarter < minimumInterval ? minimumInterval : quarter > maximumInterval ? maximumInterval : quarter;
        }

        public void Report(long scannedSoFar, long evaluatedSoFar, long limitOfScan)
        {
            Interlocked.Exchange(ref scanned, scannedSoFar);
            Interlocked.Exchange(ref evaluated, evaluatedSoFar);
            Interlocked.Exchange(ref limit, limitOfScan);
        }

        public Task<bool> OnPageAsync(BacktestProgress progress)
        {
            Report(progress.Scanned, progress.Evaluated, progress.Limit);
            if (stopRequested)
            {
                stopped = true;
            }

            return Task.FromResult(!stopped);
        }

        public async ValueTask DisposeAsync()
        {
            await stop.CancelAsync().ConfigureAwait(false);
            await exited.WaitAsync().ConfigureAwait(false);
            stop.Dispose();
            exited.Dispose();
        }

        private async Task RunAsync()
        {
            try
            {
                using var timer = new PeriodicTimer(interval);
                do
                {
                    await BeatAsync().ConfigureAwait(false);
                } while (await timer.WaitForNextTickAsync(stop.Token).ConfigureAwait(false));
            }
            catch (OperationCanceledException) when (stop.IsCancellationRequested)
            {
            }
            finally
            {
                exited.Release();
            }
        }

        private async Task BeatAsync()
        {
            try
            {
                var scannedNow = Interlocked.Read(ref scanned);
                var limitNow = Interlocked.Read(ref limit);
                await using var dbContext = newDbContext();
                var status = await EntityAnalysisModelBacktestInstanceRepository.HeartbeatAsync(dbContext, instanceId,
                        scannedNow, Interlocked.Read(ref evaluated),
                        limitNow == 0 ? 0 : (double)scannedNow / limitNow, stop.Token)
                    .ConfigureAwait(false);
                if (status != BacktestInstanceStatus.Running)
                {
                    stopRequested = true;
                }
            }
            catch (OperationCanceledException) when (stop.IsCancellationRequested)
            {
            }
            catch (Exception ex)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn(
                        $"Entity Backtest: heartbeat for backtest instance {instanceId} failed and will be retried: {ex.Message}");
                }
            }
        }
    }
}