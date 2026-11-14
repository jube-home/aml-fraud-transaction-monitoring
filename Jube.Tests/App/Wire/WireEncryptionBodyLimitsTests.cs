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
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Jube.App.Wire;
using Jube.Cache.Resp.Interfaces;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace Jube.Test.App.Wire
{
    [Trait("Category", "Unit")]
    public sealed class WireEncryptionBodyLimitsTests : IDisposable
    {
        private readonly RSA serverKey = RSA.Create(2048);
        private readonly WireCipher cipher;
        private readonly CountingReplay replay = new();

        public WireEncryptionBodyLimitsTests()
        {
            cipher = new WireCipher(Pem(serverKey.ExportPkcs8PrivateKeyPem()),
                Pem(serverKey.ExportSubjectPublicKeyInfoPem()));
        }

        public void Dispose()
        {
            cipher.Dispose();
            serverKey.Dispose();
        }

        private static string Pem(string pem)
        {
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(pem));
        }

        private async Task<(int Status, bool Reached)> SendAsync(string inner, long? contentLength, int bodyBytes)
        {
            var key = RandomNumberGenerator.GetBytes(WireCipher.KeySize);
            var time = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
            var sealedBody = WireCipher.Seal(key, new byte[bodyBytes],
                WireCipher.RequestAssociatedData("POST", "/api/Rows", time, inner));
            var context = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Name, "t")], "Test"))
            };

            context.Request.Method = "POST";
            context.Request.Path = "/api/Rows";
            context.Request.ContentType = WireMediaType.Encrypted;
            context.Request.ContentLength = contentLength;
            context.Request.Body = new MemoryStream(sealedBody);
            context.Request.Headers[WireMediaType.KeyHeader] =
                Convert.ToBase64String(serverKey.Encrypt(key, RSAEncryptionPadding.OaepSHA256));
            context.Request.Headers[WireMediaType.TimeHeader] = time;
            context.Request.Headers[WireMediaType.InnerHeader] = inner;
            context.Response.Body = new MemoryStream();

            var reached = false;

            await new WireEncryptionMiddleware(_ =>
            {
                reached = true;

                return Task.CompletedTask;
            }, cipher, replay, TimeProvider.System).InvokeAsync(context);

            return (context.Response.StatusCode, reached);
        }

        [Fact]
        public async Task AWellFormedSealedBodyReachesTheEndpointAsync()
        {
            var length = WireCipher.NonceSize + 4 + WireCipher.TagSize;

            var (status, reached) = await SendAsync(WireMediaType.Json, length, 4);

            status.Should().Be(StatusCodes.Status200OK);
            reached.Should().BeTrue();
        }

        [Fact]
        public async Task ASealedBodyWithNoDeclaredLengthIsRefusedBeforeItIsBufferedAsync()
        {
            var (status, reached) = await SendAsync(WireMediaType.Json, null, 4);

            status.Should().Be(StatusCodes.Status400BadRequest);
            reached.Should().BeFalse();
        }

        [Fact]
        public async Task ASealedBodyDeclaringMoreThanSixtyFourMegabytesIsRefusedBeforeItIsBufferedAsync()
        {
            var (status, reached) = await SendAsync(WireMediaType.Json, 64L * 1024 * 1024 + 1, 4);

            status.Should().Be(StatusCodes.Status400BadRequest);
            reached.Should().BeFalse();
            replay.Claims.Should().Be(0);
        }

        [Theory]
        [InlineData("text/html")]
        [InlineData("application/x-www-form-urlencoded")]
        [InlineData("application/x-jube-encrypted")]
        [InlineData("")]
        public async Task AnInnerTypeOutsideJsonAndMessagePackIsRefusedAsync(string inner)
        {
            var length = WireCipher.NonceSize + 4 + WireCipher.TagSize;

            var (status, reached) = await SendAsync(inner, length, 4);

            status.Should().Be(StatusCodes.Status400BadRequest);
            reached.Should().BeFalse();
            replay.Claims.Should().Be(0);
        }

        private sealed class CountingReplay : ICacheWireReplayRepository
        {
            public int Claims { get; private set; }

            public Task<bool> TryClaimAsync(string fingerprint, TimeSpan retention)
            {
                Claims++;

                return Task.FromResult(true);
            }
        }
    }
}
