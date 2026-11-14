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
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Jube.ResilientHttp;
using Jube.Svirfd.Interfaces;
using Microsoft.Extensions.Logging;
using Polly.CircuitBreaker;
using Polly.Timeout;
using Svirfd;

namespace Jube.Svirfd
{
    public sealed class SvirfdHost : ISvirfdLookup, IAsyncDisposable
    {
        private readonly SvirfdSettings settings;
        private readonly ILogger logger;
        private readonly SvirfdClient? client;
        private readonly SvirfdRemoteCache cache;
        private readonly SvirfdRemoteCircuit circuit = new();
        private readonly ResilientHttpExecutor executor;
        private readonly string breakerKey;
        private readonly HttpResilienceOptions resilience;
        private long seq = -1;
        private bool createFailed;

        public SvirfdHost(SvirfdSettings settings, HttpClient http, ILogger logger, ResilientHttpExecutor? executor = null)
        {
            this.executor = executor ?? new ResilientHttpExecutor(TimeProvider.System);
            this.settings = settings;
            this.logger = logger;
            resilience = settings.ToResilienceOptions();
            breakerKey = Uri.TryCreate(settings.BaseUrl, UriKind.Absolute, out var baseUri)
                ? baseUri.Authority
                : settings.BaseUrl;
            cache = new SvirfdRemoteCache(settings.RemoteCacheLifetime, settings.RemoteCacheMaximumEntries);

            if (!settings.Enabled)
            {
                return;
            }

            if (settings.Mode != SvirfdMode.Directory && !settings.BaseUrlIsSecure)
            {
                createFailed = true;
                logger.LogError("Svirfd: SvirfdBaseUrl must be an https address without credentials (plain http is accepted only for a loopback host), so the API key and addresses are not sent in clear and lookups are off.");

                return;
            }

            try
            {
                client = new SvirfdClient(settings.ToOptions(), http, logger);
                client.Updated += sequence => Interlocked.Exchange(ref seq, sequence);
            }
            catch (ArgumentException ex)
            {
                createFailed = true;
                logger.LogError(ex, "Svirfd: the client could not be created from the settings, so lookups are off.");
            }
        }

        public bool Enabled => settings.Enabled && client != null;
        public bool Ready => !createFailed && client is { Ready: true };
        public bool RemoteEngineLookup => settings.RemoteEngineLookup;
        public SvirfdMode Mode => client?.Mode ?? settings.Mode;
        public long Seq => client?.Seq ?? Interlocked.Read(ref seq);
        public bool StartFailed => createFailed || client is { State: SvirfdState.Failed };
        public SvirfdState State => client?.State ?? SvirfdState.Failed;
        public string LastError => client?.LastError is { } error ? $"{error.GetType().Name}: {error.Message}" : string.Empty;
        public DateTime? NextRetryAt => client?.NextRetryAt?.UtcDateTime;
        public TimeSpan? Staleness => client?.Staleness;
        public DateTime CircuitOpenUntil => circuit.OpenUntil;
        public int CachedRecords => cache.Count;

        public string Attribution =>
            string.IsNullOrWhiteSpace(client?.Attribution) ? SvirfdCredit.Line : client!.Attribution;

        public string LicenceUrl =>
            string.IsNullOrWhiteSpace(client?.LicenceUrl) ? SvirfdCredit.LicenceUrl : client!.LicenceUrl;

        public async Task StartAsync(CancellationToken token)
        {
            if (client == null || createFailed)
            {
                return;
            }

            try
            {
                client.StateChanged += state => logger.LogInformation("Svirfd: state is now {State}.", state);

                await client.StartAsync(token).ConfigureAwait(false);

                if (settings.WaitForReady || !settings.BackgroundStart)
                {
                    await client.WaitUntilReadyAsync(token).ConfigureAwait(false);
                }

                logger.LogInformation(
                    "Svirfd: started in {Mode} mode, state {State}, sequence {Seq}{Remote}.", client.Mode, client.State,
                    client.Seq,
                    client.Mode == SvirfdMode.Remote
                        ? settings.RemoteEngineLookup
                            ? " with per-transaction lookups going to the service"
                            : " with per-transaction lookups off"
                        : string.Empty);
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Svirfd: the client did not start. {Reason}", $"{ex.GetType().Name}: {ex.Message}");
            }
        }

        public bool TryLookup(string? address, out IpResponse? record)
        {
            record = null;

            if (!Ready || client!.Mode == SvirfdMode.Remote || !TryParse(address, out var parsed))
            {
                return false;
            }

            record = client.Lookup(parsed);

            return record != null;
        }

        public Task<IpResponse?> LookupAsync(string? address, CancellationToken token)
        {
            return LookupAsync(address, true, false, token);
        }

        public Task<IpResponse?> LookupFreshAsync(string? address, CancellationToken token)
        {
            return LookupAsync(address, true, true, token);
        }

        public Task<IpResponse?> LookupAsync(string? address, bool remoteAllowed, CancellationToken token)
        {
            return LookupAsync(address, remoteAllowed, false, token);
        }

        public Task<IpResponse?> LookupFreshAsync(string? address, bool remoteAllowed, CancellationToken token)
        {
            return LookupAsync(address, remoteAllowed, true, token);
        }

        public async ValueTask DisposeAsync()
        {
            if (client != null)
            {
                await client.DisposeAsync().ConfigureAwait(false);
            }
        }

        private async Task<IpResponse?> LookupAsync(string? address, bool remoteAllowed, bool fresh,
            CancellationToken token)
        {
            if (!Ready || !TryParse(address, out var parsed))
            {
                return null;
            }

            if (client!.Mode != SvirfdMode.Remote)
            {
                return client.Lookup(parsed);
            }

            if (!remoteAllowed || !SvirfdAddressScope.IsPubliclyRoutable(parsed))
            {
                return null;
            }

            var key = parsed.ToString();
            var now = DateTime.UtcNow;

            if (!fresh && cache.TryGet(key, now, out var cached))
            {
                return cached;
            }

            if (circuit.IsOpen(now))
            {
                return null;
            }

            try
            {
                var record = await executor.ExecuteAsync(breakerKey, resilience,
                    ct => client.LookupAsync(parsed, ct), token).ConfigureAwait(false);

                if (record != null)
                {
                    cache.Set(key, record, DateTime.UtcNow);
                }

                return record;
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
                throw;
            }
            catch (TimeoutRejectedException)
            {
                circuit.Open(DateTime.UtcNow, TimeSpan.FromSeconds(5));

                logger.LogWarning(
                    "Svirfd: the remote lookup of {Address} timed out after {Timeout}; remote lookups pause for five seconds.",
                    key, settings.RemoteLookupTimeout);

                return null;
            }
            catch (BrokenCircuitException ex)
            {
                circuit.Open(DateTime.UtcNow, ex.RetryAfter ?? TimeSpan.FromSeconds(5));

                logger.LogWarning(
                    "Svirfd: repeated remote failures opened the service breaker; remote lookups pause for {Delay}.",
                    ex.RetryAfter ?? TimeSpan.FromSeconds(5));

                return null;
            }
            catch (HttpRequestException ex)
            {
                Trip(ex, key);

                return null;
            }
            catch (Exception ex) when (ex is not OutOfMemoryException)
            {
                logger.LogError(ex, "Svirfd: the remote lookup of {Address} failed.", key);

                return null;
            }
        }

        private static bool TryParse(string? address, out IPAddress parsed)
        {
            if (!IPAddress.TryParse(address, out var candidate))
            {
                parsed = IPAddress.None;

                return false;
            }

            parsed = candidate.IsIPv4MappedToIPv6 ? candidate.MapToIPv4() : candidate;

            return true;
        }

        private void Trip(HttpRequestException ex, string address)
        {
            var now = DateTime.UtcNow;
            var retryAfter = (ex as SvirfdException)?.RetryAfter;

            switch (ex)
            {
                case SvirfdRateLimitedException:
                    var delay = retryAfter ?? circuit.OpenForRateLimit(now);

                    if (retryAfter.HasValue)
                    {
                        circuit.Open(now, delay);
                    }

                    logger.LogWarning(
                        "Svirfd: the service rate limited the lookup of {Address}; remote lookups pause for {Delay}.",
                        address, delay);

                    break;
                case SvirfdUnauthorizedException:
                    circuit.OpenIndefinitely();

                    logger.LogError(ex,
                        "Svirfd: the service refused the API key on the lookup of {Address}; remote lookups are off until restart.",
                        address);

                    break;
                case SvirfdUnavailableException:
                    circuit.Open(now, retryAfter ?? TimeSpan.FromSeconds(5));

                    logger.LogWarning("Svirfd: the service index is loading; remote lookups pause for {Delay}.",
                        retryAfter ?? TimeSpan.FromSeconds(5));

                    break;
                default:
                    circuit.Open(now, TimeSpan.FromSeconds(5));

                    logger.LogWarning(ex, "Svirfd: the remote lookup of {Address} failed; remote lookups pause for five seconds.",
                        address);

                    break;
            }
        }
    }
}
