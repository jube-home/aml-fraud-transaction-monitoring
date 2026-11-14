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
using Jube.Service.Query.CaseNarrative;
using Jube.Service.Query.CaseNarrative.Interfaces;
using log4net;
using Microsoft.Extensions.Hosting;

namespace Jube.App.Code
{
    public sealed class CaseNarrativeJobs(
        DynamicEnvironment.DynamicEnvironment dynamicEnvironment,
        ICaseNarrativeAgent agent,
        ILog log) : BackgroundService
    {
        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!"True".Equals(dynamicEnvironment.AppSettings("EnableCaseNarratives"),
                    StringComparison.OrdinalIgnoreCase))
            {
                return Task.CompletedTask;
            }

            var job = new CaseNarrativeJob(dynamicEnvironment.AppSettings("ConnectionString"), dynamicEnvironment,
                agent, log);

            return Task.WhenAll(
                RunAsync("Narratives", "WaitCaseNarratives", async token =>
                {
                    var written = await job.RunNarrativesAsync(token).ConfigureAwait(false);

                    if (log.IsInfoEnabled)
                    {
                        log.Info($"CaseNarrativeJobs: narratives written {written}.");
                    }
                }, TimeSpan.FromMinutes(2), stoppingToken));
        }

        private async Task RunAsync(string name, string wait, Func<CancellationToken, Task> cycle, TimeSpan delay,
            CancellationToken token)
        {
            var milliseconds = long.Parse(dynamicEnvironment.AppSettings(wait), NumberStyles.Integer,
                CultureInfo.InvariantCulture);

            try
            {
                await Task.Delay(delay, token).ConfigureAwait(false);

                while (!token.IsCancellationRequested)
                {
                    try
                    {
                        await cycle(token).ConfigureAwait(false);
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        log.Error($"CaseNarrativeJobs: the {name} cycle failed.", ex);
                    }

                    await Task.Delay(TimeSpan.FromMilliseconds(milliseconds), token).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
                log.Info($"Graceful Cancellation CaseNarrativeJobs: {name}.");
            }
        }
    }
}
