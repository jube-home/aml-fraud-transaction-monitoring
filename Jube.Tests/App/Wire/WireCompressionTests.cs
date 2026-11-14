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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Jube.App.Wire;
using Jube.Dto.Query.WireSchema;
using MessagePack;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Xunit;

namespace Jube.Test.App.Wire
{
    [Trait("Category", "Unit")]
    public sealed class WireCompressionTests
    {
        private const string CompressedByTheServer =
            "C8012363D20000053FF91ADC002893A3474554AC6170692F456E746974792F30AE4A7562652E44746F2E5468696E6793A4504F5322001C3122000C43001C3221000D43001C3322000C43001C3421000D43000DA8000C43000DA7000D43000DA8000C43000DA7000D43000DA8000C43000DA7000D43000DA8000C43000DA7000D43000DA8000C43000DA7000D43000DA8000C43000DA7000D43000DA8000C43000DA7000D43000DA8000C43000DA7000D43000DA8000C43000DA7000D43000DA8000C43000DA7000D43000DA8000C43000DA7000D43000DA8000C43000DA7000D43000DA8000C43000DA7000D43000DA8000C43000DA7000D43000DA8000C43000DA7000D43000DA8000C43000DA7000D43000DA8000C43000DA7000D430007A800505468696E67";

        private static List<WireRouteDto> Rows(int count)
        {
            return Enumerable.Range(0, count).Select(i => new WireRouteDto
            {
                Method = i % 2 == 0 ? "GET" : "POST",
                Template = "api/Entity/" + i % 5,
                Shape = "Jube.Dto.Thing"
            }).ToList();
        }

        private static async Task<(object Result, DefaultHttpContext Context)> Respond(WireCompression compression,
            object endpointResult)
        {
            var registry = new WireTypeRegistry(new WireOptions(WireMode.MessagePack, compression: compression));
            registry.Attach([]);

            var context = new DefaultHttpContext();
            context.Request.Headers.Accept = WireMediaType.MessagePack;
            context.Response.Body = new MemoryStream();

            var result = await new MessagePackResponseFilter(registry).InvokeAsync(
                EndpointFilterInvocationContext.Create(context), _ => ValueTask.FromResult(endpointResult));

            await ((IResult)result).ExecuteAsync(context);

            return (result, context);
        }

        [Fact]
        public async Task ALargeResponseIsCompressedAndNamesItsCompressionAndDecodesBackToTheSameRowsAsync()
        {
            var rows = Rows(200);

            var (_, context) = await Respond(WireCompression.Lz4, TypedResults.Ok(rows));
            var body = ((MemoryStream)context.Response.Body).ToArray();

            context.Response.Headers[WireMediaType.CompressionHeader].ToString().Should().Be("lz4");
            WireSerializer.IsLz4Block(body).Should().BeTrue();
            body.Length.Should().BeLessThan(MessagePackSerializer.Serialize(rows, WireSerializer.Options).Length / 2);

            var decoded = MessagePackSerializer.Deserialize<List<WireRouteDto>>(body, WireSerializer.Lz4Options);
            decoded.Should().BeEquivalentTo(rows, options => options.WithStrictOrdering());
        }

        [Fact]
        public async Task AResponseBelowTheThresholdIsLeftUncompressedAndNamesNoCompressionAsync()
        {
            var (_, context) = await Respond(WireCompression.Lz4, TypedResults.Ok(Rows(2)));
            var body = ((MemoryStream)context.Response.Body).ToArray();

            context.Response.Headers.ContainsKey(WireMediaType.CompressionHeader).Should().BeFalse();
            WireSerializer.IsLz4Block(body).Should().BeFalse();
            MessagePackSerializer.Deserialize<List<WireRouteDto>>(body, WireSerializer.Options).Should().HaveCount(2);
        }

        [Fact]
        public async Task NoneLeavesEveryResponseUncompressedWhateverItsSizeAsync()
        {
            var (_, context) = await Respond(WireCompression.None, TypedResults.Ok(Rows(200)));

            context.Response.Headers.ContainsKey(WireMediaType.CompressionHeader).Should().BeFalse();
            WireSerializer.IsLz4Block(((MemoryStream)context.Response.Body).ToArray()).Should().BeFalse();
        }

        [Fact]
        public void TheBytesTheServerWroteAreTheBytesTheBrowserTestsDecode()
        {
            var body = Convert.FromHexString(CompressedByTheServer);

            WireSerializer.IsLz4Block(body).Should().BeTrue();

            var rows = MessagePackSerializer.Deserialize<List<WireRouteDto>>(body, WireSerializer.Lz4Options);

            rows.Should().HaveCount(40);
            rows[0].Template.Should().Be("api/Entity/0");
            rows[39].Method.Should().Be("POST");
        }

        [Fact]
        public async Task ACompressedRequestBodyIsRefusedSoNoDecompressionIsEverDoneOnInputAsync()
        {
            var compressed = MessagePackSerializer.Serialize(Rows(200)[0], WireSerializer.Lz4Options);
            var oversized = MessagePackSerializer.Serialize(Rows(200), WireSerializer.Lz4Options);

            foreach (var body in new[] { compressed, oversized })
            {
                var reached = false;
                var context = new DefaultHttpContext();
                context.Request.Method = "POST";
                context.Request.Body = new MemoryStream(body);
                context.Request.ContentLength = body.Length;
                context.Request.ContentType = WireMediaType.MessagePack;
                context.SetEndpoint(new Endpoint(_ => Task.CompletedTask,
                    new EndpointMetadataCollection(new Microsoft.AspNetCore.Http.Metadata.AcceptsMetadata(
                        ["application/json"], typeof(List<WireRouteDto>))), "test"));

                var registry = new WireTypeRegistry();
                registry.Attach([]);

                await new MessagePackRequestMiddleware(_ =>
                {
                    reached = true;

                    return Task.CompletedTask;
                }, registry, Options.Create(new Microsoft.AspNetCore.Http.Json.JsonOptions())).InvokeAsync(context);

                reached.Should().BeFalse();
                context.Response.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
            }
        }

        [Theory]
        [InlineData("None", true, WireCompression.None)]
        [InlineData("lz4", true, WireCompression.Lz4)]
        [InlineData("LZ4", true, WireCompression.Lz4)]
        [InlineData("Zstd", false, WireCompression.None)]
        [InlineData("1", false, WireCompression.None)]
        [InlineData("", false, WireCompression.None)]
        public void OnlyTheTwoNamedCompressionsParse(string value, bool valid, WireCompression expected)
        {
            WireOptions.TryParseCompression(value, out var compression).Should().Be(valid);

            if (valid)
            {
                compression.Should().Be(expected);
            }
        }
    }
}
