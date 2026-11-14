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
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Jube.App.Code.Waf;
using Jube.App.Middlewares;
using Jube.App.Wire;
using Jube.Cache.Resp;
using Jube.Cache.Resp.Interfaces;
using Jube.Dto.Query.WireSchema;
using Jube.Test.Infrastructure;
using Jube.Test.Waf;
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
    public sealed class WireEncryptionEndToEndTests : IAsyncLifetime
    {
        private static readonly JsonSerializerOptions Web = new(JsonSerializerDefaults.Web);

        private readonly RSA serverKey = RSA.Create(2048);
        private readonly FakeTime time = new();
        private readonly FakeHybridResilientRespDatabase resp = new();

        private WebApplication app;
        private HttpClient client;
        private WireCipher cipher;

        public async Task InitializeAsync()
        {
            cipher = new WireCipher(Pem(serverKey.ExportPkcs8PrivateKeyPem()),
                Pem(serverKey.ExportSubjectPublicKeyInfoPem()));

            var environment = TestDynamicEnvironment.Create(new Dictionary<string, string>
            {
                ["WafEnabled"] = "True",
                ["WafMaxInspectionMilliseconds"] = "10000"
            });
            var inspector = new WafInspector(WafTestRuleSets.SeededRegistry(), environment);

            var builder = WebApplication.CreateBuilder();
            builder.WebHost.UseUrls("http://127.0.0.1:0");
            builder.Logging.ClearProviders();
            builder.Services.AddSingleton(new WireOptions(WireMode.MessagePack, cipher.PublicKey, WireCompression.Lz4));
            builder.Services.AddSingleton<WireTypeRegistry>();
            builder.Services.AddSingleton(cipher);
            builder.Services.AddSingleton<ICacheWireReplayRepository>(new CacheWireReplayRepository(resp, TestLog.NoOp));
            builder.Services.AddSingleton<TimeProvider>(time);
            builder.Services.AddAuthentication("Test")
                .AddScheme<AuthenticationSchemeOptions, AllowEveryone>("Test", _ => { });
            builder.Services.AddAuthorization();

            app = builder.Build();
            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseMiddleware<WireEncryptionMiddleware>();
            app.UseMiddleware<MessagePackRequestMiddleware>();
            app.Use((context, next) =>
                new WafMiddleware(_ => next(), inspector, environment, TestLog.NoOp).InvokeAsync(context));
            app.UseEndpoints(endpoints =>
            {
                var wire = endpoints.MapGroup(string.Empty)
                    .AddEndpointFilter<MessagePackResponseFilter>()
                    .AllowMessagePack(true);

                var group = wire.MapGroup("/api/Rows").RequireAuthorization();

                group.MapGet("", () => TypedResults.Ok(new List<WireRouteDto>
                    {
                        new() { Method = "GET", Template = "secret-template", Shape = "s" }
                    }))
                    .Produces<List<WireRouteDto>>();
                group.MapGet("Large", () => TypedResults.Ok(Enumerable.Range(0, 80).Select(i =>
                        new WireRouteDto { Method = "GET", Template = "large-" + i % 4, Shape = "s" }).ToList()))
                    .Produces<List<WireRouteDto>>();
                group.MapPost("", (WireRouteDto body) => TypedResults.Ok(body)).Produces<WireRouteDto>();
                group.MapPost("Reject", (WireRouteDto _) => TypedResults.BadRequest(new { error = "nope-secret" }));

                app.Services.GetRequiredService<WireTypeRegistry>().Attach(endpoints.DataSources);
            });

            await app.StartAsync();

            client = new HttpClient { BaseAddress = new Uri(app.Urls.First()) };
        }

        public async Task DisposeAsync()
        {
            client.Dispose();
            cipher.Dispose();
            serverKey.Dispose();
            await app.StopAsync();
            await app.DisposeAsync();
        }

        private static string Pem(string pem)
        {
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(pem));
        }

        private sealed class FakeTime : TimeProvider
        {
            public DateTimeOffset Now { get; set; } = DateTimeOffset.UtcNow;

            public override DateTimeOffset GetUtcNow()
            {
                return Now;
            }
        }

        private sealed record Envelope(byte[] Key, string Time, string Inner);

        private async Task<(HttpResponseMessage Response, Envelope Envelope)> SendAsync(
            HttpMethod method,
            string path,
            string inner = WireMediaType.Json,
            byte[] plaintext = null,
            RSA wrapWith = null,
            long? sentAt = null,
            Action<HttpRequestMessage> tamper = null,
            string signedPath = null)
        {
            var key = RandomNumberGenerator.GetBytes(WireCipher.KeySize);
            var stamp = (sentAt ?? time.Now.ToUnixTimeMilliseconds()).ToString();
            var wrapped = (wrapWith ?? serverKey).Encrypt(key, RSAEncryptionPadding.OaepSHA256);

            var request = new HttpRequestMessage(method, path);
            request.Headers.Add(WireMediaType.KeyHeader, Convert.ToBase64String(wrapped));
            request.Headers.Add(WireMediaType.TimeHeader, stamp);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(WireMediaType.MessagePack));
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json", 0.9));

            if (plaintext != null)
            {
                var sealedBody = WireCipher.Seal(key, plaintext,
                    WireCipher.RequestAssociatedData(method.Method, signedPath ?? path, stamp, inner));

                request.Headers.Add(WireMediaType.InnerHeader, inner);
                request.Content = new ByteArrayContent(sealedBody);
                request.Content.Headers.ContentType = new MediaTypeHeaderValue(WireMediaType.Encrypted);
            }

            tamper?.Invoke(request);

            return (await client.SendAsync(request), new Envelope(key, stamp, inner));
        }

        private static async Task<byte[]> OpenAsync(HttpResponseMessage response, Envelope envelope)
        {
            response.Content.Headers.ContentType!.MediaType.Should().Be(WireMediaType.Encrypted);

            var inner = response.Headers.GetValues(WireMediaType.InnerHeader).Single();

            return WireCipher.Open(envelope.Key, await response.Content.ReadAsByteArrayAsync(),
                WireCipher.ResponseAssociatedData(envelope.Time, (int)response.StatusCode, inner));
        }

        private static byte[] Json(object value)
        {
            return JsonSerializer.SerializeToUtf8Bytes(value, Web);
        }

        [Fact]
        public async Task AJsonBodyRoundTripsAndNeitherDirectionIsReadableOnTheWireAsync()
        {
            var (response, envelope) = await SendAsync(HttpMethod.Post, "/api/Rows", plaintext: Json(
                new WireRouteDto { Method = "POST", Template = "visible-only-inside", Shape = "s" }));

            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var raw = await response.Content.ReadAsByteArrayAsync();
            Encoding.UTF8.GetString(raw).Should().NotContain("visible-only-inside");

            var plain = await OpenAsync(response, envelope);
            response.Headers.GetValues(WireMediaType.InnerHeader).Single().Should().StartWith(WireMediaType.MessagePack);
            MessagePackSerializer.Deserialize<WireRouteDto>(plain, WireSerializer.Options).Template
                .Should().Be("visible-only-inside");
        }

        [Fact]
        public async Task ALargeResponseIsCompressedBeforeItIsSealedSoEncryptionDoesNotCostTheCompressionAsync()
        {
            var (response, envelope) = await SendAsync(HttpMethod.Get, "/api/Rows/Large");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.Headers.GetValues(WireMediaType.CompressionHeader).Single().Should().Be("lz4");

            var plain = await OpenAsync(response, envelope);

            WireSerializer.IsLz4Block(plain).Should().BeTrue();
            (await response.Content.ReadAsByteArrayAsync()).Length.Should().BeLessThan(80 * 30);
            MessagePackSerializer.Deserialize<List<WireRouteDto>>(plain, WireSerializer.Lz4Options)
                .Should().HaveCount(80);
        }

        [Fact]
        public async Task AMessagePackBodyAlsoRoundTripsInsideTheEnvelopeAsync()
        {
            var body = MessagePackSerializer.Serialize(
                new WireRouteDto { Method = "POST", Template = "packed", Shape = "s" }, WireSerializer.Options);

            var (response, envelope) = await SendAsync(HttpMethod.Post, "/api/Rows", WireMediaType.MessagePack, body);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            MessagePackSerializer.Deserialize<WireRouteDto>(await OpenAsync(response, envelope),
                WireSerializer.Options).Template.Should().Be("packed");
        }

        [Fact]
        public async Task ABodylessReadHasItsResponseEncryptedAsync()
        {
            var (response, envelope) = await SendAsync(HttpMethod.Get, "/api/Rows");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            Encoding.UTF8.GetString(await response.Content.ReadAsByteArrayAsync())
                .Should().NotContain("secret-template");

            MessagePackSerializer.Deserialize<List<WireRouteDto>>(await OpenAsync(response, envelope),
                WireSerializer.Options).Single().Template.Should().Be("secret-template");
        }

        [Fact]
        public async Task AnErrorResponseIsEncryptedTooAndKeepsItsStatusAsync()
        {
            var (response, envelope) = await SendAsync(HttpMethod.Post, "/api/Rows/Reject",
                plaintext: Json(new WireRouteDto { Method = "x", Template = "y", Shape = "z" }));

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            Encoding.UTF8.GetString(await OpenAsync(response, envelope)).Should().Contain("nope-secret");
        }

        [Fact]
        public async Task ARequestWithoutTheKeyHeaderIsServedAsBeforeSoEncryptionIsPerRequestAsync()
        {
            var response = await client.GetAsync("/api/Rows");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            (await response.Content.ReadAsStringAsync()).Should().Contain("secret-template");
        }

        [Fact]
        public async Task ATamperedBodyIsRejectedAsync()
        {
            var (response, _) = await SendAsync(HttpMethod.Post, "/api/Rows",
                plaintext: Json(new WireRouteDto { Method = "a", Template = "b", Shape = "c" }),
                tamper: request =>
                {
                    var bytes = request.Content!.ReadAsByteArrayAsync().Result;
                    bytes[^1] ^= 0x01;
                    var flipped = new ByteArrayContent(bytes);
                    flipped.Headers.ContentType = request.Content.Headers.ContentType;
                    request.Content = flipped;
                });

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task ABodySealedForAnotherPathCannotBeReplayedAtThisOneAsync()
        {
            var (response, _) = await SendAsync(HttpMethod.Post, "/api/Rows",
                plaintext: Json(new WireRouteDto { Method = "a", Template = "b", Shape = "c" }),
                signedPath: "/api/Rows/Reject");

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task AnIdenticalRequestIsRejectedTheSecondTimeAsync()
        {
            var key = RandomNumberGenerator.GetBytes(WireCipher.KeySize);
            var wrapped = Convert.ToBase64String(serverKey.Encrypt(key, RSAEncryptionPadding.OaepSHA256));
            var stamp = time.Now.ToUnixTimeMilliseconds().ToString();

            HttpRequestMessage Build()
            {
                var request = new HttpRequestMessage(HttpMethod.Get, "/api/Rows");
                request.Headers.Add(WireMediaType.KeyHeader, wrapped);
                request.Headers.Add(WireMediaType.TimeHeader, stamp);

                return request;
            }

            (await client.SendAsync(Build())).StatusCode.Should().Be(HttpStatusCode.OK);
            (await client.SendAsync(Build())).StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task ARequestRepeatedAtAnotherNodeSharingTheSameRespIsRejectedAsync()
        {
            var key = RandomNumberGenerator.GetBytes(WireCipher.KeySize);
            var wrapped = serverKey.Encrypt(key, RSAEncryptionPadding.OaepSHA256);
            var stamp = time.Now.ToUnixTimeMilliseconds().ToString();

            async Task<int> Through(ICacheWireReplayRepository repository)
            {
                var context = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Name, "t")], "Test"))
                };
                context.Request.Headers[WireMediaType.KeyHeader] = Convert.ToBase64String(wrapped);
                context.Request.Headers[WireMediaType.TimeHeader] = stamp;
                context.Response.Body = new System.IO.MemoryStream();

                await new WireEncryptionMiddleware(_ => Task.CompletedTask, cipher, repository, time)
                    .InvokeAsync(context);

                return context.Response.StatusCode;
            }

            var nodeA = new CacheWireReplayRepository(resp, TestLog.NoOp);
            var nodeB = new CacheWireReplayRepository(resp, TestLog.NoOp);

            (await Through(nodeA)).Should().Be(StatusCodes.Status200OK);
            (await Through(nodeB)).Should().Be(StatusCodes.Status400BadRequest);
        }

        [Fact]
        public async Task ARespFailureRejectsTheRequestRatherThanSkippingTheReplayCheckAsync()
        {
            resp.ThrowOnMethod = "StringSetIfAbsentAsync";

            var (response, _) = await SendAsync(HttpMethod.Get, "/api/Rows");

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Theory]
        [InlineData(-(WireEncryptionMiddleware.SkewSeconds + 5))]
        [InlineData(WireEncryptionMiddleware.SkewSeconds + 5)]
        public async Task ARequestOutsideTheClockSkewIsRejectedAsync(int offsetSeconds)
        {
            var (response, _) = await SendAsync(HttpMethod.Get, "/api/Rows",
                sentAt: time.Now.AddSeconds(offsetSeconds).ToUnixTimeMilliseconds());

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task AKeyWrappedForADifferentServerIsRejectedAsync()
        {
            using var other = RSA.Create(2048);

            var (response, _) = await SendAsync(HttpMethod.Get, "/api/Rows", wrapWith: other);

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Theory]
        [InlineData("not base64 at all!")]
        [InlineData("AAAA")]
        [InlineData("")]
        public async Task AMalformedKeyHeaderIsRejectedWithoutAnErrorAsync(string header)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "/api/Rows");
            request.Headers.TryAddWithoutValidation(WireMediaType.KeyHeader, header);
            request.Headers.Add(WireMediaType.TimeHeader, time.Now.ToUnixTimeMilliseconds().ToString());

            (await client.SendAsync(request)).StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Theory]
        [InlineData("<script>alert(1)</script>")]
        [InlineData("1 UNION SELECT username, password FROM users--")]
        public async Task AnAttackInsideAnEncryptedBodyIsStillBlockedByTheWafAsync(string attack)
        {
            var (response, _) = await SendAsync(HttpMethod.Post, "/api/Rows",
                plaintext: Json(new WireRouteDto { Method = "a", Template = attack, Shape = "c" }));

            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task AnUnauthenticatedCallerCannotMakeTheServerAttemptADecryptionAsync()
        {
            var reached = false;
            var middleware = new WireEncryptionMiddleware(_ =>
            {
                reached = true;

                return Task.CompletedTask;
            }, cipher, new CacheWireReplayRepository(resp, TestLog.NoOp), time);

            var context = new DefaultHttpContext();
            context.Request.Headers[WireMediaType.KeyHeader] = "AAAA";

            await middleware.InvokeAsync(context);

            reached.Should().BeTrue();
            context.Response.StatusCode.Should().Be(StatusCodes.Status200OK);
        }

        [Fact]
        public void TheSealedFormIsNonceCiphertextAndTagAndOnlyOpensWithTheSameKeyAndContext()
        {
            var key = RandomNumberGenerator.GetBytes(WireCipher.KeySize);
            var aad = WireCipher.ResponseAssociatedData("1", 200, "x");
            var sealedData = WireCipher.Seal(key, [1, 2, 3], aad);

            sealedData.Should().HaveCount(WireCipher.NonceSize + 3 + WireCipher.TagSize);
            WireCipher.Open(key, sealedData, aad).Should().Equal(1, 2, 3);

            var wrongContext = () => WireCipher.Open(key, sealedData, WireCipher.ResponseAssociatedData("2", 200, "x"));
            var wrongKey = () => WireCipher.Open(RandomNumberGenerator.GetBytes(WireCipher.KeySize), sealedData, aad);

            wrongContext.Should().Throw<CryptographicException>();
            wrongKey.Should().Throw<CryptographicException>();
        }

        [Fact]
        public void TwoSealsOfTheSamePlaintextNeverShareANonce()
        {
            var key = RandomNumberGenerator.GetBytes(WireCipher.KeySize);
            var first = WireCipher.Seal(key, [9], []);
            var second = WireCipher.Seal(key, [9], []);

            first.Take(WireCipher.NonceSize).Should().NotEqual(second.Take(WireCipher.NonceSize));
        }

        [Fact]
        public void APublicKeyThatIsNotTheHalfOfThePrivateKeyIsRefusedAtStartup()
        {
            using var other = RSA.Create(2048);

            var act = () => new WireCipher(Pem(serverKey.ExportPkcs8PrivateKeyPem()),
                Pem(other.ExportSubjectPublicKeyInfoPem()));

            act.Should().Throw<InvalidOperationException>();
        }

        [Fact]
        public void AKeyShorterThan2048BitsIsRefusedAtStartup()
        {
            using var weak = RSA.Create(1024);

            var act = () => new WireCipher(Pem(weak.ExportPkcs8PrivateKeyPem()),
                Pem(weak.ExportSubjectPublicKeyInfoPem()));

            act.Should().Throw<InvalidOperationException>();
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
