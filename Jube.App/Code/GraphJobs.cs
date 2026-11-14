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
using Jube.Service.Query.Graph.Jobs;
using Jube.Service.Query.Graph.Resolution;
using log4net;
using Microsoft.Extensions.Hosting;

namespace Jube.App.Code
{
    public sealed class GraphJobs(DynamicEnvironment.DynamicEnvironment dynamicEnvironment, ILog log)
        : BackgroundService
    {
        protected override Task ExecuteAsync(CancellationToken stoppingToken) => Task.WhenAll(
            RunAsync("ArchiveKeyIndex", "EnableGraphArchiveKeyIndex", "WaitGraphArchiveKeyIndex", ArchiveKeyIndexAsync,
                stoppingToken, TimeSpan.FromSeconds(10)),
            RunAsync("Build", "EnableGraphBuild", "WaitGraphBuild", BuildAsync, stoppingToken,
                TimeSpan.FromSeconds(20)),
            RunAsync("BuildQueue", "EnableGraphBuild", "WaitGraphBuildQueue", BuildQueueAsync, stoppingToken,
                TimeSpan.FromSeconds(25)),
            RunAsync("Hubs", "EnableGraphHubs", "WaitGraphHubs", HubsAsync, stoppingToken, TimeSpan.FromSeconds(60)),
            RunAsync("Communities", "EnableGraphCommunities", "WaitGraphCommunities", CommunitiesAsync, stoppingToken,
                TimeSpan.FromSeconds(90)),
            RunAsync("Distance", "EnableGraphDistance", "WaitGraphDistance", DistanceAsync, stoppingToken,
                TimeSpan.FromSeconds(120)),
            RunAsync("Patterns", "EnableGraphPatterns", "WaitGraphPatterns", PatternsAsync, stoppingToken,
                TimeSpan.FromSeconds(150)),
            RunAsync("EntityResolution", "EnableGraphEntityResolution", "WaitGraphEntityResolution",
                EntityResolutionAsync, stoppingToken, TimeSpan.FromSeconds(180)),
            RunAsync("Retention", "EnableGraphRetention", "WaitGraphRetention", RetentionAsync, stoppingToken,
                TimeSpan.FromSeconds(15)));

        private async Task RunAsync(string name, string enable, string wait, Func<CancellationToken, Task> cycle,
            CancellationToken token, TimeSpan? delay = null)
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
                        log.Error($"GraphJobs: the {name} cycle failed.", ex);
                    }

                    await Task.Delay(TimeSpan.FromMilliseconds(milliseconds), token).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
                log.Info($"Graceful Cancellation GraphJobs: {name}.");
            }
        }

        private async Task ArchiveKeyIndexAsync(CancellationToken token)
        {
            var job = new GraphArchiveKeyIndexJob(dynamicEnvironment.AppSettings("ConnectionString"),
                dynamicEnvironment.AppSettings("MigrationConnectionString"), log);
            var result = await job.RunOnceAsync(token).ConfigureAwait(false);

            if (log.IsInfoEnabled && (result.Created || result.Skipped != null))
            {
                log.Info($"GraphJobs: ArchiveKey index valid {result.Valid}, created {result.Created}, " +
                         $"skipped {result.Skipped ?? "none"}.");
            }
        }

        private async Task EntityResolutionAsync(CancellationToken token)
        {
            var keys = (dynamicEnvironment.AppSettings("GraphEntityMatchFuzzyKeys") ?? string.Empty)
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            var job = new GraphEntityResolutionJob(dynamicEnvironment.AppSettings("ConnectionString"), log,
                new GraphEntityResolutionOptions(
                    int.Parse(dynamicEnvironment.AppSettings("GraphEntityMatchMaximumCluster"),
                        CultureInfo.InvariantCulture),
                    "True".Equals(dynamicEnvironment.AppSettings("GraphEntityMatchAutoConfirmHard"),
                        StringComparison.OrdinalIgnoreCase),
                    keys,
                    double.Parse(dynamicEnvironment.AppSettings("GraphEntityMatchMinimumPosterior"),
                        CultureInfo.InvariantCulture)));

            var result = await job.RunOnceAsync(token).ConfigureAwait(false);

            if (log.IsInfoEnabled && (result.HardCandidates > 0 || result.FuzzyCandidates > 0))
            {
                log.Info($"GraphJobs: entity resolution hard {result.HardCandidates}, fuzzy {result.FuzzyCandidates}, " +
                         $"auto-confirmed {result.AutoConfirmed}, resolved nodes {result.ResolvedNodes}.");
            }
        }

        private async Task BuildAsync(CancellationToken token)
        {
            var job = new GraphBuildJob(dynamicEnvironment.AppSettings("ConnectionString"), log,
                (int)Setting("GraphBuildBatchSize"), (int)Setting("GraphBuildMaximumBatchesPerCycle"),
                (int)Setting("GraphRetentionDays"),
                double.Parse(dynamicEnvironment.AppSettings("GraphReferenceAmount"), CultureInfo.InvariantCulture),
                "True".Equals(dynamicEnvironment.AppSettings("GraphBuildRequiresArchiveKeyIndex"),
                    StringComparison.OrdinalIgnoreCase));
            var result = await job.RunOnceAsync(token).ConfigureAwait(false);

            if (log.IsInfoEnabled && (result.Batches > 0 || result.Retracted > 0 || result.Skipped != null))
            {
                log.Info($"GraphJobs: build tenants {result.Tenants}, definitions {result.Definitions}, " +
                         $"batches {result.Batches}, events {result.Events}, retracted {result.Retracted}, " +
                         $"skipped {result.Skipped ?? "none"}.");
            }
        }

        private async Task BuildQueueAsync(CancellationToken token)
        {
            var job = new GraphBuildJob(dynamicEnvironment.AppSettings("ConnectionString"), log,
                (int)Setting("GraphBuildBatchSize"), (int)Setting("GraphBuildMaximumBatchesPerCycle"),
                (int)Setting("GraphRetentionDays"),
                double.Parse(dynamicEnvironment.AppSettings("GraphReferenceAmount"), CultureInfo.InvariantCulture),
                "True".Equals(dynamicEnvironment.AppSettings("GraphBuildRequiresArchiveKeyIndex"),
                    StringComparison.OrdinalIgnoreCase));
            var result = await job.ApplyQueuedAsync((int)Setting("GraphBuildQueueBatchSize"), token).ConfigureAwait(false);

            if (log.IsDebugEnabled && result.Entries > 0)
            {
                log.Debug($"GraphJobs: queue entries {result.Entries}, tenants {result.Tenants}, events {result.Events}, " +
                          $"dropped {result.Dropped}.");
            }
        }

        private async Task HubsAsync(CancellationToken token)
        {
            var job = new GraphHubJob(dynamicEnvironment.AppSettings("ConnectionString"), log,
                double.Parse(dynamicEnvironment.AppSettings("GraphHubPercentile"), CultureInfo.InvariantCulture),
                (int)Setting("GraphHubMinimumDegree"));
            var result = await job.RunOnceAsync(token).ConfigureAwait(false);

            if (log.IsInfoEnabled)
            {
                log.Info($"GraphJobs: hubs tenants {result.Tenants}, hubs {result.Hubs}, " +
                         $"edges reweighted {result.EdgesReweighted}.");
            }
        }

        private async Task CommunitiesAsync(CancellationToken token)
        {
            var job = new GraphCommunityJob(dynamicEnvironment.AppSettings("ConnectionString"), log,
                double.Parse(dynamicEnvironment.AppSettings("GraphCommunityResolution"), CultureInfo.InvariantCulture),
                (int)Setting("GraphAlgorithmsMaximumEdges"));
            var result = await job.RunOnceAsync(token).ConfigureAwait(false);

            if (log.IsInfoEnabled)
            {
                log.Info($"GraphJobs: communities tenants {result.Tenants}, nodes {result.Nodes}, " +
                         $"communities {result.Communities}, skipped {result.Skipped ?? "none"}.");
            }
        }

        private async Task DistanceAsync(CancellationToken token)
        {
            var job = new GraphDistanceJob(dynamicEnvironment.AppSettings("ConnectionString"), log,
                (int)Setting("GraphDistanceCap"), (int)Setting("GraphAlgorithmsMaximumEdges"));
            var result = await job.RunOnceAsync(token).ConfigureAwait(false);

            if (log.IsInfoEnabled)
            {
                log.Info($"GraphJobs: distance tenants {result.Tenants}, seeds {result.Seeds}, nodes {result.Nodes}.");
            }
        }

        private async Task PatternsAsync(CancellationToken token)
        {
            var job = new GraphPatternJob(dynamicEnvironment.AppSettings("ConnectionString"), log,
                (int)Setting("GraphPatternWindowDays"), (int)Setting("GraphPatternMaximumEvents"),
                double.Parse(dynamicEnvironment.AppSettings("GraphPatternStructuringThreshold"),
                    CultureInfo.InvariantCulture));
            var result = await job.RunOnceAsync(token).ConfigureAwait(false);

            if (log.IsInfoEnabled && result.Patterns > 0)
            {
                log.Info($"GraphJobs: patterns tenants {result.Tenants}, events {result.Events}, " +
                         $"patterns {result.Patterns}.");
            }
        }

        private Task RetentionAsync(CancellationToken token)
        {
            var job = new GraphRetentionJob(dynamicEnvironment.AppSettings("ConnectionString"),
                dynamicEnvironment.AppSettings("MigrationConnectionString"), log, (int)Setting("GraphRetentionDays"));

            return job.RunOnceAsync(token);
        }

        private long Setting(string name)
        {
            return long.Parse(dynamicEnvironment.AppSettings(name), NumberStyles.Integer,
                CultureInfo.InvariantCulture);
        }
    }
}
