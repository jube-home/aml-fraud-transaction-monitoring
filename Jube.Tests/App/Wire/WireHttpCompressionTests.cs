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
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using FluentAssertions;
using Jube.App.Wire;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;

namespace Jube.Test.App.Wire
{
    [Trait("Category", "Unit")]
    public sealed class WireHttpCompressionTests
    {
        private static readonly string Large = new('x', 4000);

        private static async Task<HttpResponseMessage> Get(WireHttpCompressionKind kind, bool excludeMessagePack,
            string path, string acceptEncoding)
        {
            var builder = WebApplication.CreateBuilder();
            builder.WebHost.UseUrls("http://127.0.0.1:0");
            builder.Logging.ClearProviders();
            builder.Services.AddWireHttpCompression(kind, CompressionLevel.Fastest, excludeMessagePack);

            await using var app = builder.Build();
            app.UseResponseCompression();
            app.MapGet("/json", () => Results.Text("{\"v\":\"" + Large + "\"}", "application/json"));
            app.MapGet("/msgpack", () => Results.Bytes(System.Text.Encoding.UTF8.GetBytes(Large),
                WireMediaType.MessagePack));
            app.MapGet("/sealed", () => Results.Bytes(System.Text.Encoding.UTF8.GetBytes(Large),
                WireMediaType.Encrypted));
            app.MapGet("/events", () => Results.Text(Large, "text/event-stream"));

            await app.StartAsync();

            using var client = new HttpClient
            {
                BaseAddress = new Uri(app.Urls.First())
            };
            client.DefaultRequestHeaders.AcceptEncoding.ParseAdd(acceptEncoding);

            var response = await client.GetAsync(path, HttpCompletionOption.ResponseHeadersRead);
            await response.Content.LoadIntoBufferAsync();

            await app.StopAsync();

            return response;
        }

        [Theory]
        [InlineData("Gzip", true, WireHttpCompressionKind.Gzip)]
        [InlineData("brotli", true, WireHttpCompressionKind.Brotli)]
        [InlineData("NONE", true, WireHttpCompressionKind.None)]
        [InlineData("Deflate", false, WireHttpCompressionKind.None)]
        [InlineData("1", false, WireHttpCompressionKind.None)]
        [InlineData("", false, WireHttpCompressionKind.None)]
        public void OnlyTheThreeNamedHttpCompressionsParse(string value, bool valid, WireHttpCompressionKind expected)
        {
            WireOptions.TryParseHttpCompression(value, out var kind).Should().Be(valid);

            if (valid)
            {
                kind.Should().Be(expected);
            }
        }

        [Theory]
        [InlineData("Fastest", true, CompressionLevel.Fastest)]
        [InlineData("optimal", true, CompressionLevel.Optimal)]
        [InlineData("SmallestSize", true, CompressionLevel.SmallestSize)]
        [InlineData("NoCompression", false, CompressionLevel.Fastest)]
        [InlineData("0", false, CompressionLevel.Fastest)]
        [InlineData("", false, CompressionLevel.Fastest)]
        public void OnlyTheThreeEffortLevelsParseAndTheNoCompressionLevelIsRefused(string value, bool valid,
            CompressionLevel expected)
        {
            WireOptions.TryParseHttpCompressionLevel(value, out var level).Should().Be(valid);

            if (valid)
            {
                level.Should().Be(expected);
            }
        }

        [Fact]
        public void TheChosenLevelReachesBothProviderOptionsAndCompressionIsAllowedOverHttps()
        {
            var services = new ServiceCollection()
                .AddWireHttpCompression(WireHttpCompressionKind.Gzip, CompressionLevel.Optimal, true)
                .BuildServiceProvider();

            services.GetRequiredService<IOptions<ResponseCompressionOptions>>().Value.EnableForHttps
                .Should().BeTrue();
            services.GetRequiredService<IOptions<GzipCompressionProviderOptions>>().Value.Level
                .Should().Be(CompressionLevel.Optimal);
            services.GetRequiredService<IOptions<BrotliCompressionProviderOptions>>().Value.Level
                .Should().Be(CompressionLevel.Optimal);
        }

        [Fact]
        public async Task BrotliCompressesWithBrotliAndNeverFallsBackToGzipAsync()
        {
            var response = await Get(WireHttpCompressionKind.Brotli, true, "/json", "gzip, br");

            response.Content.Headers.ContentEncoding.Should().Equal("br");
        }

        [Fact]
        public async Task GzipCompressesAJsonResponseForAClientThatAcceptsItAsync()
        {
            var response = await Get(WireHttpCompressionKind.Gzip, true, "/json", "gzip, br");

            response.Content.Headers.ContentEncoding.Should().Equal("gzip");
            response.Content.Headers.ContentLength.Should().BeLessThan(Large.Length / 4);
        }

        [Fact]
        public async Task GzipDoesNotUseBrotliEvenForAClientThatPrefersItAsync()
        {
            var response = await Get(WireHttpCompressionKind.Gzip, true, "/json", "br, gzip;q=0.5");

            response.Content.Headers.ContentEncoding.Should().Equal("gzip");
        }

        [Fact]
        public async Task AClientThatAcceptsNoEncodingGetsThePlainBodyAsync()
        {
            var response = await Get(WireHttpCompressionKind.Gzip, true, "/json", "identity");

            response.Content.Headers.ContentEncoding.Should().BeEmpty();
            response.Content.Headers.ContentLength.Should().BeGreaterThan(Large.Length);
        }

        [Fact]
        public async Task MessagePackIsNotCompressedASecondTimeWhileTheMessagePackCompressionIsOnAsync()
        {
            var response = await Get(WireHttpCompressionKind.Gzip, true, "/msgpack", "gzip");

            response.Content.Headers.ContentEncoding.Should().BeEmpty();
        }

        [Fact]
        public async Task MessagePackIsCompressedHereWhenTheMessagePackCompressionIsOffAsync()
        {
            var response = await Get(WireHttpCompressionKind.Gzip, false, "/msgpack", "gzip");

            response.Content.Headers.ContentEncoding.Should().Equal("gzip");
        }

        [Fact]
        public async Task ASealedEnvelopeAndAnEventStreamAreNeverCompressedAsync()
        {
            (await Get(WireHttpCompressionKind.Gzip, false, "/sealed", "gzip")).Content.Headers.ContentEncoding
                .Should().BeEmpty();
            (await Get(WireHttpCompressionKind.Gzip, false, "/events", "gzip")).Content.Headers.ContentEncoding
                .Should().BeEmpty();
        }
    }
}
