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
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Jube.Service.Query.CaseTriage;
using Jube.Service.Query.CaseTriage.Interfaces;
using log4net;
using Microsoft.Extensions.Hosting;

namespace Jube.App.Code
{
    public sealed class CaseTriageReplayJobs(
        DynamicEnvironment.DynamicEnvironment dynamicEnvironment,
        ICaseTriageReplayAgent agent,
        ILog log) : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!"True".Equals(dynamicEnvironment.AppSettings("EnableCaseTriageReplay"),
                    StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var job = new CaseTriageReplayJob(dynamicEnvironment.AppSettings("ConnectionString"), dynamicEnvironment,
                agent, log);

            var milliseconds = long.Parse(dynamicEnvironment.AppSettings("WaitCaseTriageReplay"),
                NumberStyles.Integer, CultureInfo.InvariantCulture);

            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    try
                    {
                        var ran = await job.RunPendingAsync(stoppingToken).ConfigureAwait(false);

                        if (ran > 0 && log.IsInfoEnabled)
                        {
                            log.Info($"CaseTriageReplayJobs: replays run {ran}.");
                        }
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        log.Error("CaseTriageReplayJobs: the cycle failed.", ex);
                    }

                    await Task.Delay(TimeSpan.FromMilliseconds(milliseconds), stoppingToken).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
                log.Info("Graceful Cancellation CaseTriageReplayJobs.");
            }
        }
    }
}
