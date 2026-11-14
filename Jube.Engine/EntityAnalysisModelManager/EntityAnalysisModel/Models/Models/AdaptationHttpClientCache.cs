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

namespace Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.Models.Models
{
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;

    public sealed class AdaptationHttpClientCache(TimeSpan retirementMargin) : IDisposable
    {
        private readonly CancellationTokenSource shutdown = new();
        private readonly Dictionary<int, Entry> current = new();
        private readonly object gate = new();
        private int pendingRetirements;

        public static AdaptationHttpClientCache Shared { get; } = new(TimeSpan.FromSeconds(60));

        public int PendingRetirements => Volatile.Read(ref pendingRetirements);

        public void Dispose()
        {
            List<Entry> entries;

            lock (gate)
            {
                entries = [..current.Values];
                current.Clear();
            }

            shutdown.Cancel();

            foreach (var entry in entries)
            {
                entry.Client.Dispose();
            }
        }

        public static HttpClient Create(int maxConnections, bool validateSsl, int timeout, bool allowInsecureTargets)
        {
            var handler = new SocketsHttpHandler
            {
                AllowAutoRedirect = false,
                UseCookies = false,
                UseProxy = false,
                MaxConnectionsPerServer = maxConnections,
                PooledConnectionLifetime = TimeSpan.FromMinutes(2),
                ConnectTimeout = TimeSpan.FromMilliseconds(timeout),
                ConnectCallback = (context, token) =>
                    AdaptationConnector.ConnectAsync(context, allowInsecureTargets, token)
            };

            if (!validateSsl)
            {
                handler.SslOptions.RemoteCertificateValidationCallback = (_, _, _, _) => true;
            }

            return new HttpClient(handler, true)
            {
                MaxResponseContentBufferSize = EntityAnalysisModelHttpAdaptation.MaximumResponseBytes,
                Timeout = TimeSpan.FromMilliseconds(timeout)
            };
        }

        public HttpClient Acquire(int tenantRegistryId, int maxConnections, bool validateSsl, int timeout,
            bool allowInsecureTargets)
        {
            var key = new Key(maxConnections, validateSsl, timeout, allowInsecureTargets);

            lock (gate)
            {
                if (current.TryGetValue(tenantRegistryId, out var existing))
                {
                    if (existing.Key == key)
                    {
                        return existing.Client;
                    }

                    Retire(existing);
                }

                var created = new Entry(key, Create(maxConnections, validateSsl, timeout, allowInsecureTargets));
                current[tenantRegistryId] = created;

                return created.Client;
            }
        }

        private void Retire(Entry entry)
        {
            Interlocked.Increment(ref pendingRetirements);

            _ = DisposeAfterGraceAsync(entry);
        }

        private async Task DisposeAfterGraceAsync(Entry entry)
        {
            try
            {
                await Task.Delay(TimeSpan.FromMilliseconds(entry.Key.Timeout) + retirementMargin, shutdown.Token)
                    .ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
            }
            finally
            {
                entry.Client.Dispose();
                Interlocked.Decrement(ref pendingRetirements);
            }
        }

        private readonly record struct Key(int MaxConnections, bool ValidateSsl, int Timeout,
            bool AllowInsecureTargets);

        private sealed record Entry(Key Key, HttpClient Client);
    }
}
