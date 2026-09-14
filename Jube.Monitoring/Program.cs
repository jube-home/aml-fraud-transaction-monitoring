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

using System.Diagnostics.Metrics;
using System.Net;
using Jube.Data.Context;
using Jube.Data.Repository;
using Jube.Monitoring;
using Jube.Tokenisation;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;

// ReSharper disable LocalizableElement

var connectionString = SubstituteSecrets(Environment.GetEnvironmentVariable("ConnectionString")
                                         ?? throw new InvalidOperationException(
                                             "The ConnectionString environment variable must be set."));
var dockerSocketPath = Environment.GetEnvironmentVariable("DockerSocketPath") ?? "/var/run/docker.sock";
var sampleIntervalSeconds = int.TryParse(Environment.GetEnvironmentVariable("SampleIntervalSeconds"),
    out var configuredSampleIntervalSeconds)
    ? configuredSampleIntervalSeconds
    : 60;

var haProxyStatsUrl = Environment.GetEnvironmentVariable("HAProxyStatsUrl") ?? "http://haproxy:7000/;csv";
var haProxyTasksHostname = Environment.GetEnvironmentVariable("HAProxyTasksHostname") ?? "tasks.haproxy";

var instance = Dns.GetHostName();
const string meterName = "Jube.Monitoring";

using var meter = new Meter(meterName);

var enableOpenTelemetry = string.Equals(Environment.GetEnvironmentVariable("EnableOpenTelemetry"), "True",
    StringComparison.OrdinalIgnoreCase);

if (enableOpenTelemetry)
{
    var openTelemetryBackendEndpoint = Environment.GetEnvironmentVariable("OpenTelemetryBackendEndpoint");
    Sdk.CreateMeterProviderBuilder()
        .ConfigureResource(r => r.AddService("Jube.Monitoring"))
        .AddMeter(meterName)
        .AddOtlpExporter(o =>
        {
            o.TimeoutMilliseconds = 5000;
            if (string.IsNullOrEmpty(openTelemetryBackendEndpoint))
            {
                return;
            }

            o.Protocol = OtlpExportProtocol.HttpProtobuf;
            o.Endpoint = new Uri($"{openTelemetryBackendEndpoint.TrimEnd('/')}/v1/metrics");
        })
        .Build();
}

var logCounterMatcher = new LogCounterMatcher(meter);

using var dockerApiClient = new DockerApiClient(dockerSocketPath);
var dockerContainerMetricSampler = new DockerContainerMetricSampler(dockerApiClient);
var dockerHostMetricSampler = new DockerHostMetricSampler(dockerApiClient);
var containerLogSampler = new ContainerLogSampler(dockerApiClient);
var dockerEventSampler = new DockerEventSampler(dockerApiClient);

using var haProxyHttpClient = new HttpClient();
haProxyHttpClient.Timeout = TimeSpan.FromSeconds(10);

var haProxyServerStatusSampler = new HaProxyServerStatusSampler(haProxyHttpClient, haProxyStatsUrl);
var haProxyReachabilityProbeSampler =
    new HaProxyReachabilityProbeSampler(haProxyHttpClient, haProxyTasksHostname);

var overlayNetworkTaskDriftSampler = new OverlayNetworkTaskDriftSampler(dockerApiClient);

Console.WriteLine(
    $"Jube.Monitoring starting: socket={dockerSocketPath}, intervalSeconds={sampleIntervalSeconds}, instance={instance}.");

while (true)
{
    try
    {
        var dbContext = DataConnectionDbContext.GetNgpsqlDbContextDataConnection(connectionString);
        await using (dbContext.ConfigureAwait(false))
        {
            var dockerContainerMetricRepository = new DockerContainerMetricRepository(dbContext);
            var dockerHostMetricRepository = new DockerHostMetricRepository(dbContext);
            var containerLogEntryRepository = new ContainerLogEntryRepository(dbContext);
            var dockerEventRepository = new DockerEventRepository(dbContext);
            var haProxyServerStatusRepository = new HaProxyServerStatusRepository(dbContext);
            var haProxyReachabilityProbeRepository = new HaProxyReachabilityProbeRepository(dbContext);
            var overlayNetworkTaskDriftRepository = new OverlayNetworkTaskDriftRepository(dbContext);

            await logCounterMatcher.RefreshAsync(dbContext, CancellationToken.None).ConfigureAwait(false);

            var occurredDate = DateTime.UtcNow;
            var containerMetricsTask = dockerContainerMetricSampler.SampleAsync();
            var hostMetricTask = dockerHostMetricSampler.SampleAsync();
            var containerLogEntriesTask = containerLogSampler.SampleAsync();
            var dockerEventsTask = dockerEventSampler.SampleAsync();
            var haProxyServerStatusesTask = haProxyServerStatusSampler.SampleAsync();
            var haProxyReachabilityProbesTask = haProxyReachabilityProbeSampler.SampleAsync();
            var overlayNetworkTaskDriftsTask = overlayNetworkTaskDriftSampler.SampleAsync();

            await Task.WhenAll(containerMetricsTask, hostMetricTask, containerLogEntriesTask, dockerEventsTask,
                    haProxyServerStatusesTask, haProxyReachabilityProbesTask, overlayNetworkTaskDriftsTask)
                .ConfigureAwait(false);

            var containerMetrics = await containerMetricsTask.ConfigureAwait(false);
            foreach (var containerMetric in containerMetrics)
            {
                containerMetric.OccurredDate = occurredDate;
                containerMetric.CreatedDate = occurredDate;
                containerMetric.Instance = instance;
                await dockerContainerMetricRepository.InsertAsync(containerMetric).ConfigureAwait(false);
            }

            var hostMetric = await hostMetricTask.ConfigureAwait(false);
            if (hostMetric != null)
            {
                hostMetric.CreatedDate = occurredDate;
                hostMetric.Instance = instance;
                await dockerHostMetricRepository.InsertAsync(hostMetric).ConfigureAwait(false);
            }

            var containerLogEntries = await containerLogEntriesTask.ConfigureAwait(false);
            foreach (var entry in containerLogEntries)
            {
                entry.CreatedDate = occurredDate;
                entry.Instance = instance;
                logCounterMatcher.CheckAndIncrement(entry.Message);
            }

            if (containerLogEntries.Count > 0)
            {
                await containerLogEntryRepository.BulkCopyAsync(containerLogEntries).ConfigureAwait(false);
            }

            var dockerEvents = await dockerEventsTask.ConfigureAwait(false);
            foreach (var dockerEvent in dockerEvents)
            {
                dockerEvent.CreatedDate = occurredDate;
                dockerEvent.Instance = instance;
            }

            if (dockerEvents.Count > 0)
            {
                await dockerEventRepository.BulkCopyAsync(dockerEvents).ConfigureAwait(false);
            }

            var haProxyServerStatuses = await haProxyServerStatusesTask.ConfigureAwait(false);
            foreach (var serverStatus in haProxyServerStatuses)
            {
                serverStatus.OccurredDate = occurredDate;
                serverStatus.CreatedDate = occurredDate;
                serverStatus.Instance = instance;
            }

            if (haProxyServerStatuses.Count > 0)
            {
                await haProxyServerStatusRepository.BulkCopyAsync(haProxyServerStatuses).ConfigureAwait(false);
            }

            var haProxyReachabilityProbes = await haProxyReachabilityProbesTask.ConfigureAwait(false);
            foreach (var probe in haProxyReachabilityProbes)
            {
                probe.OccurredDate = occurredDate;
                probe.CreatedDate = occurredDate;
                probe.Instance = instance;
            }

            if (haProxyReachabilityProbes.Count > 0)
            {
                await haProxyReachabilityProbeRepository.BulkCopyAsync(haProxyReachabilityProbes)
                    .ConfigureAwait(false);
            }

            var overlayNetworkTaskDrifts = await overlayNetworkTaskDriftsTask.ConfigureAwait(false);
            foreach (var drift in overlayNetworkTaskDrifts)
            {
                drift.OccurredDate = occurredDate;
                drift.CreatedDate = occurredDate;
                drift.Instance = instance;
            }

            if (overlayNetworkTaskDrifts.Count > 0)
            {
                await overlayNetworkTaskDriftRepository.BulkCopyAsync(overlayNetworkTaskDrifts)
                    .ConfigureAwait(false);
            }

            Console.WriteLine(
                $"Jube.Monitoring sampled {containerMetrics.Count} container(s), " +
                $"{(hostMetric == null ? "no" : "one")} host metric, {containerLogEntries.Count} log line(s), " +
                $"{dockerEvents.Count} event(s), {haProxyServerStatuses.Count} HAProxy server row(s), " +
                $"{haProxyReachabilityProbes.Count} reachability probe(s) and {overlayNetworkTaskDrifts.Count} " +
                $"overlay network drift row(s) at {occurredDate:O}.");
        }
    }
    catch (Exception exception)
    {
        Console.Error.WriteLine($"Jube.Monitoring sampling cycle failed: {exception}");
    }

    await Task.Delay(TimeSpan.FromSeconds(sampleIntervalSeconds)).ConfigureAwait(false);
}

static string SubstituteSecrets(string value)
{
    var secretsPath = Environment.GetEnvironmentVariable("SecretsPath") ?? string.Empty;

    foreach (var token in Tokenisation.ReturnTokens(value))
    {
        var path = Path.Combine(secretsPath, token);
        if (!File.Exists(path))
        {
            Console.WriteLine($"Could not find secret file {path} for token {token}.");
            continue;
        }

        var secret = File.ReadAllText(path).Trim();
        if (secret.Length == 0)
        {
            Console.WriteLine($"Secret file {path} for token {token} is empty.");
            continue;
        }

        value = value.Replace($"[@{token}@]", secret);
    }

    return value;
}