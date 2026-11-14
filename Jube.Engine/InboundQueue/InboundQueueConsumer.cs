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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jube.Data.Context;
using Jube.Data.Repository;
using log4net;

namespace Jube.Engine.InboundQueue
{
    public sealed class InboundQueueConsumer(
        string connectionString,
        ILog log,
        InboundQueueOptions options,
        string node,
        Func<bool> modelsLoaded,
        Func<Guid, InboundModelState> modelState,
        Func<Guid, byte[], CancellationToken, Task<bool>> invoke)
    {
        public const int MaximumBackoffSeconds = 300;

        public static int BackoffSeconds(int attempts)
        {
            return (int)Math.Min(MaximumBackoffSeconds, 5d * Math.Pow(2, Math.Max(0, attempts - 1)));
        }

        public async Task RunAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                var processed = 0;

                try
                {
                    processed = await RunOnceAsync(token).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    log.Error("InboundQueue: the cycle failed; will try again.", ex);
                }

                if (processed > 0)
                {
                    continue;
                }

                try
                {
                    await Task.Delay(options.PollMilliseconds, token).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }

        public async Task<int> RunOnceAsync(CancellationToken token)
        {
            if (!modelsLoaded())
            {
                return 0;
            }

            await using var dbContext = DataConnectionDbContext.GetResilientDbContextDataConnection(connectionString,
                log);

            var repository = new TransactionInboundQueueRepository(dbContext);

            var rows = await repository.ClaimAsync(node, Math.Max(1, options.BatchSize),
                Math.Max(10, options.VisibilitySeconds), options.MaximumAttempts, token).ConfigureAwait(false);

            if (rows.Count == 0)
            {
                return 0;
            }

            using var gate = new SemaphoreSlim(Math.Max(1, options.Concurrency));

            await Task.WhenAll(rows.Select(row => ProcessAsync(row, gate, token))).ConfigureAwait(false);

            return rows.Count;
        }

        private async Task ProcessAsync(ClaimedInboundRow row, SemaphoreSlim gate, CancellationToken token)
        {
            await using var dbContext = DataConnectionDbContext.GetResilientDbContextDataConnection(connectionString,
                log);

            var repository = new TransactionInboundQueueRepository(dbContext);

            try
            {
                await gate.WaitAsync(token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                await repository.RetryAsync(row.Id, 0, "Cancelled", CancellationToken.None).ConfigureAwait(false);

                return;
            }

            try
            {
                var state = modelState(row.EntityAnalysisModelGuid);

                if (state != InboundModelState.Ready)
                {
                    await FailAsync(repository, row, state == InboundModelState.Unknown
                        ? "ModelNotFound"
                        : "ModelNotStarted").ConfigureAwait(false);

                    return;
                }

                bool ok;

                try
                {
                    ok = await invoke(row.EntityAnalysisModelGuid, row.Payload, token).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    await repository.RetryAsync(row.Id, 0, "Cancelled", CancellationToken.None).ConfigureAwait(false);

                    return;
                }
                catch (Exception ex)
                {
                    log.Error($"InboundQueue: processing row {row.Id} failed.", ex);
                    await FailAsync(repository, row, "Exception " + ex.GetType().Name).ConfigureAwait(false);

                    return;
                }

                if (!ok)
                {
                    await FailAsync(repository, row, "NoResponse").ConfigureAwait(false);

                    return;
                }

                await repository.CompleteAsync(row.Id, CancellationToken.None).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                log.Error($"InboundQueue: settling row {row.Id} failed; it will be taken again when its claim expires.",
                    ex);
            }
            finally
            {
                gate.Release();
            }
        }

        private async Task FailAsync(TransactionInboundQueueRepository repository, ClaimedInboundRow row, string error)
        {
            if (row.Attempts >= options.MaximumAttempts)
            {
                log.Warn($"InboundQueue: row {row.Id} is dead after {row.Attempts} attempts: {error}.");
                await repository.KillAsync(row.Id, error, CancellationToken.None).ConfigureAwait(false);

                return;
            }

            await repository.RetryAsync(row.Id, BackoffSeconds(row.Attempts), error, CancellationToken.None)
                .ConfigureAwait(false);
        }
    }
}
