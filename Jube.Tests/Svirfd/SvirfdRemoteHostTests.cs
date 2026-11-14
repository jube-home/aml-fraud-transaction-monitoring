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
using FluentAssertions;
using Jube.Svirfd;
using Microsoft.Extensions.Logging.Abstractions;
using Svirfd;
using Xunit;

namespace Jube.Test.Svirfd
{
    [Trait("Category", "Unit")]
    public sealed class SvirfdRemoteHostTests
    {
        private static SvirfdSettings Remote(TimeSpan? timeout = null)
        {
            return new SvirfdSettings
            {
                Mode = SvirfdMode.Remote,
                BaseUrl = "https://svirfd.test",
                RemoteLookupTimeout = timeout ?? TimeSpan.FromSeconds(5)
            };
        }

        private static async Task<SvirfdHost> StartAsync(StubSvirfdHandler handler, SvirfdSettings? settings = null)
        {
            var host = new SvirfdHost(settings ?? Remote(), new HttpClient(handler), NullLogger.Instance);
            await host.StartAsync(CancellationToken.None);

            return host;
        }

        private static Task<HttpResponseMessage> Json(string body, HttpStatusCode status = HttpStatusCode.OK)
        {
            return Task.FromResult(new HttpResponseMessage(status)
            {
                Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json")
            });
        }

        [Fact]
        public async Task RemoteLookupsAreCachedAndFreshLookupsBypassTheCache()
        {
            var json = SvirfdJson.Serialize(await SvirfdFixture.RecordAsync("37.30.56.54"));
            var handler = new StubSvirfdHandler((request, _) =>
            {
                request.RequestUri!.ToString().Should().Be("https://svirfd.test/8.8.8.8");

                return Json(json);
            });

            await using var host = await StartAsync(handler);

            host.Ready.Should().BeTrue();
            host.Mode.Should().Be(SvirfdMode.Remote);
            host.TryLookup("8.8.8.8", out _).Should().BeFalse();

            (await host.LookupAsync("8.8.8.8", CancellationToken.None))!.Asn!.Name.Should().Be("T-Mobile Polska S.A.");
            (await host.LookupAsync("8.8.8.8", CancellationToken.None))!.Asn!.Name.Should().Be("T-Mobile Polska S.A.");
            handler.Calls.Should().Be(1);

            (await host.LookupFreshAsync("8.8.8.8", CancellationToken.None)).Should().NotBeNull();
            handler.Calls.Should().Be(2);
            host.CachedRecords.Should().Be(1);
            host.Attribution.Should().Be(SvirfdCredit.Line);
        }

        [Fact]
        public async Task RateLimitOpensTheCircuitAndLookupsAnswerNullMeanwhile()
        {
            var handler = new StubSvirfdHandler((_, _) => Json("", HttpStatusCode.TooManyRequests));

            await using var host = await StartAsync(handler);

            (await host.LookupAsync("8.8.8.8", CancellationToken.None)).Should().BeNull();
            handler.Calls.Should().Be(1);
            host.CircuitOpenUntil.Should().BeAfter(DateTime.UtcNow.AddMilliseconds(-100));

            (await host.LookupAsync("9.9.9.9", CancellationToken.None)).Should().BeNull();
            handler.Calls.Should().Be(1);
        }

        [Fact]
        public async Task RetryAfterFromTheServiceSetsTheExactPause()
        {
            var handler = new StubSvirfdHandler((_, _) =>
            {
                var response = new HttpResponseMessage(HttpStatusCode.TooManyRequests)
                {
                    Content = new StringContent("{\"error\":\"daily quota exhausted\"}")
                };
                response.Headers.TryAddWithoutValidation("Retry-After", "120");

                return Task.FromResult(response);
            });

            await using var host = await StartAsync(handler);

            (await host.LookupAsync("8.8.8.8", CancellationToken.None)).Should().BeNull();
            host.CircuitOpenUntil.Should().BeAfter(DateTime.UtcNow.AddSeconds(100));
            host.CircuitOpenUntil.Should().BeBefore(DateTime.UtcNow.AddSeconds(125));
        }

        [Fact]
        public async Task EveryRequestNamesJubeInTheUserAgent()
        {
            var json = SvirfdJson.Serialize(await SvirfdFixture.RecordAsync("37.30.56.54"));
            string? agent = null;
            var handler = new StubSvirfdHandler((request, _) =>
            {
                agent = request.Headers.UserAgent.ToString();

                return Json(json);
            });

            await using var host = await StartAsync(handler);
            await host.LookupAsync("8.8.8.8", CancellationToken.None);

            agent.Should().StartWith("Svirfd-dotnet/").And.EndWith("-Jube");
        }

        [Fact]
        public async Task AutoModeFallsBackToTheServiceWhenTheSnapshotIsRefusedAndKeepsRetrying()
        {
            var json = SvirfdJson.Serialize(await SvirfdFixture.RecordAsync("37.30.56.54"));
            var handler = new StubSvirfdHandler((request, _) =>
                request.RequestUri!.AbsolutePath == "/8.8.8.8"
                    ? Json(json)
                    : Json("{\"error\":\"slow down\"}", HttpStatusCode.TooManyRequests));

            await using var host = await StartAsync(handler, new SvirfdSettings
            {
                Mode = SvirfdMode.Auto,
                ApiKey = "test-key",
                BaseUrl = "https://svirfd.test",
                LocalRetryPause = TimeSpan.FromMinutes(5)
            });

            for (var i = 0; i < 100 && host.State != SvirfdState.FellBackToRemote; i++)
            {
                await Task.Delay(50);
            }

            host.State.Should().Be(SvirfdState.FellBackToRemote);
            host.Ready.Should().BeTrue();
            host.NextRetryAt.Should().NotBeNull();
            host.LastError.Should().NotBeEmpty();
            (await host.LookupAsync("8.8.8.8", CancellationToken.None)).Should().NotBeNull();
        }

        [Fact]
        public async Task RefusedKeyOpensTheCircuitUntilRestart()
        {
            var handler = new StubSvirfdHandler((_, _) => Json("{\"error\":\"API key not valid.\"}",
                HttpStatusCode.Unauthorized));

            await using var host = await StartAsync(handler);

            (await host.LookupAsync("8.8.8.8", CancellationToken.None)).Should().BeNull();
            host.CircuitOpenUntil.Should().Be(DateTime.MaxValue);
            (await host.LookupAsync("9.9.9.9", CancellationToken.None)).Should().BeNull();
            handler.Calls.Should().Be(1);
        }

        [Fact]
        public async Task IndexLoadingPausesBriefly()
        {
            var handler = new StubSvirfdHandler((_, _) => Json("{\"error\":\"index loading\"}",
                HttpStatusCode.ServiceUnavailable));

            await using var host = await StartAsync(handler);

            (await host.LookupAsync("8.8.8.8", CancellationToken.None)).Should().BeNull();
            host.CircuitOpenUntil.Should().BeBefore(DateTime.UtcNow.AddSeconds(6));
            host.CircuitOpenUntil.Should().BeAfter(DateTime.UtcNow.AddSeconds(3));
        }

        [Fact]
        public async Task SlowServiceIsCutOffByTheLookupTimeout()
        {
            var handler = new StubSvirfdHandler(async (_, token) =>
            {
                await Task.Delay(TimeSpan.FromSeconds(10), token);

                return new HttpResponseMessage(HttpStatusCode.OK);
            });

            await using var host = await StartAsync(handler, Remote(TimeSpan.FromMilliseconds(200)));

            var started = DateTime.UtcNow;
            (await host.LookupAsync("8.8.8.8", CancellationToken.None)).Should().BeNull();
            (DateTime.UtcNow - started).Should().BeLessThan(TimeSpan.FromSeconds(5));
            host.CircuitOpenUntil.Should().BeAfter(DateTime.UtcNow.AddSeconds(3));
        }

        [Fact]
        public async Task TimeoutOpensTheCircuitSoTheNextLookupDoesNotWaitAgain()
        {
            var handler = new StubSvirfdHandler(async (_, token) =>
            {
                await Task.Delay(TimeSpan.FromSeconds(10), token);

                return new HttpResponseMessage(HttpStatusCode.OK);
            });

            await using var host = await StartAsync(handler, Remote(TimeSpan.FromMilliseconds(200)));

            (await host.LookupAsync("8.8.8.8", CancellationToken.None)).Should().BeNull();

            var started = DateTime.UtcNow;
            (await host.LookupAsync("8.8.4.4", CancellationToken.None)).Should().BeNull();
            (DateTime.UtcNow - started).Should().BeLessThan(TimeSpan.FromMilliseconds(100));
            handler.Calls.Should().Be(1);
        }

        [Fact]
        public async Task NonAddressesNeverReachTheService()
        {
            var handler = new StubSvirfdHandler((_, _) => Json("{}"));

            await using var host = await StartAsync(handler);

            (await host.LookupAsync("not-an-address", CancellationToken.None)).Should().BeNull();
            (await host.LookupAsync(null, CancellationToken.None)).Should().BeNull();
            handler.Calls.Should().Be(0);
        }
    }
}
