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
using System.IO;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Jube.App.Code.Waf;
using Jube.App.Middlewares;
using Jube.Test.Infrastructure;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace Jube.Test.Waf
{
    [Trait("Category", "Unit")]
    public sealed class WafMiddlewareTests
    {
        private static Jube.DynamicEnvironment.DynamicEnvironment Environment(bool enabled)
        {
            return TestDynamicEnvironment.Create(new Dictionary<string, string>
            {
                ["WafEnabled"] = enabled ? "True" : "False"
            });
        }

        private static DefaultHttpContext Context(string method, string path, string? contentType, string? body,
            string? queryValue = null)
        {
            var context = new DefaultHttpContext();
            context.Request.Method = method;
            context.Request.Path = path;
            if (queryValue != null)
            {
                context.Request.QueryString = new QueryString("?q=" + queryValue);
            }

            if (body != null)
            {
                var bytes = Encoding.UTF8.GetBytes(body);
                context.Request.Body = new MemoryStream(bytes);
                context.Request.ContentLength = bytes.Length;
                context.Request.ContentType = contentType;
            }

            context.Response.Body = new MemoryStream();
            return context;
        }

        private static WafMiddleware Middleware(RequestDelegate next, bool enabled)
        {
            return new WafMiddleware(next, new WafInspector(WafTestRuleSets.SeededRegistry()), Environment(enabled),
                TestLog.NoOp);
        }

        [Fact]
        public async Task AJsonBodyAttackIsBlockedWithForbiddenAndDoesNotReachTheEndpointAsync()
        {
            var reached = false;
            var middleware = Middleware(_ =>
            {
                reached = true;
                return Task.CompletedTask;
            }, true);
            var context = Context("POST", "/api/Test", "application/json", "{\"name\":\"<script>alert(1)</script>\"}");

            await middleware.InvokeAsync(context);

            reached.Should().BeFalse();
            context.Response.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
        }

        [Fact]
        public async Task ABenignJsonBodyReachesTheEndpointWithTheBodyStillReadableAsync()
        {
            const string body = "{\"narrative\":\"an ordinary payment to ACME\"}";
            string? downstreamBody = null;
            var middleware = Middleware(async ctx =>
            {
                using var reader = new StreamReader(ctx.Request.Body);
                downstreamBody = await reader.ReadToEndAsync();
            }, true);
            var context = Context("POST", "/api/Test", "application/json", body);

            await middleware.InvokeAsync(context);

            context.Response.StatusCode.Should().Be(StatusCodes.Status200OK);
            downstreamBody.Should().Be(body);
        }

        [Fact]
        public async Task WhenDisabledAnAttackPassesThroughAsync()
        {
            var reached = false;
            var middleware = Middleware(_ =>
            {
                reached = true;
                return Task.CompletedTask;
            }, false);
            var context = Context("POST", "/api/Test", "application/json", "{\"name\":\"<script>alert(1)</script>\"}");

            await middleware.InvokeAsync(context);

            reached.Should().BeTrue();
            context.Response.StatusCode.Should().Be(StatusCodes.Status200OK);
        }

        [Fact]
        public async Task AQueryStringAttackIsBlockedAsync()
        {
            var reached = false;
            var middleware = Middleware(_ =>
            {
                reached = true;
                return Task.CompletedTask;
            }, true);
            var context = Context("GET", "/api/Test", null, null, "1%20UNION%20SELECT%20x");

            await middleware.InvokeAsync(context);

            reached.Should().BeFalse();
            context.Response.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
        }

        [Fact]
        public async Task APathTraversalAttackIsBlockedAsync()
        {
            var reached = false;
            var middleware = Middleware(_ =>
            {
                reached = true;
                return Task.CompletedTask;
            }, true);
            var context = Context("GET", "/api/Test/../../etc/passwd", null, null);

            await middleware.InvokeAsync(context);

            reached.Should().BeFalse();
            context.Response.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
        }
    }
}