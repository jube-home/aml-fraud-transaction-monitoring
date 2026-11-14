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
    using Retention;

    public class RetentionPurgeStarter(Context context)
    {
        public static bool IsEnabled(DynamicEnvironment.DynamicEnvironment environment)
        {
            return environment.AppSettings("EnableRetentionPurge")
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

                        var dryRun = !environment.AppSettings("RetentionPurgeDryRun")
                            .Equals("False", StringComparison.OrdinalIgnoreCase);

                        var result = await RetentionPurge.RunAsync(dbContext,
                            int.Parse(environment.AppSettings("RetentionArchiveDays")), dryRun,
                            int.Parse(environment.AppSettings("RetentionPurgeDeleteLimit")), DateTime.UtcNow, token)
                            .ConfigureAwait(false);

                        log.Info(
                            $"Retention Purge: {(result.DryRun ? "dry run would remove" : "removed")} {result.Archive} archived transactions and {result.AskJooby} expired AskJooby rows and {result.Queue} inbound queue rows left behind by deleted models.");
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        log.Error($"Retention Purge: has created an error inside the loop {ex}");
                    }

                    await Task.Delay(int.Parse(environment.AppSettings("WaitRetentionPurge")), token)
                        .ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException ex)
            {
                log.Info($"Graceful Cancellation RetentionPurgeStarter: {ex.Message}");
            }
        }
    }
}
