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
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Jube.App.Wire;
using Jube.Dto.Query.WireSchema;
using MessagePack;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace Jube.Test.App.Wire
{
    [Trait("Category", "Unit")]
    public sealed class WireResponseFilterTests
    {
        private static readonly List<WireRouteDto> Rows = [new() { Method = "GET", Template = "a", Shape = "b" }];

        private static async Task<object> Run(object endpointResult, bool acceptMessagePack, HttpContext context = null)
        {
            context ??= new DefaultHttpContext();

            if (acceptMessagePack)
            {
                context.Request.Headers.Accept = WireMediaType.MessagePack;
            }

            var registry = new WireTypeRegistry();
            registry.Attach([]);

            var filter = new MessagePackResponseFilter(registry);

            return await filter.InvokeAsync(EndpointFilterInvocationContext.Create(context),
                _ => ValueTask.FromResult(endpointResult));
        }

        [Fact]
        public async Task WithoutTheMessagePackAcceptHeaderTheResultIsLeftAsJsonAsync()
        {
            var original = TypedResults.Ok(Rows);

            (await Run(original, false)).Should().BeSameAs(original);
        }

        [Fact]
        public async Task AnOkListIsWrittenAsMessagePackWithItsShapeAndSchemaHashAsync()
        {
            var context = new DefaultHttpContext();
            context.Response.Body = new MemoryStream();

            var result = await Run(TypedResults.Ok(Rows), true, context);

            result.Should().BeOfType<MessagePackHttpResult>();
            await ((IResult)result).ExecuteAsync(context);

            context.Response.ContentType.Should().Be(WireMediaType.MessagePack);
            context.Response.Headers[WireMediaType.ShapeHeader].ToString()
                .Should().Be(typeof(WireRouteDto).FullName + "[]");
            context.Response.Headers[WireMediaType.SchemaHeader].ToString().Should().HaveLength(16);
            context.Response.Headers.Vary.ToString().Should().Contain("Accept");

            var decoded = MessagePackSerializer.Deserialize<List<WireRouteDto>>(
                ((MemoryStream)context.Response.Body).ToArray(), WireSerializer.Options);
            decoded.Single().Template.Should().Be("a");
        }

        [Fact]
        public async Task ABarePocoReturnedWithoutAnIResultIsAlsoWrittenAsMessagePackAsync()
        {
            (await Run(Rows, true)).Should().BeOfType<MessagePackHttpResult>();
        }

        [Fact]
        public async Task ErrorStatusesStayJsonSoTheClientErrorMappingIsUnchangedAsync()
        {
            var original = TypedResults.BadRequest(Rows);

            (await Run(original, true)).Should().BeSameAs(original);
        }

        [Fact]
        public async Task ACreatedResultIsWrittenAsMessagePackWithItsStatusAsync()
        {
            var context = new DefaultHttpContext();
            context.Response.Body = new MemoryStream();

            var result = await Run(TypedResults.Created("/x", Rows[0]), true, context);

            await ((IResult)result).ExecuteAsync(context);

            context.Response.StatusCode.Should().Be(StatusCodes.Status201Created);
        }

        [Fact]
        public async Task ATypeThatCannotBeEncodedFallsBackToJsonAsync()
        {
            var original = TypedResults.Ok(new { Anonymous = 1 });

            (await Run(original, true)).Should().BeSameAs(original);
        }

        [Fact]
        public async Task PlainTextAndEmptyResultsAreNotTouchedAsync()
        {
            var text = TypedResults.Text("hello");
            var empty = TypedResults.NoContent();

            (await Run(text, true)).Should().BeSameAs(text);
            (await Run(empty, true)).Should().BeSameAs(empty);
            (await Run("raw", true)).Should().Be("raw");
        }
    }
}
