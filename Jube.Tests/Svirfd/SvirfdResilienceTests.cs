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
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Jube.ResilientHttp;
using Jube.Svirfd;
using Microsoft.Extensions.Logging.Abstractions;
using Svirfd;
using Xunit;

namespace Jube.Test.Svirfd
{
    [Trait("Category", "Unit")]
    public sealed class SvirfdResilienceTests
    {
        private static SvirfdSettings Remote(TimeSpan? timeout = null)
        {
            return new SvirfdSettings
            {
                Mode = SvirfdMode.Remote, BaseUrl = "https://svirfd.test",
                RemoteLookupTimeout = timeout ?? TimeSpan.FromSeconds(5)
            };
        }

        private static async Task<SvirfdHost> StartAsync(StubSvirfdHandler handler, SvirfdSettings settings)
        {
            var host = new SvirfdHost(settings, new HttpClient(handler), NullLogger.Instance);
            await host.StartAsync(CancellationToken.None);

            return host;
        }

        [Fact]
        public void TheLookupPipelineUsesTheSharedDefaultsWithTheConfiguredTimeoutAndNoRetry()
        {
            var shared = HttpResilienceDefaults.Shared;
            var options = Remote(TimeSpan.FromSeconds(3)).ToResilienceOptions();

            options.Timeout.Should().Be(TimeSpan.FromSeconds(3));
            options.MaxRetries.Should().Be(0);
            options.FailureRatio.Should().Be(shared.FailureRatio);
            options.MinimumThroughput.Should().Be(shared.MinimumThroughput);
            options.BreakDuration.Should().Be(shared.BreakDuration);
            options.SamplingDuration.Should().Be(shared.SamplingDuration);
        }

        [Fact]
        public void ATimeoutBelowTheFloorIsRaisedSoThePipelineCanBeBuilt()
        {
            Remote(TimeSpan.FromMilliseconds(1)).ToResilienceOptions().Timeout.Should()
                .Be(SvirfdSettings.MinimumRemoteLookupTimeout);
        }

        [Fact]
        public async Task AServerFailureIsNeverRetriedBecauseTheCallerIsInsideATransactionAsync()
        {
            var handler = new StubSvirfdHandler((_, _) =>
                Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError)));

            await using var host = await StartAsync(handler, Remote());

            (await host.LookupAsync("8.8.8.8", CancellationToken.None)).Should().BeNull();

            handler.Calls.Should().Be(1);
            host.CircuitOpenUntil.Should().BeAfter(DateTime.UtcNow.AddSeconds(3));
            host.CircuitOpenUntil.Should().BeBefore(DateTime.UtcNow.AddSeconds(6));
        }

        [Fact]
        public async Task RepeatedNetworkFailuresStopReachingTheServiceAsync()
        {
            var handler = new StubSvirfdHandler((_, _) => throw new IOException("connection reset"));

            await using var host = await StartAsync(handler, Remote());

            for (var i = 1; i <= 12; i++)
            {
                (await host.LookupAsync($"8.8.8.{i}", CancellationToken.None)).Should().BeNull();
            }

            handler.Calls.Should().BeLessThanOrEqualTo(HttpResilienceDefaults.Shared.MinimumThroughput);
            host.CircuitOpenUntil.Should().BeAfter(DateTime.UtcNow);
        }

        [Fact]
        public async Task CallerCancellationIsNotAFailureAndDoesNotOpenAnyCircuitAsync()
        {
            var handler = new StubSvirfdHandler(async (_, token) =>
            {
                await Task.Delay(TimeSpan.FromSeconds(10), token);

                return new HttpResponseMessage(HttpStatusCode.OK);
            });

            await using var host = await StartAsync(handler, Remote());
            using var cancelled = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));

            await FluentActions.Awaiting(() => host.LookupAsync("8.8.8.8", cancelled.Token))
                .Should().ThrowAsync<OperationCanceledException>();

            host.CircuitOpenUntil.Should().BeBefore(DateTime.UtcNow.AddSeconds(1));
        }

        [Fact]
        public async Task EachHostKeepsItsOwnBreakerSoOneInstallationNeverTripsAnotherAsync()
        {
            var failing = new StubSvirfdHandler((_, _) => throw new IOException("down"));
            var healthy = new StubSvirfdHandler((_, _) =>
                Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound)));

            await using var first = await StartAsync(failing, Remote());
            await using var second = await StartAsync(healthy, Remote());

            for (var i = 1; i <= 8; i++)
            {
                await first.LookupAsync($"8.8.8.{i}", CancellationToken.None);
            }

            await second.LookupAsync("9.9.9.9", CancellationToken.None);

            healthy.Calls.Should().Be(1);
        }

        [Fact]
        public async Task TheLookupRunsOnTheSuppliedExecutorSoProductionSharesTheProcessWideBreakersAsync()
        {
            var executor = new ResilientHttpExecutor(TimeProvider.System);

            var handler = new StubSvirfdHandler((_, _) =>
                Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound)));

            await using var host = new SvirfdHost(Remote(), new HttpClient(handler), NullLogger.Instance, executor);
            await host.StartAsync(CancellationToken.None);

            executor.PipelineCount.Should().Be(0);

            await host.LookupAsync("9.9.9.9", CancellationToken.None);

            executor.PipelineCount.Should().Be(1);
        }
    }
}
