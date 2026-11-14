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

namespace Jube.Engine.BackgroundTasks.TaskStarters
{
    using System;
    using System.Threading.Tasks;
    using Context;
    using Data.Context;
    using Erasure;

    public class ErasureExecutionStarter(Context context)
    {
        public static bool IsEnabled(DynamicEnvironment.DynamicEnvironment environment)
        {
            return environment.AppSettings("EnableErasureExecution")
                .Equals("True", StringComparison.OrdinalIgnoreCase);
        }

        public async Task StartAsync()
        {
            var token = context.Services.TaskCoordinator.CancellationToken;
            var environment = context.Services.DynamicEnvironment;
            var log = context.Services.Log;

            try
            {
                while (!token.IsCancellationRequested)
                {
                    try
                    {
                        var purgeConnection = environment.AppSettings("PurgeConnectionString");

                        await using var dbContext = DataConnectionDbContext.GetResilientDbContextDataConnection(
                            string.IsNullOrWhiteSpace(purgeConnection)
                                ? environment.AppSettings("ConnectionString")
                                : purgeConnection, log);

                        var result = await ErasureExecutor.RunAsync(dbContext,
                            int.Parse(environment.AppSettings("ErasureRetentionMinimumDays")), DateTime.UtcNow, token)
                            .ConfigureAwait(false);

                        if (result.Executed + result.Refused > 0)
                        {
                            log.Info(
                                $"Erasure: executed {result.Executed} and refused {result.Refused} approved requests.");
                        }
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        log.Error($"Erasure: has created an error inside the loop {ex.GetType().Name}");
                    }

                    await Task.Delay(int.Parse(environment.AppSettings("WaitErasureExecution")), token)
                        .ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException ex)
            {
                log.Info($"Graceful Cancellation ErasureExecutionStarter: {ex.Message}");
            }
        }
    }
}
