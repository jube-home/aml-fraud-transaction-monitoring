/* Copyright (C) 2022-present Jube Holdings Limited.
 *
 * This file is part of Jube™ software.

 * Jube™ is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License
 * as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
 * Jube™ is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty
 * of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.

 * You should have received a copy of the GNU Affero General Public License along with Jube™. If not,
 * see <https://www.gnu.org/licenses/>.
 */

using System.Diagnostics.Metrics;
using System.Net;
using System.Text.RegularExpressions;
using Jube.Data.Context;
using Jube.Data.Repository;
using Jube.Monitoring;
using Jube.Tokenisation;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;

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

var dockerApiClient = new DockerApiClient(dockerSocketPath);
var containerLogSampler = new ContainerLogSampler(dockerApiClient);

using var haProxyHttpClient = new HttpClient();
haProxyHttpClient.Timeout = TimeSpan.FromSeconds(10);

var haProxyServerStatusSampler = new HaProxyServerStatusSampler(haProxyHttpClient, haProxyStatsUrl);
var haProxyReachabilityProbeSampler =
    new HaProxyReachabilityProbeSampler(haProxyHttpClient, haProxyTasksHostname);

var overlayNetworkTaskDriftSampler = new OverlayNetworkTaskDriftSampler(dockerApiClient);

var allowedProxyPaths = new[]
{
    new Regex(@"^/containers/json(\?.*)?$", RegexOptions.Compiled),
    new Regex(@"^/containers/[A-Za-z0-9_.-]+/stats(\?.*)?$", RegexOptions.Compiled),
    new Regex(@"^/containers/[A-Za-z0-9_.-]+/json$", RegexOptions.Compiled),
    new Regex(@"^/info$", RegexOptions.Compiled),
    new Regex(@"^/version$", RegexOptions.Compiled),
    new Regex(@"^/system/df$", RegexOptions.Compiled),
    new Regex(@"^/events(\?.*)?$", RegexOptions.Compiled)
};

var builder = WebApplication.CreateBuilder(args);
builder.Logging.AddSimpleConsole();
var app = builder.Build();

app.MapGet("/proxy", async (HttpContext context, CancellationToken token) =>
{
    var pathAndQuery = context.Request.QueryString.HasValue
        ? context.Request.Query["path"].ToString()
        : string.Empty;

    if (string.IsNullOrEmpty(pathAndQuery) || !pathAndQuery.StartsWith('/')
                                           || !allowedProxyPaths.Any(pattern => pattern.IsMatch(pathAndQuery)))
    {
        context.Response.StatusCode = (int)HttpStatusCode.Forbidden;

        return;
    }

    var (statusCode, contentType, body) = await dockerApiClient.ProxyGetAsync(pathAndQuery, token)
        .ConfigureAwait(false);

    context.Response.StatusCode = statusCode;
    context.Response.ContentType = contentType ?? "application/octet-stream";
    await context.Response.Body.WriteAsync(body, token).ConfigureAwait(false);
});

app.MapGet("/health", () => Results.Ok());

Console.WriteLine(
    $"Jube.Monitoring starting: socket={dockerSocketPath}, intervalSeconds={sampleIntervalSeconds}, instance={instance}.");

var samplingLoop = Task.Run(async () =>
{
    while (true)
    {
        try
        {
            var dbContext = DataConnectionDbContext.GetNgpsqlDbContextDataConnection(connectionString);

            await using (dbContext.ConfigureAwait(false))
            {
                var containerLogEntryRepository = new ContainerLogEntryRepository(dbContext);
                var haProxyServerStatusRepository = new HaProxyServerStatusRepository(dbContext);
                var haProxyReachabilityProbeRepository = new HaProxyReachabilityProbeRepository(dbContext);
                var overlayNetworkTaskDriftRepository = new OverlayNetworkTaskDriftRepository(dbContext);

                await logCounterMatcher.RefreshAsync(dbContext, CancellationToken.None).ConfigureAwait(false);

                var occurredDate = DateTime.UtcNow;
                var containerLogEntriesTask = containerLogSampler.SampleAsync();
                var haProxyServerStatusesTask = haProxyServerStatusSampler.SampleAsync();
                var haProxyReachabilityProbesTask = haProxyReachabilityProbeSampler.SampleAsync();
                var overlayNetworkTaskDriftsTask = overlayNetworkTaskDriftSampler.SampleAsync();

                await Task.WhenAll(containerLogEntriesTask, haProxyServerStatusesTask,
                        haProxyReachabilityProbesTask, overlayNetworkTaskDriftsTask)
                    .ConfigureAwait(false);

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
                    $"Jube.Monitoring sampled {containerLogEntries.Count} log line(s), " +
                    $"{haProxyServerStatuses.Count} HAProxy server row(s), " +
                    $"{haProxyReachabilityProbes.Count} reachability probe(s) and " +
                    $"{overlayNetworkTaskDrifts.Count} overlay network drift row(s) at {occurredDate:O}.");
            }
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine($"Jube.Monitoring sampling cycle failed: {exception}");
        }

        await Task.Delay(TimeSpan.FromSeconds(sampleIntervalSeconds)).ConfigureAwait(false);
    }
});

await app.RunAsync().ConfigureAwait(false);
await samplingLoop.ConfigureAwait(false);

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