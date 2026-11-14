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
    using CaseDigest;
    using Context;
    using Data.Context;
    using Jube.Case;

    public class CaseDigestStarter(Context context)
    {
        public static bool IsEnabled(DynamicEnvironment.DynamicEnvironment environment)
        {
            return environment.AppSettings("EnableCaseDigest").Equals("True", StringComparison.OrdinalIgnoreCase);
        }

        public async Task StartAsync()
        {
            var token = context.Services.TaskCoordinator.CancellationToken;
            var environment = context.Services.DynamicEnvironment;
            var log = context.Services.Log;
            DateTime? lastRun = null;

            try
            {
                while (!token.IsCancellationRequested)
                {
                    try
                    {
                        var now = DateTime.UtcNow;

                        if (CaseDigest.IsDue(now, int.Parse(environment.AppSettings("CaseDigestHourUtc")),
                                environment.AppSettings("CaseDigestDays"), lastRun))
                        {
                            lastRun = now;

                            await using var dbContext = DataConnectionDbContext.GetResilientDbContextDataConnection(
                                environment.AppSettings("ConnectionString"), log);

                            var email = environment.AppSettings("CaseDigestEmail")
                                .Equals("True", StringComparison.OrdinalIgnoreCase);

                            var mail = new SendMail(environment, log);

                            var result = await CaseDigest.RunAsync(dbContext, now, email,
                                (to, subject, body) => mail.TrySend(to, subject, body, false).Ok, token)
                                .ConfigureAwait(false);

                            log.Info($"Case Digest: notified {result.Notified} and emailed {result.Emailed}.");
                        }
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        log.Error($"Case Digest: has created an error inside the loop {ex}");
                    }

                    await Task.Delay(TimeSpan.FromSeconds(60), token).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException ex)
            {
                log.Info($"Graceful Cancellation CaseDigestStarter: {ex.Message}");
            }
        }
    }
}
