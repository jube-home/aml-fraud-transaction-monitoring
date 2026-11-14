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
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Jube.App.Middlewares;
using Jube.Data.Concurrency;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace Jube.Test.Middlewares
{
    [Trait("Category", "Unit")]
    public sealed class VersionConflictMiddlewareTests
    {
        private static DefaultHttpContext Context()
        {
            return new DefaultHttpContext { Response = { Body = new MemoryStream() } };
        }

        [Fact]
        public async Task AVersionConflictBecomesA409WithTheTwoVersions()
        {
            var context = Context();
            var middleware = new VersionConflictMiddleware(_ => throw new VersionConflictException(2, 4));

            await middleware.InvokeAsync(context);

            context.Response.StatusCode.Should().Be(StatusCodes.Status409Conflict);
            context.Response.ContentType.Should().StartWith("application/json");

            context.Response.Body.Position = 0;

            using var body = await JsonDocument.ParseAsync(context.Response.Body);

            body.RootElement.GetProperty("code").GetString().Should().Be("VersionConflict");
            body.RootElement.GetProperty("suppliedVersion").GetInt32().Should().Be(2);
            body.RootElement.GetProperty("currentVersion").GetInt32().Should().Be(4);
        }

        [Fact]
        public async Task AnythingElseIsLeftToTheNextHandler()
        {
            var middleware = new VersionConflictMiddleware(_ => throw new InvalidOperationException("other"));

            var call = () => middleware.InvokeAsync(Context());

            await call.Should().ThrowAsync<InvalidOperationException>();
        }

        [Fact]
        public async Task ARequestThatDoesNotConflictPassesStraightThrough()
        {
            var context = Context();
            var middleware = new VersionConflictMiddleware(inner =>
            {
                inner.Response.StatusCode = StatusCodes.Status204NoContent;

                return Task.CompletedTask;
            });

            await middleware.InvokeAsync(context);

            context.Response.StatusCode.Should().Be(StatusCodes.Status204NoContent);
        }
    }
}
