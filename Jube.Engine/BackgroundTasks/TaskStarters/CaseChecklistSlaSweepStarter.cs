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
    using CaseEscalation;
    using Context;
    using Data.Context;

    public class CaseChecklistSlaSweepStarter(Context context)
    {
        public static bool IsEnabled(DynamicEnvironment.DynamicEnvironment environment)
        {
            return environment.AppSettings("EnableCaseChecklistSlaSweep")
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
                        await using var dbContext = DataConnectionDbContext.GetResilientDbContextDataConnection(
                            environment.AppSettings("ConnectionString"), log);

                        var recorded = await CaseChecklistSlaSweepSignal.RunAsync(dbContext,
                            int.Parse(environment.AppSettings("CaseChecklistSlaSweepBatchSize")), token)
                            .ConfigureAwait(false);

                        if (recorded > 0 && log.IsInfoEnabled)
                        {
                            log.Info($"Case Checklist SLA Sweep: recorded {recorded} breached items.");
                        }
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        log.Error($"Case Checklist SLA Sweep: has created an error inside the loop {ex}");
                    }

                    await Task.Delay(
                        TimeSpan.FromSeconds(int.Parse(environment.AppSettings("CaseChecklistSlaSweepSeconds"))),
                        token).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException ex)
            {
                log.Info($"Graceful Cancellation CaseChecklistSlaSweepAsync: {ex.Message}");
            }
        }
    }
}
