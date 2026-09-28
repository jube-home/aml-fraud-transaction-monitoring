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

using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Jube.App.Middlewares;
using Jube.Test.Infrastructure;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace Jube.Test.Middlewares
{
    [Trait("Category", "Unit")]
    [Collection("JubeCache")]
    public sealed class GlobalHttpRateLimitMiddlewareTests
    {
        private static Jube.DynamicEnvironment.DynamicEnvironment Environment(bool enabled, int attempts = 3,
            bool backplane = false)
        {
            return TestDynamicEnvironment.Create(new Dictionary<string, string>
            {
                ["EnableGlobalHttpRateLimit"] = enabled ? "True" : "False",
                ["GlobalHttpRateLimitAttempts"] = attempts.ToString(),
                ["GlobalHttpRateLimitInterval"] = "s",
                ["GlobalHttpRateLimitIntervalValue"] = "60",
                ["RateLimitBackplane"] = backplane ? "True" : "False"
            });
        }

        private static DefaultHttpContext Context(string remoteIp)
        {
            var context = new DefaultHttpContext();
            context.Connection.RemoteIpAddress = System.Net.IPAddress.Parse(remoteIp);
            return context;
        }

        [Fact]
        public async Task WhenDisabledEveryRequestReachesTheEndpointAsync()
        {
            var reached = 0;
            var middleware = new GlobalHttpRateLimitMiddleware(_ =>
            {
                reached++;
                return Task.CompletedTask;
            }, new GlobalHttpRequestSourceIpThrottle(), TestCacheService.Create(out _), Environment(false));

            for (var i = 0; i < 10; i++)
            {
                await middleware.InvokeAsync(Context("203.0.113.30"));
            }

            reached.Should().Be(10);
        }

        [Fact]
        public async Task WhenEnabledRequestsBeyondTheThresholdGet429AndDoNotReachTheEndpointAsync()
        {
            var reached = 0;
            var middleware = new GlobalHttpRateLimitMiddleware(_ =>
            {
                reached++;
                return Task.CompletedTask;
            }, new GlobalHttpRequestSourceIpThrottle(), TestCacheService.Create(out _), Environment(true));

            for (var i = 0; i < 3; i++)
            {
                var context = Context("203.0.113.31");
                await middleware.InvokeAsync(context);
                context.Response.StatusCode.Should().NotBe(StatusCodes.Status429TooManyRequests);
            }

            var fourth = Context("203.0.113.31");
            await middleware.InvokeAsync(fourth);

            fourth.Response.StatusCode.Should().Be(StatusCodes.Status429TooManyRequests);
            reached.Should().Be(3);
        }

        [Fact]
        public async Task DifferentSourceIpsHaveIndependentBudgetsAsync()
        {
            var middleware = new GlobalHttpRateLimitMiddleware(_ => Task.CompletedTask,
                new GlobalHttpRequestSourceIpThrottle(), TestCacheService.Create(out _), Environment(true, 2));

            await middleware.InvokeAsync(Context("203.0.113.32"));
            await middleware.InvokeAsync(Context("203.0.113.32"));

            var otherIp = Context("203.0.113.33");
            await middleware.InvokeAsync(otherIp);

            otherIp.Response.StatusCode.Should().NotBe(StatusCodes.Status429TooManyRequests);
        }

        [Fact]
        public async Task WhenBackplaneIsEnabledTwoMiddlewareInstancesShareTheCountThroughTheSameCacheServiceAsync()
        {
            var sharedCacheService = TestCacheService.Create(out _);
            var environment = Environment(true, 2, backplane: true);

            var nodeA = new GlobalHttpRateLimitMiddleware(_ => Task.CompletedTask,
                new GlobalHttpRequestSourceIpThrottle(), sharedCacheService, environment);
            var nodeB = new GlobalHttpRateLimitMiddleware(_ => Task.CompletedTask,
                new GlobalHttpRequestSourceIpThrottle(), sharedCacheService, environment);

            await nodeA.InvokeAsync(Context("203.0.113.34"));
            await nodeB.InvokeAsync(Context("203.0.113.34"));

            var thirdOnEitherNode = Context("203.0.113.34");
            await nodeA.InvokeAsync(thirdOnEitherNode);

            thirdOnEitherNode.Response.StatusCode.Should().Be(StatusCodes.Status429TooManyRequests,
                "with the backplane on, two nodes sharing one Redis-backed counter must see each other's " +
                "requests, unlike the in-memory throttle which is local to a single process");
        }

        [Fact]
        public async Task WithoutTheBackplaneTwoMiddlewareInstancesDoNotShareTheCountAsync()
        {
            var cacheService = TestCacheService.Create(out _);
            var environment = Environment(true, 2, backplane: false);

            var nodeA = new GlobalHttpRateLimitMiddleware(_ => Task.CompletedTask,
                new GlobalHttpRequestSourceIpThrottle(), cacheService, environment);
            var nodeB = new GlobalHttpRateLimitMiddleware(_ => Task.CompletedTask,
                new GlobalHttpRequestSourceIpThrottle(), cacheService, environment);

            await nodeA.InvokeAsync(Context("203.0.113.35"));
            await nodeA.InvokeAsync(Context("203.0.113.35"));

            var firstOnNodeB = Context("203.0.113.35");
            await nodeB.InvokeAsync(firstOnNodeB);

            firstOnNodeB.Response.StatusCode.Should().NotBe(StatusCodes.Status429TooManyRequests,
                "each in-memory throttle instance is local to its own node when the backplane is off");
        }

        [Fact]
        public async Task ANonPositiveThresholdNeverRateLimitsAsync()
        {
            var middleware = new GlobalHttpRateLimitMiddleware(_ => Task.CompletedTask,
                new GlobalHttpRequestSourceIpThrottle(), TestCacheService.Create(out _), Environment(true, 0));

            var context = Context("203.0.113.36");
            for (var i = 0; i < 10; i++)
            {
                context = Context("203.0.113.36");
                await middleware.InvokeAsync(context);
            }

            context.Response.StatusCode.Should().NotBe(StatusCodes.Status429TooManyRequests);
        }
    }
}