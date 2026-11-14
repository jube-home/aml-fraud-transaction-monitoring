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
using System.Net.Sockets;
using System.Threading.Tasks;
using FluentAssertions;
using Jube.ResilientHttp;
using Jube.ResilientHttp.Extensions;
using Polly.CircuitBreaker;
using Polly.Timeout;
using Xunit;

namespace Jube.Test.ResilientHttp
{
    [Trait("Category", "Unit")]
    public sealed class ResilientHttpExecutorTests
    {
        private static readonly HttpResilienceOptions TripsAfterTwo = new()
        {
            Timeout = TimeSpan.FromSeconds(5),
            FailureRatio = 0.5,
            MinimumThroughput = 2,
            SamplingDuration = TimeSpan.FromSeconds(30),
            BreakDuration = TimeSpan.FromMinutes(5)
        };

        private static Task<int> Fail(HttpStatusCode? status)
        {
            throw new HttpRequestException("failed", null, status);
        }

        [Fact]
        public async Task SuccessfulCallReturnsTheValueAsync()
        {
            var executor = new ResilientHttpExecutor(TimeProvider.System);

            var result = await executor.ExecuteAsync("a", TripsAfterTwo, _ => Task.FromResult(7));

            result.Should().Be(7);
        }

        [Fact]
        public async Task CircuitOpensAfterRepeatedTransientFailuresAndStopsInvokingTheActionAsync()
        {
            var executor = new ResilientHttpExecutor(TimeProvider.System);
            var calls = 0;

            for (var i = 0; i < 2; i++)
            {
                var act = async () => await executor.ExecuteAsync("a", TripsAfterTwo, _ =>
                {
                    calls++;
                    return Fail(HttpStatusCode.ServiceUnavailable);
                });

                await act.Should().ThrowAsync<HttpRequestException>();
            }

            var open = async () => await executor.ExecuteAsync("a", TripsAfterTwo, _ =>
            {
                calls++;
                return Task.FromResult(1);
            });

            var thrown = (await open.Should().ThrowAsync<BrokenCircuitException>()).Which;

            ResilientHttpExecutor.IsCircuitOpen(thrown).Should().BeTrue();
            calls.Should().Be(2);
        }

        [Fact]
        public async Task OneKeyOpeningTheCircuitDoesNotAffectAnotherKeyAsync()
        {
            var executor = new ResilientHttpExecutor(TimeProvider.System);

            for (var i = 0; i < 2; i++)
            {
                var act = async () =>
                    await executor.ExecuteAsync("tenant-1|host", TripsAfterTwo, _ => Fail(HttpStatusCode.BadGateway));

                await act.Should().ThrowAsync<HttpRequestException>();
            }

            var blocked = async () =>
                await executor.ExecuteAsync("tenant-1|host", TripsAfterTwo, _ => Task.FromResult(1));
            await blocked.Should().ThrowAsync<BrokenCircuitException>();

            var other = await executor.ExecuteAsync("tenant-2|host", TripsAfterTwo, _ => Task.FromResult(2));

            other.Should().Be(2);
        }

        [Fact]
        public async Task ClientErrorsDoNotOpenTheCircuitAsync()
        {
            var executor = new ResilientHttpExecutor(TimeProvider.System);

            for (var i = 0; i < 6; i++)
            {
                var act = async () =>
                    await executor.ExecuteAsync("a", TripsAfterTwo, _ => Fail(HttpStatusCode.NotFound));

                await act.Should().ThrowAsync<HttpRequestException>();
            }

            (await executor.ExecuteAsync("a", TripsAfterTwo, _ => Task.FromResult(3))).Should().Be(3);
        }

        [Fact]
        public async Task NoRetryIsMadeByDefaultAsync()
        {
            var executor = new ResilientHttpExecutor(TimeProvider.System);
            var calls = 0;
            var options = TripsAfterTwo with { MinimumThroughput = 100 };

            var act = async () => await executor.ExecuteAsync("a", options, _ =>
            {
                calls++;
                return Fail(HttpStatusCode.InternalServerError);
            });

            await act.Should().ThrowAsync<HttpRequestException>();
            calls.Should().Be(1);
        }

        [Fact]
        public async Task ConfiguredRetriesRetryOnlyTransientFailuresAsync()
        {
            var executor = new ResilientHttpExecutor(TimeProvider.System);
            var options = TripsAfterTwo with
            {
                MinimumThroughput = 100, MaxRetries = 2, RetryDelay = TimeSpan.FromMilliseconds(1)
            };
            var transient = 0;
            var permanent = 0;

            var first = async () => await executor.ExecuteAsync("a", options, _ =>
            {
                transient++;
                return Fail(HttpStatusCode.ServiceUnavailable);
            });
            await first.Should().ThrowAsync<HttpRequestException>();

            var second = async () => await executor.ExecuteAsync("b", options, _ =>
            {
                permanent++;
                return Fail(HttpStatusCode.BadRequest);
            });
            await second.Should().ThrowAsync<HttpRequestException>();

            transient.Should().Be(3);
            permanent.Should().Be(1);
        }

        [Fact]
        public async Task CallExceedingTheTimeoutIsRejectedAsync()
        {
            var executor = new ResilientHttpExecutor(TimeProvider.System);
            var options = TripsAfterTwo with { Timeout = TimeSpan.FromMilliseconds(100), MinimumThroughput = 100 };

            var act = async () => await executor.ExecuteAsync("a", options, async ct =>
            {
                await Task.Delay(TimeSpan.FromSeconds(10), ct);
                return 1;
            });

            await act.Should().ThrowAsync<TimeoutRejectedException>();
        }

        [Fact]
        public async Task SamePipelineIsReusedForTheSameKeyAndOptionsAsync()
        {
            var executor = new ResilientHttpExecutor(TimeProvider.System);

            await executor.ExecuteAsync("a", TripsAfterTwo, _ => Task.FromResult(1));
            await executor.ExecuteAsync("a", TripsAfterTwo, _ => Task.FromResult(1));
            await executor.ExecuteAsync("b", TripsAfterTwo, _ => Task.FromResult(1));

            executor.PipelineCount.Should().Be(2);
        }

        [Theory]
        [InlineData(HttpStatusCode.InternalServerError, true)]
        [InlineData(HttpStatusCode.ServiceUnavailable, true)]
        [InlineData(HttpStatusCode.RequestTimeout, true)]
        [InlineData(HttpStatusCode.TooManyRequests, true)]
        [InlineData(HttpStatusCode.BadRequest, false)]
        [InlineData(HttpStatusCode.NotFound, false)]
        [InlineData(HttpStatusCode.Found, false)]
        public void StatusClassification(HttpStatusCode status, bool expected)
        {
            HttpFailureClassification.IsTransient(new HttpRequestException("x", null, status)).Should()
                .Be(expected);
        }

        [Fact]
        public void NetworkLevelFailuresAreTransientAndCallerCancellationIsNot()
        {
            HttpFailureClassification.IsTransient(new HttpRequestException("x")).Should().BeTrue();
            HttpFailureClassification.IsTransient(new SocketException()).Should().BeTrue();
            HttpFailureClassification.IsTransient(new IOException()).Should().BeTrue();
            HttpFailureClassification.IsTransient(new TaskCanceledException("t", new TimeoutException())).Should()
                .BeTrue();
            HttpFailureClassification.IsTransient(new OperationCanceledException()).Should().BeFalse();
            HttpFailureClassification.IsTransient(new InvalidOperationException()).Should().BeFalse();
        }

        [Fact]
        public void ThrowIfUnsuccessfulCarriesTheStatusCode()
        {
            using var ok = new HttpResponseMessage(HttpStatusCode.OK);
            using var bad = new HttpResponseMessage(HttpStatusCode.BadGateway);

            ok.ThrowIfUnsuccessful();

            var act = () => bad.ThrowIfUnsuccessful();

            act.Should().Throw<HttpRequestException>().Which.StatusCode.Should().Be(HttpStatusCode.BadGateway);
        }

        [Fact]
        public void DefaultsAreConfigurableAndDocumented()
        {
            var original = HttpResilienceDefaults.Shared;

            try
            {
                original.MaxRetries.Should().Be(0);
                original.MinimumThroughput.Should().Be(5);

                HttpResilienceDefaults.ConfigureShared(original with { MinimumThroughput = 9 });

                HttpResilienceDefaults.Shared.MinimumThroughput.Should().Be(9);
            }
            finally
            {
                HttpResilienceDefaults.ConfigureShared(original);
            }
        }
    }
}
