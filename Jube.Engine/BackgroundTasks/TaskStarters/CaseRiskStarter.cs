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
    using CaseRisk;
    using Context;
    using Data.Context;

    public class CaseRiskStarter(Context context)
    {
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
                        var settings = CaseRiskSettingsReader.Read(environment);

                        await using var dbContext = DataConnectionDbContext.GetResilientDbContextDataConnection(
                            environment.AppSettings("ConnectionString"), log);

                        var result = await CaseRiskSweep.RunAsync(dbContext, settings,
                            TimeSpan.FromMinutes(int.Parse(environment.AppSettings("CaseRiskSweepMinutes"))),
                            int.Parse(environment.AppSettings("CaseRiskBatchSize")), DateTime.UtcNow, log, token)
                            .ConfigureAwait(false);

                        if (result.Examined > 0 && log.IsInfoEnabled)
                        {
                            log.Info($"Case Risk: examined {result.Examined} cases, wrote {result.Written} " +
                                     $"assessments, {result.Failed} failed.");
                        }
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        log.Error($"Case Risk: has created an error inside the loop {ex}");
                    }

                    await Task.Delay(int.Parse(environment.AppSettings("CaseRiskWait")), token)
                        .ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException ex)
            {
                log.Info($"Graceful Cancellation CaseRiskAsync: {ex.Message}");
            }
        }
    }
}
