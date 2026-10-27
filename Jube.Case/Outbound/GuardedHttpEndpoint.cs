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

using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using log4net;

namespace Jube.Case.Outbound
{
    public sealed class GuardedHttpEndpoint
    {
        private static readonly ConcurrentDictionary<string, GuardedHttpEndpoint> shared = new();

        private readonly HttpClient client;
        private readonly OutboundHostPolicy policy;
        private readonly IHostResolver resolver;

        public GuardedHttpEndpoint(OutboundHostPolicy policy, IHostResolver? resolver = null)
        {
            this.policy = policy;
            this.resolver = resolver ?? new DnsHostResolver();

            client = new HttpClient(new SocketsHttpHandler
            {
                AllowAutoRedirect = false,
                UseProxy = false,
                UseCookies = false,
                PooledConnectionLifetime = TimeSpan.FromMinutes(2),
                ConnectCallback = ConnectAsync
            }, true);
        }

        public static GuardedHttpEndpoint For(DynamicEnvironment.DynamicEnvironment environment)
        {
            var policy = OutboundHostPolicy.From(environment);

            return shared.GetOrAdd(policy.Fingerprint, _ => new GuardedHttpEndpoint(policy));
        }

        public Task<DispatchResult> PostAsync(string url, string body, ILog log, string? headerName = null,
            string? headerValue = null, CancellationToken token = default)
        {
            return SendAsync(url, HttpMethod.Post, body, log, headerName, headerValue, token);
        }

        public Task<DispatchResult> GetAsync(string url, ILog log, string? headerName = null,
            string? headerValue = null, CancellationToken token = default)
        {
            return SendAsync(url, HttpMethod.Get, null, log, headerName, headerValue, token);
        }

        private async Task<DispatchResult> SendAsync(string url, HttpMethod method, string? body, ILog log,
            string? headerName, string? headerValue, CancellationToken token)
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            {
                return DispatchResult.Refused(PolicyCode.NotAbsolute);
            }

            var refusal = policy.CheckUri(uri);

            if (refusal != null)
            {
                return DispatchResult.Refused(refusal);
            }

            var correlationId = Guid.NewGuid();

            try
            {
                using var request = new HttpRequestMessage(method, uri);
                request.Headers.TryAddWithoutValidation("X-Correlation-Id", correlationId.ToString());

                if (!string.IsNullOrEmpty(headerName) && !string.IsNullOrEmpty(headerValue))
                {
                    request.Headers.TryAddWithoutValidation(headerName, headerValue);
                }

                if (body != null)
                {
                    request.Content = new StringContent(body, Encoding.UTF8, "application/json");
                }

                using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token)
                    .ConfigureAwait(false);

                var status = (int)response.StatusCode;

                if (status is >= 300 and < 400)
                {
                    return new DispatchResult(false, status,
                        $"Refused by the outbound host policy ({PolicyCode.RedirectRefused}).", correlationId,
                        PolicyCode.RedirectRefused);
                }

                response.EnsureSuccessStatusCode();

                return DispatchResult.Success(status, correlationId);
            }
            catch (HttpRequestException ex) when (ex.InnerException is OutboundPolicyException policyException)
            {
                return new DispatchResult(false, null,
                    $"Refused by the outbound host policy ({policyException.Code}).", correlationId,
                    policyException.Code);
            }
            catch (Exception ex) when (ex is not OperationCanceledException || !token.IsCancellationRequested)
            {
                log.Error(
                    $"Failed to dispatch to host {uri.Host} for verb {method}, correlationId={correlationId}: {ex.GetType().Name}");

                return DispatchResult.Failure($"{ex.GetType().Name}: {Scrub(ex.Message)}",
                    ex is HttpRequestException { StatusCode: { } code } ? (int)code : null, correlationId);
            }
        }

        private static string Scrub(string message)
        {
            return message.Length > 200 ? message[..200] : message;
        }

        private async ValueTask<Stream> ConnectAsync(SocketsHttpConnectionContext context, CancellationToken token)
        {
            var host = context.DnsEndPoint.Host;

            IReadOnlyList<IPAddress> addresses = HostSpelling.TryParseAddress(host, out var literal)
                ? [literal]
                : await resolver.ResolveAsync(host, token).ConfigureAwait(false);

            var refusal = policy.CheckResolved(host, addresses, out var permitted);

            if (refusal != null)
            {
                throw new OutboundPolicyException(refusal);
            }

            Exception? last = null;

            foreach (var address in permitted)
            {
                var socket = new Socket(address.AddressFamily, SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };

                try
                {
                    await socket.ConnectAsync(new IPEndPoint(address, context.DnsEndPoint.Port), token)
                        .ConfigureAwait(false);

                    return new NetworkStream(socket, true);
                }
                catch (Exception ex)
                {
                    socket.Dispose();
                    last = ex;
                }
            }

            throw new HttpRequestException("The host could not be reached.", last);
        }
    }
}
