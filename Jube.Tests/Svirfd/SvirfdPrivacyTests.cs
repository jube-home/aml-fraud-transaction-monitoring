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
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Jube.Svirfd;
using Microsoft.Extensions.Logging.Abstractions;
using Svirfd;
using Xunit;

namespace Jube.Test.Svirfd
{
    [Trait("Category", "Unit")]
    public sealed class SvirfdPrivacyTests
    {
        [Theory]
        [InlineData("https://svirfd.com", true)]
        [InlineData("https://example.test/", true)]
        [InlineData("http://localhost:5000", true)]
        [InlineData("http://127.0.0.1", true)]
        [InlineData("http://svirfd.com", false)]
        [InlineData("http://10.0.0.5", false)]
        [InlineData("https://user:secret@svirfd.com", false)]
        [InlineData("ftp://svirfd.com", false)]
        [InlineData("svirfd.com", false)]
        public void OnlyAnHttpsServiceWithoutCredentialsIsTrustedWithTheKey(string baseUrl, bool secure)
        {
            new SvirfdSettings { BaseUrl = baseUrl }.BaseUrlIsSecure.Should().Be(secure);
        }

        [Fact]
        public async Task AnInsecureBaseUrlTurnsLookupsOffAndSendsNothing()
        {
            var handler = new StubSvirfdHandler((_, _) => throw new InvalidOperationException("nothing may be sent"));
            var settings = new SvirfdSettings
            {
                Mode = SvirfdMode.Remote, BaseUrl = "http://svirfd.example", ApiKey = "secret-key"
            };

            await using var host = new SvirfdHost(settings, new HttpClient(handler), NullLogger.Instance);
            await host.StartAsync(CancellationToken.None);

            host.Enabled.Should().BeFalse();
            host.Ready.Should().BeFalse();
            host.StartFailed.Should().BeTrue();
            (await host.LookupAsync("8.8.8.8", CancellationToken.None)).Should().BeNull();
            handler.Calls.Should().Be(0);
        }

        [Theory]
        [InlineData("10.1.2.3")]
        [InlineData("172.16.0.1")]
        [InlineData("172.31.255.255")]
        [InlineData("192.168.1.1")]
        [InlineData("127.0.0.1")]
        [InlineData("169.254.1.1")]
        [InlineData("100.64.0.1")]
        [InlineData("0.0.0.0")]
        [InlineData("224.0.0.1")]
        [InlineData("::1")]
        [InlineData("fe80::1")]
        [InlineData("fc00::1")]
        [InlineData("fd12:3456::1")]
        [InlineData("ff02::1")]
        [InlineData("::ffff:10.0.0.1")]
        public void NonPublicAddressesAreNotRoutable(string address)
        {
            SvirfdAddressScope.IsPubliclyRoutable(IPAddress.Parse(address)).Should().BeFalse();
        }

        [Theory]
        [InlineData("8.8.8.8")]
        [InlineData("172.15.0.1")]
        [InlineData("172.32.0.1")]
        [InlineData("100.128.0.1")]
        [InlineData("192.169.0.1")]
        [InlineData("37.30.56.54")]
        [InlineData("2001:4860:4860::8888")]
        [InlineData("::ffff:8.8.8.8")]
        public void PublicAddressesAreRoutable(string address)
        {
            SvirfdAddressScope.IsPubliclyRoutable(IPAddress.Parse(address)).Should().BeTrue();
        }

        [Fact]
        public async Task ARemoteLookupSendsOnlyThePublicAddressAndNeverAPrivateOne()
        {
            var json = SvirfdJson.Serialize(await SvirfdFixture.RecordAsync("37.30.56.54"));
            var seen = new System.Collections.Generic.List<string>();
            HttpContent? content = null;
            var handler = new StubSvirfdHandler((request, _) =>
            {
                seen.Add(request.RequestUri!.ToString());
                content = request.Content;

                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
                });
            });

            var settings = new SvirfdSettings { Mode = SvirfdMode.Remote, BaseUrl = "https://svirfd.test" };
            await using var host = new SvirfdHost(settings, new HttpClient(handler), NullLogger.Instance);
            await host.StartAsync(CancellationToken.None);

            (await host.LookupAsync("10.0.0.7", CancellationToken.None)).Should().BeNull();
            (await host.LookupAsync("192.168.0.9", CancellationToken.None)).Should().BeNull();
            (await host.LookupAsync("::ffff:8.8.8.8", CancellationToken.None)).Should().NotBeNull();

            seen.Should().Equal("https://svirfd.test/8.8.8.8");
            content.Should().BeNull();
        }
    }
}
