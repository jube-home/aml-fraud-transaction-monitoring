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
    public sealed class SvirfdTenantSwitchTests
    {
        private static SvirfdSettings Remote()
        {
            return new SvirfdSettings
            {
                Mode = SvirfdMode.Remote, BaseUrl = "https://svirfd.test", RemoteLookupTimeout = TimeSpan.FromSeconds(5)
            };
        }

        private static async Task<SvirfdHost> StartAsync(StubSvirfdHandler handler)
        {
            var host = new SvirfdHost(Remote(), new HttpClient(handler), NullLogger.Instance);
            await host.StartAsync(CancellationToken.None);

            return host;
        }

        [Fact]
        public async Task ARemoteLookupForATenantWithTheSwitchOffSendsNothingAsync()
        {
            var handler = new StubSvirfdHandler((_, _) => throw new InvalidOperationException("nothing may be sent"));

            await using var host = await StartAsync(handler);

            (await host.LookupAsync("8.8.8.8", false, CancellationToken.None)).Should().BeNull();
            (await host.LookupFreshAsync("8.8.8.8", false, CancellationToken.None)).Should().BeNull();

            handler.Calls.Should().Be(0);
        }

        [Fact]
        public async Task SwitchedOffLookupsNeverTripTheBreakerOrTheCircuitOrFillTheCacheAsync()
        {
            var handler = new StubSvirfdHandler((_, _) => throw new System.IO.IOException("connection reset"));

            await using var host = await StartAsync(handler);

            for (var i = 1; i <= 25; i++)
            {
                (await host.LookupAsync($"8.8.8.{i}", false, CancellationToken.None)).Should().BeNull();
            }

            handler.Calls.Should().Be(0);
            host.CircuitOpenUntil.Should().BeBefore(DateTime.UtcNow.AddSeconds(1));
            host.CachedRecords.Should().Be(0);

            (await host.LookupAsync("8.8.4.4", true, CancellationToken.None)).Should().BeNull();

            handler.Calls.Should().Be(1);
        }

        [Fact]
        public async Task ASwitchedOffTenantDoesNotReadTheCacheAnotherTenantFilledAsync()
        {
            var handler = new StubSvirfdHandler((_, _) =>
                Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError)));

            await using var host = await StartAsync(handler);

            await host.LookupAsync("8.8.8.8", true, CancellationToken.None);

            handler.Calls.Should().Be(1);

            (await host.LookupAsync("8.8.8.8", false, CancellationToken.None)).Should().BeNull();

            handler.Calls.Should().Be(1);
        }

        [Fact]
        public async Task TheDefaultOverloadsStillAllowTheRemoteLookupAsync()
        {
            var handler = new StubSvirfdHandler((_, _) =>
                Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError)));

            await using var host = await StartAsync(handler);

            await host.LookupAsync("8.8.8.8", CancellationToken.None);

            handler.Calls.Should().Be(1);
        }

        [Fact]
        public async Task ADirectoryLookupIsUnaffectedByTheSwitchAsync()
        {
            await using var host = await SvirfdFixture.StartDirectoryHostAsync();

            (await host.LookupAsync("37.30.56.54", false, CancellationToken.None)).Should().NotBeNull();
            (await host.LookupFreshAsync("185.220.101.151", false, CancellationToken.None)).Should().NotBeNull();
        }

        [Fact]
        public async Task TheNullLookupIgnoresTheSwitchAsync()
        {
            var lookup = new SvirfdNullLookup();

            (await lookup.LookupAsync("8.8.8.8", false, CancellationToken.None)).Should().BeNull();
            (await lookup.LookupFreshAsync("8.8.8.8", true, CancellationToken.None)).Should().BeNull();
        }
    }
}
