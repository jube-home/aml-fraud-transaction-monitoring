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
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;
using AdaptationHttpClientCache =
    Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.Models.Models.AdaptationHttpClientCache;

namespace Jube.Test.Engine.EntityAnalysisModelManager.EntityAnalysisModel.Models
{
    [Trait("Category", "Unit")]
    public sealed class AdaptationHttpClientCacheTests
    {
        private static async Task WaitUntilAsync(Func<bool> condition)
        {
            for (var i = 0; i < 200 && !condition(); i++)
            {
                await Task.Delay(25);
            }
        }

        [Fact]
        public void SameSettingsForATenantReuseTheClient()
        {
            using var cache = new AdaptationHttpClientCache(TimeSpan.FromMilliseconds(50));

            var first = cache.Acquire(1, 10, true, 1000, true);
            var second = cache.Acquire(1, 10, true, 1000, true);

            second.Should().BeSameAs(first);
            cache.PendingRetirements.Should().Be(0);
        }

        [Fact]
        public void DifferentTenantsGetDifferentClients()
        {
            using var cache = new AdaptationHttpClientCache(TimeSpan.FromMilliseconds(50));

            cache.Acquire(1, 10, true, 1000, true).Should().NotBeSameAs(cache.Acquire(2, 10, true, 1000, true));
            cache.PendingRetirements.Should().Be(0);
        }

        [Fact]
        public async Task ChangedSettingsRetireTheOldClientOnlyAfterTheGracePeriod()
        {
            using var cache = new AdaptationHttpClientCache(TimeSpan.FromMilliseconds(200));

            var old = cache.Acquire(1, 10, true, 100, true);
            var replacement = cache.Acquire(1, 10, false, 100, true);

            replacement.Should().NotBeSameAs(old);
            cache.PendingRetirements.Should().Be(1);

            await WaitUntilAsync(() => cache.PendingRetirements == 0);

            cache.PendingRetirements.Should().Be(0);

            var act = async () => await old.GetAsync("http://127.0.0.1:1/");

            await act.Should().ThrowAsync<ObjectDisposedException>();
            cache.Acquire(1, 10, false, 100, true).Should().BeSameAs(replacement);
        }

        [Fact]
        public async Task RetiringAClientDoesNotCutShortAnInFlightRequest()
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();

            try
            {
                var port = ((IPEndPoint)listener.LocalEndpoint).Port;

                var server = Task.Run(async () =>
                {
                    using var connection = await listener.AcceptTcpClientAsync();
                    var stream = connection.GetStream();
                    var buffer = new byte[8192];
                    await stream.ReadAsync(buffer);
                    await Task.Delay(600);

                    await stream.WriteAsync(Encoding.UTF8.GetBytes(
                        "HTTP/1.1 200 OK\r\nContent-Length: 2\r\nConnection: close\r\n\r\nok"));
                });

                using var cache = new AdaptationHttpClientCache(TimeSpan.FromSeconds(3));

                var old = cache.Acquire(1, 10, true, 5000, true);
                var inFlight = old.GetStringAsync($"http://127.0.0.1:{port}/score");

                await Task.Delay(100);
                cache.Acquire(1, 10, true, 6000, true);
                cache.PendingRetirements.Should().Be(1);

                (await inFlight).Should().Be("ok");
                await server;
            }
            finally
            {
                listener.Stop();
            }
        }

        [Fact]
        public async Task DisposingTheCacheStopsPendingRetirementsImmediately()
        {
            var cache = new AdaptationHttpClientCache(TimeSpan.FromMinutes(5));

            cache.Acquire(1, 10, true, 1000, true);
            cache.Acquire(1, 10, false, 1000, true);
            cache.PendingRetirements.Should().Be(1);

            cache.Dispose();

            await WaitUntilAsync(() => cache.PendingRetirements == 0);

            cache.PendingRetirements.Should().Be(0);
        }
    }
}
