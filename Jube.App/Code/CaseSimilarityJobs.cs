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
using Jube.Service.Query.CaseSimilarity;
using log4net;
using Microsoft.Extensions.Hosting;

namespace Jube.App.Code
{
    public sealed class CaseSimilarityJobs(DynamicEnvironment.DynamicEnvironment dynamicEnvironment, ILog log)
        : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!"True".Equals(dynamicEnvironment.AppSettings("EnableCaseSimilarity"),
                    StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var wait = TimeSpan.FromMilliseconds(Setting("WaitCaseSimilarity"));

            var job = new CaseSimilarityJob(dynamicEnvironment.AppSettings("ConnectionString"), dynamicEnvironment,
                log, new CaseSimilarityJobSettings
                {
                    RefreshAfter = TimeSpan.FromMinutes(Setting("CaseSimilarityRefreshMinutes")),
                    WindowDays = (int)Setting("CaseSimilarityWindowDays"),
                    MaximumRows = (int)Setting("CaseSimilarityMaximumRows"),
                    MaximumCases = (int)Setting("CaseSimilarityMaximumCases")
                });

            if (!job.HasHashingKey)
            {
                log.Warn("CaseSimilarityJobs: CaseSimilarityHashingKey is not set, so no similarity vector is built.");

                return;
            }

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken).ConfigureAwait(false);

                while (!stoppingToken.IsCancellationRequested)
                {
                    try
                    {
                        var result = await job.RunOnceAsync(stoppingToken).ConfigureAwait(false);

                        if (log.IsInfoEnabled && result.Checked > 0)
                        {
                            log.Info($"CaseSimilarityJobs: checked {result.Checked}, changed {result.Changed}, " +
                                     $"failed {result.Failed}.");
                        }
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        log.Error("CaseSimilarityJobs: the sweep failed.", ex);
                    }

                    await Task.Delay(wait, stoppingToken).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
                log.Info("Graceful Cancellation CaseSimilarityJobs.");
            }
        }

        private long Setting(string name)
        {
            return long.Parse(dynamicEnvironment.AppSettings(name), NumberStyles.Integer,
                CultureInfo.InvariantCulture);
        }
    }
}
