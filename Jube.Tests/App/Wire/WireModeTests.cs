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
using System.Threading.Tasks;
using FluentAssertions;
using Jube.App.Wire;
using Jube.Dto.Query.CaseKeyJournal;
using Jube.Dto.Query.WireSchema;
using MessagePack;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;
using Xunit;

namespace Jube.Test.App.Wire
{
    [Trait("Category", "Unit")]
    public sealed class WireModeTests
    {
        private static readonly List<WireRouteDto> Plain = [new() { Method = "GET", Template = "a", Shape = "b" }];

        private static WireTypeRegistry Registry(WireMode mode)
        {
            var registry = new WireTypeRegistry(new WireOptions(mode));
            registry.Attach([]);

            return registry;
        }

        private static async Task<object> Respond(WireMode mode, object endpointResult)
        {
            var context = new DefaultHttpContext();
            context.Request.Headers.Accept = WireMediaType.MessagePack;

            var filter = new MessagePackResponseFilter(Registry(mode));

            return await filter.InvokeAsync(EndpointFilterInvocationContext.Create(context),
                _ => ValueTask.FromResult(endpointResult));
        }

        private static async Task<(DefaultHttpContext Context, bool Reached)> Send(WireMode mode)
        {
            var body = MessagePackSerializer.Serialize(Plain[0], WireSerializer.Options);
            var context = new DefaultHttpContext();
            context.Request.Method = "POST";
            context.Request.Body = new MemoryStream(body);
            context.Request.ContentLength = body.Length;
            context.Request.ContentType = WireMediaType.MessagePack;
            context.SetEndpoint(new Endpoint(_ => Task.CompletedTask,
                new EndpointMetadataCollection(new AcceptsMetadata(["application/json"], typeof(WireRouteDto))),
                "test"));

            var reached = false;
            var middleware = new MessagePackRequestMiddleware(_ =>
            {
                reached = true;

                return Task.CompletedTask;
            }, Registry(mode), Options.Create(new JsonOptions()));

            await middleware.InvokeAsync(context);

            return (context, reached);
        }

        [Fact]
        public async Task JsonModeNeverWritesMessagePackEvenWhenAskedAsync()
        {
            var rows = TypedResults.Ok(new CaseKeyJournalDto());

            (await Respond(WireMode.Json, rows)).Should().BeSameAs(rows);
            (await Respond(WireMode.Json, TypedResults.Ok(Plain))).Should().NotBeOfType<MessagePackHttpResult>();
        }

        [Fact]
        public async Task MixedModeWritesMessagePackOnlyForResponsesThatCarryRowGridsAsync()
        {
            (await Respond(WireMode.Mixed, TypedResults.Ok(new CaseKeyJournalDto())))
                .Should().BeOfType<MessagePackHttpResult>();
            (await Respond(WireMode.Mixed, TypedResults.Ok(Plain))).Should().NotBeOfType<MessagePackHttpResult>();
        }

        [Fact]
        public async Task MessagePackModeWritesMessagePackForEveryEncodableResponseAsync()
        {
            (await Respond(WireMode.MessagePack, TypedResults.Ok(new CaseKeyJournalDto())))
                .Should().BeOfType<MessagePackHttpResult>();
            (await Respond(WireMode.MessagePack, TypedResults.Ok(Plain))).Should().BeOfType<MessagePackHttpResult>();
        }

        [Theory]
        [InlineData(WireMode.Json)]
        [InlineData(WireMode.Mixed)]
        public async Task ARequestBodyInMessagePackIsRejectedBeforeItIsParsedUnlessTheModeIsMessagePackAsync(
            WireMode mode)
        {
            var (context, reached) = await Send(mode);

            context.Response.StatusCode.Should().Be(StatusCodes.Status415UnsupportedMediaType);
            reached.Should().BeFalse();
        }

        [Fact]
        public async Task MessagePackModeAcceptsAMessagePackRequestBodyAsync()
        {
            var (context, reached) = await Send(WireMode.MessagePack);

            context.Response.StatusCode.Should().Be(StatusCodes.Status200OK);
            reached.Should().BeTrue();
        }

        [Fact]
        public void TheSchemaNamesTheModeAndTheHashChangesWithItSoTheClientRefetchesAfterARestart()
        {
            var json = Registry(WireMode.Json).GetSchema();
            var mixed = Registry(WireMode.Mixed).GetSchema();

            json.Mode.Should().Be("Json");
            mixed.Mode.Should().Be("Mixed");
            json.Hash.Should().NotBe(mixed.Hash);
        }

        [Fact]
        public void ARegistryBuiltWithoutOptionsDefaultsToMessagePack()
        {
            new WireTypeRegistry().Mode.Should().Be(WireMode.MessagePack);
        }

        [Fact]
        public void OnlyTypesWhoseGraphContainsRowGridsCarryRows()
        {
            var registry = Registry(WireMode.Mixed);

            registry.CarriesRows(registry.ShapeOf(typeof(CaseKeyJournalDto))).Should().BeTrue();
            registry.CarriesRows(registry.ShapeOf(typeof(List<CaseKeyJournalDto>))).Should().BeTrue();
            registry.CarriesRows(registry.ShapeOf(typeof(WireRouteDto))).Should().BeFalse();
        }

        [Theory]
        [InlineData("Json", true, WireMode.Json)]
        [InlineData("mixed", true, WireMode.Mixed)]
        [InlineData("MESSAGEPACK", true, WireMode.MessagePack)]
        [InlineData("Protobuf", false, WireMode.Json)]
        [InlineData("7", false, WireMode.Json)]
        [InlineData("1", false, WireMode.Json)]
        [InlineData("", false, WireMode.Json)]
        public void OnlyTheThreeNamedModesParse(string value, bool valid, WireMode expected)
        {
            WireOptions.TryParse(value, out var mode).Should().Be(valid);

            if (valid)
            {
                mode.Should().Be(expected);
            }
        }
    }
}
