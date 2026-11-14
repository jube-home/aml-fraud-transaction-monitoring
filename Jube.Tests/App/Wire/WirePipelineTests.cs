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
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Jube.App.Code.Waf;
using Jube.App.Middlewares;
using Jube.App.Wire;
using Jube.Dto.Query.WireSchema;
using Jube.Test.Infrastructure;
using Jube.Test.Waf;
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
    public sealed class WirePipelineTests
    {
        private static readonly JsonSerializerOptions Web = new(JsonSerializerDefaults.Web);

        private static DefaultHttpContext Context(byte[] body, string contentType, System.Type requestType)
        {
            var context = new DefaultHttpContext();
            context.Request.Method = "POST";
            context.Request.Path = "/api/Test";
            context.Request.Body = new MemoryStream(body);
            context.Request.ContentLength = body.Length;
            context.Request.ContentType = contentType;
            context.Response.Body = new MemoryStream();

            var metadata = requestType == null
                ? new EndpointMetadataCollection()
                : new EndpointMetadataCollection(new AcceptsMetadata(["application/json"], requestType));

            context.SetEndpoint(new Endpoint(_ => Task.CompletedTask, metadata, "test"));

            return context;
        }

        private static MessagePackRequestMiddleware Transcoder(RequestDelegate next)
        {
            return new MessagePackRequestMiddleware(next, new WireTypeRegistry(),
                Options.Create(new JsonOptions()));
        }

        private static RequestDelegate Waf(RequestDelegate next)
        {
            var environment = TestDynamicEnvironment.Create(new Dictionary<string, string>
            {
                ["WafEnabled"] = "True",
                ["WafMaxInspectionMilliseconds"] = "10000"
            });

            return new WafMiddleware(next, new WafInspector(WafTestRuleSets.SeededRegistry(), environment),
                environment, TestLog.NoOp).InvokeAsync;
        }

        private static byte[] Pack<T>(T value)
        {
            return MessagePackSerializer.Serialize(value, WireSerializer.Options);
        }

        private static WireRouteDto Route(string template)
        {
            return new WireRouteDto { Method = "GET", Template = template, Shape = "x" };
        }

        [Fact]
        public async Task ABenignMessagePackBodyReachesTheEndpointAsTheEquivalentJsonAsync()
        {
            string downstream = null;
            string contentType = null;

            var pipeline = Transcoder(Waf(async ctx =>
            {
                contentType = ctx.Request.ContentType;
                using var reader = new StreamReader(ctx.Request.Body);
                downstream = await reader.ReadToEndAsync();
            }));

            var context = Context(Pack(Route("api/Ordinary")), WireMediaType.MessagePack, typeof(WireRouteDto));

            await pipeline.InvokeAsync(context);

            context.Response.StatusCode.Should().Be(StatusCodes.Status200OK);
            contentType.Should().Be("application/json");
            JsonSerializer.Deserialize<WireRouteDto>(downstream, Web)!.Template.Should().Be("api/Ordinary");
        }

        [Theory]
        [InlineData("<script>alert(1)</script>")]
        [InlineData("1 UNION SELECT username, password FROM users--")]
        [InlineData("../../../etc/passwd")]
        public async Task AnAttackCarriedInAMessagePackBodyIsStillBlockedByTheWafAsync(string attack)
        {
            var reached = false;

            var pipeline = Transcoder(Waf(_ =>
            {
                reached = true;

                return Task.CompletedTask;
            }));

            var context = Context(Pack(Route(attack)), WireMediaType.MessagePack, typeof(WireRouteDto));

            await pipeline.InvokeAsync(context);

            reached.Should().BeFalse();
            context.Response.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
        }

        [Fact]
        public async Task TheSameAttackInJsonAndInMessagePackGetsTheSameVerdictAsync()
        {
            var attack = "<script>alert(1)</script>";
            var json = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(Route(attack), Web));

            var viaJson = Context(json, "application/json", typeof(WireRouteDto));
            var viaPack = Context(Pack(Route(attack)), WireMediaType.MessagePack, typeof(WireRouteDto));

            var pipeline = Transcoder(Waf(_ => Task.CompletedTask));

            await pipeline.InvokeAsync(viaJson);
            await pipeline.InvokeAsync(viaPack);

            viaPack.Response.StatusCode.Should().Be(viaJson.Response.StatusCode);
        }

        [Fact]
        public async Task ANilBodyIsRejectedLikeTheJsonNullLiteralAsync()
        {
            var reached = false;
            var context = Context(Pack<WireRouteDto>(null), WireMediaType.MessagePack, typeof(WireRouteDto));

            await Transcoder(_ =>
            {
                reached = true;

                return Task.CompletedTask;
            }).InvokeAsync(context);

            reached.Should().BeFalse();
            context.Response.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        }

        [Theory]
        [InlineData(new byte[] { 0x93, 0x01 })]
        [InlineData(new byte[] { 0xc1 })]
        [InlineData(new byte[] { 0xdd, 0xff, 0xff, 0xff, 0xff })]
        public async Task MalformedBytesAreRejectedAs400WithoutReachingTheEndpointAsync(byte[] bytes)
        {
            var reached = false;
            var context = Context(bytes, WireMediaType.MessagePack, typeof(WireRouteDto));

            await Transcoder(_ =>
            {
                reached = true;

                return Task.CompletedTask;
            }).InvokeAsync(context);

            reached.Should().BeFalse();
            context.Response.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        }

        [Fact]
        public async Task ADeeplyNestedBodyIsRejectedAs400Async()
        {
            var bytes = new byte[500];
            System.Array.Fill(bytes, (byte)0x91);
            bytes[^1] = 0xc0;
            var reached = false;
            var context = Context(bytes, WireMediaType.MessagePack, typeof(WireRouteDto));

            await Transcoder(_ =>
            {
                reached = true;

                return Task.CompletedTask;
            }).InvokeAsync(context);

            reached.Should().BeFalse();
            context.Response.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        }

        [Fact]
        public async Task AMessagePackBodyToAnEndpointThatTakesNoBodyIsRejectedAs415Async()
        {
            var reached = false;
            var context = Context(Pack(Route("x")), WireMediaType.MessagePack, null);

            await Transcoder(_ =>
            {
                reached = true;

                return Task.CompletedTask;
            }).InvokeAsync(context);

            reached.Should().BeFalse();
            context.Response.StatusCode.Should().Be(StatusCodes.Status415UnsupportedMediaType);
        }

        [Fact]
        public async Task AJsonRequestPassesThroughUntouchedAsync()
        {
            var json = Encoding.UTF8.GetBytes("{\"template\":\"a\"}");
            string downstream = null;
            var context = Context(json, "application/json", typeof(WireRouteDto));

            await Transcoder(async ctx =>
            {
                using var reader = new StreamReader(ctx.Request.Body);
                downstream = await reader.ReadToEndAsync();
            }).InvokeAsync(context);

            downstream.Should().Be("{\"template\":\"a\"}");
            context.Request.ContentType.Should().Be("application/json");
        }
    }
}
