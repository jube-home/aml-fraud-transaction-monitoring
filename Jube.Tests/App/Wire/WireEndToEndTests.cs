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
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using FluentAssertions;
using Jube.App.Endpoints.Query;
using Jube.App.Wire;
using Jube.Dto.Query.WireSchema;
using MessagePack;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;

namespace Jube.Test.App.Wire
{
    [Trait("Category", "Unit")]
    public sealed class WireEndToEndTests : IAsyncLifetime
    {
        private WebApplication app;
        private HttpClient client;

        public async Task InitializeAsync()
        {
            var builder = WebApplication.CreateBuilder();
            builder.WebHost.UseUrls("http://127.0.0.1:0");
            builder.Logging.ClearProviders();
            builder.Services.AddSingleton<WireTypeRegistry>();
            builder.Services.AddAuthentication("Test")
                .AddScheme<AuthenticationSchemeOptions, AllowEveryone>("Test", _ => { });
            builder.Services.AddAuthorization();

            app = builder.Build();
            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseMiddleware<MessagePackRequestMiddleware>();
            app.UseEndpoints(endpoints =>
            {
                var wire = endpoints.MapGroup(string.Empty)
                    .AddEndpointFilter<MessagePackResponseFilter>()
                    .AllowMessagePack();

                wire.MapWireSchemaEndpoints();

                var group = wire.MapGroup("/api/Rows").RequireAuthorization();

                group.MapGet("", GetAll).Produces<List<WireRouteDto>>();
                group.MapPost("", Echo).Produces<WireRouteDto>();
                group.MapPut("{id:int}", EchoWithId).Produces<WireRouteDto>();
                group.MapPost("Reject", Reject);
                group.MapGet("Anonymous", Anonymous);

                app.Services.GetRequiredService<WireTypeRegistry>().Attach(endpoints.DataSources);
            });

            await app.StartAsync();

            client = new HttpClient { BaseAddress = new Uri(app.Urls.First()) };
        }

        public async Task DisposeAsync()
        {
            client.Dispose();
            await app.StopAsync();
            await app.DisposeAsync();
        }

        private static IResult GetAll()
        {
            return TypedResults.Ok(new List<WireRouteDto>
            {
                new() { Method = "GET", Template = "a", Shape = "b" },
                new() { Method = "POST", Template = "c", Shape = "d" }
            });
        }

        private static IResult Echo(WireRouteDto body)
        {
            return TypedResults.Ok(body);
        }

        private static IResult EchoWithId(int id, WireRouteDto body)
        {
            body.Template += "#" + id;

            return TypedResults.Ok(body);
        }

        private static IResult Reject(WireRouteDto body)
        {
            return TypedResults.BadRequest(new { errors = new[] { new { propertyName = "x", errorMessage = "bad" } } });
        }

        private static IResult Anonymous()
        {
            return TypedResults.Ok(new { Anonymous = true });
        }

        private static HttpRequestMessage Msgpack(HttpMethod method, string path, object body = null)
        {
            var request = new HttpRequestMessage(method, path);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(WireMediaType.MessagePack));
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json", 0.9));

            if (body != null)
            {
                request.Content = new ByteArrayContent(
                    MessagePackSerializer.Serialize(body.GetType(), body, WireSerializer.Options));
                request.Content.Headers.ContentType = new MediaTypeHeaderValue(WireMediaType.MessagePack);
            }

            return request;
        }

        [Fact]
        public async Task AGetAsksingForMessagePackComesBackAsMessagePackWithItsShapeAsync()
        {
            var response = await client.SendAsync(Msgpack(HttpMethod.Get, "/api/Rows"));

            response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
            response.Content.Headers.ContentType!.MediaType.Should().Be(WireMediaType.MessagePack);
            response.Headers.GetValues(WireMediaType.ShapeHeader).Single()
                .Should().Be(typeof(WireRouteDto).FullName + "[]");

            var rows = MessagePackSerializer.Deserialize<List<WireRouteDto>>(
                await response.Content.ReadAsByteArrayAsync(), WireSerializer.Options);

            rows.Select(r => r.Template).Should().Equal("a", "c");
        }

        [Fact]
        public async Task AGetWithoutTheAcceptHeaderIsStillJsonAsync()
        {
            var response = await client.GetAsync("/api/Rows");

            response.Content.Headers.ContentType!.MediaType.Should().Be("application/json");
            (await response.Content.ReadFromJsonAsync<List<WireRouteDto>>())!.Should().HaveCount(2);
        }

        [Fact]
        public async Task AMessagePackPostIsBoundByTheOrdinaryJsonBinderAndEchoedAsMessagePackAsync()
        {
            var response = await client.SendAsync(Msgpack(HttpMethod.Post, "/api/Rows",
                new WireRouteDto { Method = "PATCH", Template = "t", Shape = "s" }));

            response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);

            var echoed = MessagePackSerializer.Deserialize<WireRouteDto>(
                await response.Content.ReadAsByteArrayAsync(), WireSerializer.Options);

            (echoed.Method, echoed.Template, echoed.Shape).Should().Be(("PATCH", "t", "s"));
        }

        [Fact]
        public async Task ARoutedPutWithAParameterBindsBothTheRouteValueAndTheMessagePackBodyAsync()
        {
            var response = await client.SendAsync(Msgpack(HttpMethod.Put, "/api/Rows/42",
                new WireRouteDto { Template = "t" }));

            var echoed = MessagePackSerializer.Deserialize<WireRouteDto>(
                await response.Content.ReadAsByteArrayAsync(), WireSerializer.Options);

            echoed.Template.Should().Be("t#42");
        }

        [Fact]
        public async Task AJsonPostIsUntouchedAndAnswersJsonAsync()
        {
            var response = await client.PostAsJsonAsync("/api/Rows",
                new { method = "GET", template = "j", shape = "s" });

            response.Content.Headers.ContentType!.MediaType.Should().Be("application/json");
            (await response.Content.ReadFromJsonAsync<WireRouteDto>())!.Template.Should().Be("j");
        }

        [Fact]
        public async Task AnErrorAnswersJsonEvenWhenMessagePackWasAskedForAsync()
        {
            var response = await client.SendAsync(Msgpack(HttpMethod.Post, "/api/Rows/Reject",
                new WireRouteDto { Template = "t" }));

            response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
            response.Content.Headers.ContentType!.MediaType.Should().Be("application/json");
            (await response.Content.ReadAsStringAsync()).Should().Contain("propertyName");
        }

        [Fact]
        public async Task AResponseTypeThatCannotBeEncodedFallsBackToJsonAsync()
        {
            var response = await client.SendAsync(Msgpack(HttpMethod.Get, "/api/Rows/Anonymous"));

            response.Content.Headers.ContentType!.MediaType.Should().Be("application/json");
        }

        [Fact]
        public async Task GarbageMessagePackIsA400NotA500Async()
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "/api/Rows")
            {
                Content = new ByteArrayContent([0xc1, 0xc1, 0xc1])
            };
            request.Content.Headers.ContentType = new MediaTypeHeaderValue(WireMediaType.MessagePack);

            var response = await client.SendAsync(request);

            response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task TheSchemaNamesTheRequestRoutesAndItsHashMatchesTheResponseHeaderAsync()
        {
            var rows = await client.SendAsync(Msgpack(HttpMethod.Get, "/api/Rows"));
            var hash = rows.Headers.GetValues(WireMediaType.SchemaHeader).Single();

            var schemaResponse = await client.GetAsync("/api/WireSchema?hash=" + hash);
            var schema = (await schemaResponse.Content.ReadFromJsonAsync<WireSchemaDto>())!;

            schemaResponse.Content.Headers.ContentType!.MediaType.Should().Be("application/json");
            schemaResponse.Headers.CacheControl!.ToString().Should().Contain("immutable");
            schema.Hash.Should().Be(hash);
            schema.Types.Should().ContainKey(typeof(WireRouteDto).FullName!);
            schema.Routes.Should().Contain(r => r.Method == "POST" && r.Template == "api/Rows"
                                                                  && r.Shape == typeof(WireRouteDto).FullName);
            schema.Routes.Should().Contain(r => r.Method == "PUT" && r.Template == "api/Rows/{id:int}");
        }

        [Fact]
        public async Task AStaleSchemaHashIsRevalidatedEveryTimeAndNeverCachedAsImmutableAsync()
        {
            var response = await client.GetAsync("/api/WireSchema?hash=stale");

            response.Headers.CacheControl!.NoCache.Should().BeTrue();
            response.Headers.CacheControl!.ToString().Should().NotContain("immutable");
            response.Headers.ETag.Should().NotBeNull();
        }

        [Fact]
        public async Task AnUnchangedSchemaIsAnsweredWithNotModifiedWhenTheEntityTagMatchesAsync()
        {
            var first = await client.GetAsync("/api/WireSchema");
            var request = new HttpRequestMessage(HttpMethod.Get, "/api/WireSchema");
            request.Headers.IfNoneMatch.Add(first.Headers.ETag!);

            var second = await client.SendAsync(request);

            second.StatusCode.Should().Be(System.Net.HttpStatusCode.NotModified);
            (await second.Content.ReadAsStringAsync()).Should().BeEmpty();
        }

        private sealed class AllowEveryone(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
        {
            protected override Task<AuthenticateResult> HandleAuthenticateAsync()
            {
                var identity = new ClaimsIdentity([new Claim(ClaimTypes.Name, "t")], "Test");

                return Task.FromResult(AuthenticateResult.Success(
                    new AuthenticationTicket(new ClaimsPrincipal(identity), "Test")));
            }
        }
    }
}
