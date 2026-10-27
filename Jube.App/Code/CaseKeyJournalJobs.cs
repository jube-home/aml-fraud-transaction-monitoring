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
using Jube.Service.Query.CaseKeyJournal;
using Jube.Service.Reactivity.Interfaces;
using log4net;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Localization;

namespace Jube.App.Code
{
    public sealed class CaseKeyJournalJobs(
        DynamicEnvironment.DynamicEnvironment dynamicEnvironment,
        ILog log,
        IStringLocalizerFactory localizers,
        IServiceChangeBus serviceChangeBus) : BackgroundService
    {
        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            return Task.WhenAll(
                RunAsync("Peers", "EnableCaseKeyJournalPeers", "WaitCaseKeyJournalPeers", PeersAsync, stoppingToken),
                RunAsync("Indexing", "EnableCaseKeyJournalIndexing", "WaitCaseKeyJournalIndexing", IndexingAsync,
                    stoppingToken),
                RunAsync("TagJobs", "EnableCaseKeyJournalTagJobs", "WaitCaseKeyJournalTagJobs", TagJobsAsync,
                    stoppingToken, TimeSpan.FromSeconds(5)));
        }

        private async Task RunAsync(string name, string enable, string wait,
            Func<CancellationToken, Task> cycle, CancellationToken token, TimeSpan? delay = null)
        {
            if (!"True".Equals(dynamicEnvironment.AppSettings(enable), StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var milliseconds = Setting(wait);

            try
            {
                await Task.Delay(delay ?? TimeSpan.FromSeconds(30), token).ConfigureAwait(false);

                while (!token.IsCancellationRequested)
                {
                    try
                    {
                        await cycle(token).ConfigureAwait(false);
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        log.Error($"CaseKeyJournalJobs: the {name} cycle failed.", ex);
                    }

                    await Task.Delay(TimeSpan.FromMilliseconds(milliseconds), token).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
                log.Info($"Graceful Cancellation CaseKeyJournalJobs: {name}.");
            }
        }

        private async Task PeersAsync(CancellationToken token)
        {
            var wait = TimeSpan.FromMilliseconds(Setting("WaitCaseKeyJournalPeers"));
            var job = new CaseKeyJournalPeerJob(dynamicEnvironment.AppSettings("ConnectionString"), log,
                (int)Setting("CaseKeyJournalPeerWindowDays"), (int)Setting("CaseKeyJournalPeerMaximumRows"),
                wait / 2);

            var result = await job.RunOnceAsync(token).ConfigureAwait(false);

            if (log.IsInfoEnabled)
            {
                log.Info($"CaseKeyJournalJobs: peers rebuilt {result.Rebuilt}, fresh {result.Fresh}, " +
                         $"distributions {result.Distributions}.");
            }
        }

        private async Task IndexingAsync(CancellationToken token)
        {
            var job = new CaseKeyJournalIndexJob(dynamicEnvironment.AppSettings("ConnectionString"),
                dynamicEnvironment.AppSettings("MigrationConnectionString"), log,
                (int)Setting("CaseKeyJournalIndexMinimumRows"), (int)Setting("CaseKeyJournalIndexMaximum"));

            var result = await job.RunOnceAsync(token).ConfigureAwait(false);

            if (log.IsInfoEnabled)
            {
                log.Info($"CaseKeyJournalJobs: indexing created {result.Created.Count}, failed {result.Failed.Count}, " +
                         $"lost {result.Lost.Count}, skipped {result.Skipped}.");
            }
        }

        private async Task TagJobsAsync(CancellationToken token)
        {
            var runner = new CaseKeyJournalTagJobRunner(dynamicEnvironment, log, localizers, serviceChangeBus);

            while (await runner.RunNextAsync(token).ConfigureAwait(false))
            {
                token.ThrowIfCancellationRequested();
            }
        }

        private long Setting(string name)
        {
            return long.Parse(dynamicEnvironment.AppSettings(name), NumberStyles.Integer,
                CultureInfo.InvariantCulture);
        }
    }
}
