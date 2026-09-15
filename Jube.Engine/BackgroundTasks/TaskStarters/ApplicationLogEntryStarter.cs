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
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Jube.Data.Context;
using Jube.Data.Poco;
using Jube.Data.Repository;
using Jube.DynamicEnvironment.Logging;

namespace Jube.Engine.BackgroundTasks.TaskStarters
{
    public class ApplicationLogEntryStarter(Context.Context context)
    {
        private const int FlushIntervalMilliseconds = 60000;

        public async Task StartAsync()
        {
            try
            {
                while (!context.Services.TaskCoordinator.CancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        await FlushAsync(context.Services.TaskCoordinator.CancellationToken).ConfigureAwait(false);

                        await Task.Delay(FlushIntervalMilliseconds, context.Services.TaskCoordinator.CancellationToken)
                            .ConfigureAwait(false);
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        context.Services.Log.Error(
                            $"ApplicationLogEntryStarter: An error in application log entry flushing has been observed as {ex}.");
                    }
                }
            }
            catch (OperationCanceledException ex)
            {
                context.Services.Log.Info(
                    $"Graceful Cancellation ApplicationLogEntryStarter: has produced an error {ex}");
            }
            catch (Exception ex)
            {
                context.Services.Log.Error($"ApplicationLogEntryStarter: has produced an error {ex}");
            }
        }

        private async Task FlushAsync(CancellationToken token)
        {
            var drained = ApplicationLogCapture.DrainAll();

            if (drained.Count == 0)
            {
                if (context.Services.Log.IsDebugEnabled)
                {
                    context.Services.Log.Debug("ApplicationLogEntryStarter: No captured log entries to flush.");
                }

                return;
            }

            var instance = Dns.GetHostName();
            var createdDate = DateTime.UtcNow;

            var models = drained.Select(record => new ApplicationLogEntry
            {
                OccurredDate = record.OccurredDate,
                Level = record.Level,
                LoggerName = record.LoggerName,
                ThreadContext = record.ThreadContext,
                Message = record.Message,
                Exception = record.Exception,
                CreatedDate = createdDate,
                Instance = instance
            }).ToList();

            foreach (var model in models)
            {
                context.Services.LogCounterRuleCache?.CheckAndIncrement(model.Message);
            }

            var dbContext = DataConnectionDbContext.GetResilientDbContextDataConnection(
                context.Services.DynamicEnvironment.AppSettings("ConnectionString"), context.Services.Log);
            try
            {
                var repository = new ApplicationLogEntryRepository(dbContext);
                await repository.BulkCopyAsync(models, token).ConfigureAwait(false);

                if (context.Services.Log.IsDebugEnabled)
                {
                    context.Services.Log.Debug(
                        $"ApplicationLogEntryStarter: Bulk inserted {models.Count} captured log entries.");
                }
            }
            finally
            {
                await dbContext.CloseAsync(token).ConfigureAwait(false);
                await dbContext.DisposeAsync(token).ConfigureAwait(false);
            }
        }
    }
}